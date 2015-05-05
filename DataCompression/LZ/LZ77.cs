using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using DataCompression.File;

namespace DataCompression.LZ
{
    public class LZ77
    {
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
