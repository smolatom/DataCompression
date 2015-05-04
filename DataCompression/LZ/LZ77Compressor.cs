using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataCompression.File;
using NUnit.Framework;

namespace DataCompression.LZ
{
    [TestFixture]
    class LZ77Compressor
    {
        [TestCase("ABRA", new byte[]
        {
            0, 116,
            255, 224, 2, 15, 255, 0, 16, 191, 248, 0, 165, 255, 192, 4, 16
        }, 17)]
        [TestCase("ABRAB", new byte[]
        {
            0, 116,
            255, 224, 2, 15, 255, 0, 16, 191, 248, 0, 164, 0, 0, 12, 32
        }, 17)]
        public void CompressionOutputIsRight(string input, byte[] expectedValue, int bytesCount)
        {
            var path = @"C:\Users\speedy\Documents\TestFiles\UnitTests\LZ77CompressionTest.xml";
            using (var testWriter = new StreamWriter(path))
            {
                testWriter.Write(input);
            }
            var compressor = new LZ77Compressor();
            compressor.Compress(path);

            using (var testReader = new BinaryReader(System.IO.File.Open(path + ".lz", FileMode.Open)))
            {
                var bytes = testReader.ReadBytes(bytesCount);
                CollectionAssert.AreEqual(expectedValue, bytes);
            }
        }

        [TestCase(new byte[]
        {
            0, 116,
            255, 224, 2, 15, 255, 0, 16, 191, 248, 0, 165, 255, 192, 4, 16
        }, "ABRA", 17)]
        [TestCase(new byte[]
        {
            0, 116,
            255, 224, 2, 15, 255, 0, 16, 191, 248, 0, 164, 0, 0, 12, 32
        }, "ABRAB", 17)]
        public void DecompressionOutputIsRight(byte[] input, string expectedValue, int bytesCount)
        {
            var path = @"C:\Users\speedy\Documents\TestFiles\UnitTests\LZ77CompressionTest.xml.lz";
            using (var testWriter = new BinaryWriter(System.IO.File.Open(path, FileMode.Create)))
            {
                testWriter.Write(input);
            }
            var compressor = new LZ77Compressor();
            compressor.Decompress(path);
            using (var testReader = new StreamReader(path.Replace(".lz", string.Empty)))
            {
                var data = testReader.ReadToEnd();
                StringAssert.AreEqualIgnoringCase(expectedValue, data);
            }
        }

        [TestCase("ABRABABRAK")]
        [TestCase("ASCII, abbreviated from American Standard Code for Information Interchange,[1] is a character-encoding scheme. Originally based on the English alphabet, it encodes 128 specified characters into 7-bit binary integers as shown by the ASCII chart on the right.[2] The characters encoded are numbers 0 to 9, lowercase letters a to z, uppercase letters A to Z, basic punctuation symbols, control codes that originated with Teletype machines, and a space. For example, lowercase j would become binary 1101010 and decimal 106.")]
        public void OutputTextIsEqualToInputAfterCompressionAndDecompression(string input)
        {
            var originalFilePath = @"C:\Users\speedy\Documents\TestFiles\UnitTests\LZ77CompressionTest.xml";
            using (var testWriter = new StreamWriter(originalFilePath))
            {
                testWriter.Write(input);
            }
            var compressor = new LZ77Compressor();
            compressor.Compress(originalFilePath);
            var compressedFilePath = @"C:\Users\speedy\Documents\TestFiles\UnitTests\LZ77CompressionTest.xml.lz";
            var decompressor = new LZ77Compressor();
            decompressor.Decompress(compressedFilePath);
            using (var testReader = new StreamReader(originalFilePath))
            {
                var decodedText = testReader.ReadToEnd();
                StringAssert.AreEqualIgnoringCase(input, decodedText);
            }
        }

        //[TestCase("Employees.xml", "EmployeesOriginal.xml")]
        //[TestCase("Employees.js", "EmployeesOriginal.js")]
        [TestCase("nasa.xml", "nasaOriginal.xml")]
        public void OutputTextIsEqualToInputAfterCompressionAndDecompression2(string fileToBeCompressed, string originalFile)
        {
            var originalFilePath = String.Format(@"C:\Users\speedy\Documents\TestFiles\UnitTests\{0}", fileToBeCompressed);
            var compressor = new LZ77Compressor();
            compressor.Compress(originalFilePath);
            var compressedFilePath = string.Format(@"C:\Users\speedy\Documents\TestFiles\UnitTests\{0}.lz", fileToBeCompressed);
            var decompressor = new LZ77Compressor();
            decompressor.Decompress(compressedFilePath);
            using (var testReader = new StreamReader(originalFilePath))
            {
                using (var originalReader = new StreamReader(string.Format(@"C:\Users\speedy\Documents\TestFiles\UnitTests\{0}", originalFile)))
                {
                    var originalData = new char[4096];
                    var decodedData = new char[4096];
                    for (int i = 0; i < testReader.BaseStream.Length; i += 4096)
                    {
                        testReader.ReadBlock(decodedData, 0, 4096);
                        originalReader.ReadBlock(originalData, 0, 4096);
                        CollectionAssert.AreEqual(originalData, decodedData);
                    }
                }
            }
        }

        private List<bool> bitBuffer;
        private StringBuilder charBuffer;
        private StringBuilder dictionary;
        private string phrase;
        private CompressedFileWriter writer;
        private CompressedFileReader reader;
        private const ushort BIT_BLOCK_LENGTH = 65520;
        private const ushort CHAR_BLOCK_LENGTH = 4096;
        private const ushort DICTIONARY_SIZE = 1024;

