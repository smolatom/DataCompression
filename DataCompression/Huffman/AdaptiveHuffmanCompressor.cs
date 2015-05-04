using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using NUnit.Framework;
using NUnit.Framework.Constraints;

namespace DataCompression.Huffman
{
    /// <summary>
    /// AdaptiveHuffmanCompressor implements Huffman's coding (Vitter variant).
    /// </summary>
    [TestFixture]
    public class AdaptiveHuffmanCompressor
    {
        [SetUp]
        public void Init()
        {
            decompressedText = new StringBuilder();
        }

        [TestCase("A", new[] { true, false, false, false, false, false, true })]
        [TestCase("B", new[] { true, false, false, false, false, true, false })]
        [TestCase("AB", new[]
        {
            true, false, false, false, false, false, true,
            false,
            true, false, false, false, false, true, false
        })]
        [TestCase("ABC", new[]
        {
            true, false, false, false, false, false, true,
            false,
            true, false, false, false, false, true, false,
            false, false,
            true, false, false, false, false, true, true
        })]
        [TestCase("ABCC", new[]
        {
            true, false, false, false, false, false, true,
            false,
            true, false, false, false, false, true, false,
            false, false,
            true, false, false, false, false, true, true,
            true, false, true
        })]
        [TestCase("ABCCB", new[]
        {
            true, false, false, false, false, false, true,
            false,
            true, false, false, false, false, true, false,
            false, false,
            true, false, false, false, false, true, true,
            true, false, true,
            true, true
        })]
        [TestCase("ABCCBC", new[]
        {
            true, false, false, false, false, false, true,
            false,
            true, false, false, false, false, true, false,
            false, false,
            true, false, false, false, false, true, true,
            true, false, true,
            true, true,
            false
        })]
        [TestCase("ABCCBCC", new[]
        {
            true, false, false, false, false, false, true,
            false,
            true, false, false, false, false, true, false,
            false, false,
            true, false, false, false, false, true, true,
            true, false, true,
            true, true,
            false,
            false
        })]
        [TestCase("ABCCBCCB", new[]
        {
            true, false, false, false, false, false, true,
            false,
            true, false, false, false, false, true, false,
            false, false,
            true, false, false, false, false, true, true,
            true, false, true,
            true, true,
            false,
            false,
            false, true
        })]
        [TestCase("ABCCBCCBD", new[]
        {
            true, false, false, false, false, false, true,  //A
            false,
            true, false, false, false, false, true, false,  //B
            false, false,
            true, false, false, false, false, true, true,   //C
            true, false, true,                              //C
            true, true,                                     //B
            false,                                          //C
            false,                                          //C
            false, true,                                    //B
            false, false, false,                            
            true, false, false, false, true, false, false   //D
        })]
        [TestCase("MISSISSIPPI", new[]
        {
            true, false, false, true, true, false, true,    //M
            false,                                          
            true, false, false, true, false, false, true,   //I
            false, false,
            true, false, true, false, false, true, true,    //S
            true, false, true,                              //S
            true, true,                                     //I
            false,                                          //S
            false,                                          //S
            false, true,                                    //I
            false, false, false,
            true, false, true, false, false, false, false,  //P
            true, false, false, true,                       //P
            true, true                                      //I

        })]
        public void CompressionOutputIsRight(string input, bool[] expectedValue)
        {
            const string file = @"C:\Users\speedy\Documents\TestFiles\UnitTests\HuffmanCompressionTest.js";
            using (var writer = new StreamWriter(file))
            {
                writer.Write(input);
            }
            Compress(file);
            const string compressedFile = @"C:\Users\speedy\Documents\TestFiles\UnitTests\HuffmanCompressionTest.js.huff";
            var bitsFromFile = getBits(compressedFile);

            Assert.IsTrue(expectedValue.SequenceEqual(bitsFromFile), String.Join(" ", bitsFromFile));
        }

        [TestCase("M")]
        [TestCase("MM")]
        [TestCase("MI")]
        [TestCase("MISSISSIPPI")]
        [TestCase("MISSISSIPI RIVER")]
        [TestCase("ASCII, abbreviated from American Standard Code for Information Interchange,[1] is a character-encoding scheme. Originally based on the English alphabet, it encodes 128 specified characters into 7-bit binary integers as shown by the ASCII chart on the right.[2] The characters encoded are numbers 0 to 9, lowercase letters a to z, uppercase letters A to Z, basic punctuation symbols, control codes that originated with Teletype machines, and a space. For example, lowercase j would become binary 1101010 and decimal 106.")]
        public void OutputTextIsEqualToInputAfterCompressionAndDecompression(string input)
        {
            const string file = @"C:\Users\speedy\Documents\TestFiles\UnitTests\HuffmanComplexTest.js";
            using (var writer = new StreamWriter(file))
            {
                writer.Write(input);
            }
            Compress(file);
            const string compressedFile = @"C:\Users\speedy\Documents\TestFiles\UnitTests\HuffmanComplexTest.js.huff";
            Decompress(compressedFile);
            string decompressedData;
            using (var reader = new StreamReader(file))
            {
                decompressedData = reader.ReadToEnd();
            }
            StringAssert.AreEqualIgnoringCase(input, decompressedData);
        }

        [TestCase(new[]
        {
            true, false, false, true, true, false, true,    //M
            false,                                          
            true, false, false, true, false, false, true,   //I
            false, false,
            true, false, true, false, false, true, true,    //S
            true, false, true,                              //S
            true, true,                                     //I
            false,                                          //S
            false,                                          //S
            false, true,                                    //I
            false, false, false,
            true, false, true, false, false, false, false,  //P
            true, false, false, true,                       //P
            true, true                                      //I

        },
            "MISSISSIPPI")]
        public void DecompressionOutputIsRight(bool[] input, string output)
        {
            var compressedFile = @"C:\Users\speedy\Documents\TestFiles\UnitTests\HuffmanCompressionTest.js.huff";
            using (var writer = new BinaryWriter(System.IO.File.Create(compressedFile)))
            {
                var length = input.Length;
                var bitsToBeWritten = new List<bool>();
                if (length > 0)
                {
                    var modulo = 8 - length % 8;
                    var bitsLeft = (byte)(modulo < 3 ? 8 + modulo - 3 : modulo - 3);
                    var moduloBits = convertByteToBits(bitsLeft);
                    for (int i = 5; i < 8; i++)
                    {
                        bitsToBeWritten.Add(moduloBits[i]);
                    }
                    for (int i = 0; i < bitsLeft; i++)
                    {
                        bitsToBeWritten.Add(false);
                    }
                    bitsToBeWritten.AddRange(input);
                }
                var buffer = new List<bool>();
                foreach (var bit in bitsToBeWritten)
                {
                    if (buffer.Count < 8)
                        buffer.Add(bit);
                    if (buffer.Count == 8)
                    {
                        var value = getByte(buffer);
                        buffer.Clear();
                        writer.Write(value);
                    }
                }
            }
            Decompress(compressedFile);
            var data = string.Empty;
            using (var reader = new StreamReader(compressedFile.Replace(".huff", String.Empty)))
            {
                data = reader.ReadToEnd();
            }
            
            StringAssert.AreEqualIgnoringCase(output, data, data);
        }

        private StringBuilder decompressedText;

        public AdaptiveHuffmanCompressor()
        {
            decompressedText = new StringBuilder();
        }

        /// <summary>
        /// Encodes input using Huffman coding (Vitter's version) and produces it as a file.
        /// </summary>
        /// <param name="path">File to be compressed</param>
        public void Compress(string path)
        {
            var extension = Path.GetExtension(path).ToLower();
            if (System.IO.File.Exists(path) && (extension == ".js" || extension == ".xml"))
            {
                var bits = new bool[0];
                using (var reader = new StreamReader(path))
                {
                    var data = reader.ReadToEnd();
                    try
                    {
                        bits = compress(data);
                    }
                    catch (Exception e) { }
                }

                var length = bits.Length;
                var bitsToBeWritten = new List<bool>();
                if (length > 0)
                {
                    var modulo = 8 - length % 8;
                    var bitsLeft = (byte)(modulo < 3 ? 8 + modulo - 3 : modulo - 3);
                    var moduloBits = convertByteToBits(bitsLeft);
                    for (int i = 5; i < 8; i++)
                    {
                        bitsToBeWritten.Add(moduloBits[i]);
                    }
                    for (int i = 0; i < bitsLeft; i++)
                    {
                        bitsToBeWritten.Add(false);
                    }
                    bitsToBeWritten.AddRange(bits);
                }
                using (var writter = new BinaryWriter(System.IO.File.Create(String.Format("{0}.huff", path))))
                {
                    var buffer = new List<bool>();
                    foreach (var bit in bitsToBeWritten)
                    {
                        if (buffer.Count < 8)
                            buffer.Add(bit);
                        if (buffer.Count == 8)
                        {
                            var value = getByte(buffer);
                            buffer.Clear();
                            writter.Write(value);
                        }
                    }
                }
            }
        }