        public void Compress(string path)
        {
            writer = new CompressedFileWriter(path + ".lz");
            bitBuffer = new List<bool>();
            dictionary = new StringBuilder();
            phrase = string.Empty;
            using (var streamReader = new StreamReader(path))
            {
                var chars = new char[CHAR_BLOCK_LENGTH];
                while (!streamReader.EndOfStream)
                {
                    var readChars = streamReader.ReadBlock(chars, 0, CHAR_BLOCK_LENGTH);
                    var dataToBeEncoded = new StringBuilder(new string(chars.Take(readChars).ToArray()));
                    compressBlock(dataToBeEncoded);
                }
                writer.WriteBlock(bitBuffer);
            }
            writer.Close();
        }

        public void Decompress(string path)
        {
            reader = new CompressedFileReader(path);
            bitBuffer = new List<bool>();
            charBuffer = new StringBuilder();
            dictionary = new StringBuilder();
            phrase = string.Empty;
            using (var streamWriter = new StreamWriter(path.Replace(".lz", string.Empty)))
            {
                while (reader.CanReadBlock)
                {
                    var bits = reader.ReadBlock();
                    decompressBlock(bits, streamWriter);
                }
            }
            reader.Close();
        }

        private void decompressBlock(List<bool> bits, StreamWriter streamWriter)
        {
            bits.InsertRange(0, bitBuffer);
            bitBuffer.Clear();
            var offsetInBits = 0;
            while (offsetInBits + 29 <= bits.Count)
            {
                var offsetInDictionary = readOffset(bits, ref offsetInBits);
                var lengthOfPhrase = readLength(bits, ref offsetInBits);
                string newPhrase = offsetInDictionary == 2047 ? readChar(bits, ref offsetInBits).ToString() : dictionary.ToString().Substring(offsetInDictionary, lengthOfPhrase) + readChar(bits, ref offsetInBits);
                charBuffer.Append(newPhrase);
                dictionary.Append(newPhrase);
            }
            bitBuffer.AddRange(bits.GetRange(offsetInBits, bits.Count - offsetInBits));
            streamWriter.Write(charBuffer.ToString());
            charBuffer.Clear();
        }

        private char readChar(List<bool> bits, ref int offsetInBits)
        {
            var charValue = bits.GetRange(offsetInBits, 7);
            offsetInBits += 7;
            return Encoding.ASCII.GetChars(new byte[] { (byte)getUshort(charValue) })[0];
        }

        private ushort readOffset(List<bool> bits, ref int offsetInBits)
        {
            var offset = getUshort(bits.GetRange(offsetInBits, 11));
            offsetInBits += 11;
            return offset;
        }

        private ushort readLength(List<bool> bits, ref int offsetInBits)
        {
            var length = getUshort(bits.GetRange(offsetInBits, 11));
            offsetInBits += 11;
            return length;
        }

        private ushort getUshort(List<bool> bits)
        {
            return (ushort)bits.Select((t, i) => t ? 1 << (bits.Count - 1 - i) : 0).Sum();
        }

        private void compressBlock(StringBuilder dataToBeEncoded)
        {
            while (dataToBeEncoded.Length > 0)
            {
                var indexInData = 0;
                var offset = 2047;
                var actualDictionary = dictionary.ToString();
                phrase += dataToBeEncoded[indexInData];
                var indexOfPhrase = actualDictionary.IndexOf(phrase, 0);
                while (indexOfPhrase > -1 && indexInData + 1 < dataToBeEncoded.Length)
                {
                    offset = indexOfPhrase;
                    phrase += dataToBeEncoded[++indexInData];
                    indexOfPhrase = actualDictionary.IndexOf(phrase, indexOfPhrase);
                }
                
                var compressedPhrase = compressPhrase((ushort)offset, (ushort)(phrase.Length - 1), phrase.Last());
                if (bitBuffer.Count + compressedPhrase.Count < BIT_BLOCK_LENGTH)
                {
                    bitBuffer.AddRange(compressedPhrase);
                }
                else
                {
                    var missingBitsCount = BIT_BLOCK_LENGTH - bitBuffer.Count;
                    bitBuffer.AddRange(compressedPhrase.GetRange(0, missingBitsCount));
                    writer.WriteBlock(bitBuffer);
                    bitBuffer = compressedPhrase.GetRange(missingBitsCount, compressedPhrase.Count - missingBitsCount);
                }
                dictionary.Append(dataToBeEncoded.ToString(0, phrase.Length));
                if (dictionary.Length > DICTIONARY_SIZE)
                    dictionary.Remove(DICTIONARY_SIZE, dictionary.Length - DICTIONARY_SIZE);
                dataToBeEncoded.Remove(0, phrase.Length);
                phrase = string.Empty;
            }

        }

        private List<bool> compressPhrase(ushort offset, ushort length, char nextChar)
        {
            var compressedPhrase = new List<bool>();
            compressedPhrase.AddRange(convertUshortToBits(offset, 11));
            compressedPhrase.AddRange(convertUshortToBits(length, 11));
            compressedPhrase.AddRange(convertUshortToBits(nextChar, 7));
            return compressedPhrase;
        }

        private IEnumerable<bool> convertUshortToBits(ushort value, byte length)
        {
            var bits = new BitArray(new int[] { value });
            var intArray = new bool[32];
            bits.CopyTo(intArray, 0);
            return intArray.Take(length).Reverse();
        }
    }
}