        private static List<bool> convertByteToBits(byte index)
        {
            var bits = new BitArray(new int[] { index });
            var intArray = new bool[32];
            bits.CopyTo(intArray, 0);
            var a = intArray.Take(8).Reverse().ToList();
            return a;
        }

        private byte getByte(List<bool> bits)
        {
            bits.Reverse();
            byte value = 0;
            for (int i = 0; i < 8; i++)
            {
                value += (byte)(bits[i] ? Math.Pow(2, i) : 0);
            }
            return value;
        }

        /// <summary>
        /// Decodes input using Huffman coding (Vitter's version) and produces it as a file.
        /// </summary>
        /// <param name="path">File to be decompressed</param>
        public void Decompress(string path)
        {
            bool[] bitsFromFile = getBits(path);

            var tree = new VitterTree();
            tree.CharRead += tree_CharRead;
            try
            {
                bitsFromFile.ToList().ForEach(tree.PushBit);
            }
            catch (Exception e)
            {
                decompressedText.Clear();
                decompressedText.Append(e.Message);
            }
            using (var writer = new StreamWriter(path.Replace(".huff", String.Empty)))
            {
                writer.Write(decompressedText.ToString());
            }
        }

        private static bool[] getBits(string path)
        {
            bool[] bitsFromFile;
            using (var reader = new BinaryReader(System.IO.File.Open(path, FileMode.Open)))
            {
                var firstPartOfData = getFirstPartOfData(reader);
                var streamLength = reader.BaseStream.Length;
                bitsFromFile = new bool[(streamLength - reader.BaseStream.Position) * 8 + firstPartOfData.Length];
                firstPartOfData.CopyTo(bitsFromFile, 0);
                var index = firstPartOfData.Length;
                while (reader.BaseStream.Position < streamLength)
                {
                    addBitsToArray(reader.ReadByte(), bitsFromFile, ref index);
                }
            }
            return bitsFromFile;
        }

        private static void addBitsToArray(byte readByte, bool[] bitsFromFile, ref int index)
        {
            for (int i = 0; i < 8; i++)
            {
                bitsFromFile[index++] = (readByte & (1 << (7 - i))) > 0;
            }           
        }

        private static bool[] getFirstPartOfData(BinaryReader reader)
        {
            var firstByte = reader.ReadByte();
            var offset = calculateOffset(firstByte);
            if (offset == 5)
                return new bool[0];
            if (offset < 5)
            {
                var data = new bool[8 - offset - 3];
                for (int i = 0; i < data.Length; i++)
                {
                    data[i] = (firstByte & (1 << (data.Length - 1 - i))) > 0;
                }
                return data;
            }
            else 
            {
                offset = offset + 3 - 8;
                var secondByte = reader.ReadByte();
                var data = new bool[8 - offset];
                for (int i = 0; i < data.Length; i++)
                {
                    data[i] = (secondByte & (1 << (data.Length - 1 - i))) > 0;
                }
                return data;
            }

        }

        private static int calculateOffset(byte firstByte)
        {
            var firstBit = (firstByte & (1 << 7)) >> 7;
            var secondBit = (firstByte & (1 << 6)) >> 6;
            var thirdBit = (firstByte & (1 << 5)) >> 5;
            var offset = 4 * firstBit + 2 * secondBit + 1 * thirdBit;
            return offset;
        }

        private bool[] compress(string input)
        {
            var tree = new VitterTree();
            var asciiEncodedInput = Encoding.ASCII.GetBytes(input);
            var huffmanEncodedChars = asciiEncodedInput.Select(tree.AddChar).ToList();
            var huffmanEncodedInput = new List<bool>();
            huffmanEncodedChars.ForEach(x => x.ToList().ForEach(huffmanEncodedInput.Add));
            return huffmanEncodedInput.ToArray();
        }
        private void tree_CharRead(object sender, CharReadEventArgs e)
        {
            decompressedText.Append(e.Character);
        }
    }
}
