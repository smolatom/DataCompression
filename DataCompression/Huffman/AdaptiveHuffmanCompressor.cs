using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
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
    class AdaptiveHuffmanCompressor
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
            var encodedInput = Compress(input);
            Assert.IsTrue(expectedValue.SequenceEqual(encodedInput), String.Join(" ", encodedInput));
        }

        [TestCase("M")]
        [TestCase("MM")]
        [TestCase("MI")]
        [TestCase("MISSISSIPPI")]
        [TestCase("MISSISSIPI RIVER")]
        [TestCase("ASCII, abbreviated from American Standard Code for Information Interchange,[1] is a character-encoding scheme. Originally based on the English alphabet, it encodes 128 specified characters into 7-bit binary integers as shown by the ASCII chart on the right.[2] The characters encoded are numbers 0 to 9, lowercase letters a to z, uppercase letters A to Z, basic punctuation symbols, control codes that originated with Teletype machines, and a space. For example, lowercase j would become binary 1101010 and decimal 106.")]
        public void OutputTextIsEqualToInputAfterCompressionAndDecompression(string input)
        {
            var encodedInput = Compress(input);
            Debug.WriteLine(String.Join(" ", encodedInput));
            var decodedInput = Decompress(encodedInput);
            StringAssert.AreEqualIgnoringCase(input, decodedInput);
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
            var decodedInput = Decompress(input);
            StringAssert.AreEqualIgnoringCase(output, decodedInput, decodedInput);
        }

        private StringBuilder decompressedText;

        public AdaptiveHuffmanCompressor()
        {
            decompressedText = new StringBuilder();
        }

        /// <summary>
        /// Encodes input using Huffman coding (Vitter's version).
        /// </summary>
        /// <param name="input">String to be encoded</param>
        /// <returns>Bool array cointaining encoded characters.</returns>
        public static bool[] Compress(string input)
        {
            var tree = new VitterTree();
            var asciiEncodedInput = Encoding.ASCII.GetBytes(input);
            var huffmanEncodedChars = asciiEncodedInput.Select(tree.AddChar).ToList();
            var huffmanEncodedInput = new List<bool>();
            huffmanEncodedChars.ForEach(x => x.ToList().ForEach(huffmanEncodedInput.Add));
            return huffmanEncodedInput.ToArray();
        }

        /// <summary>
        /// Decodes input using Huffman coding (Vitter's version).
        /// </summary>
        /// <param name="encodedInput"></param>
        /// <returns>String cointaining decoded characters.</returns>
        public string Decompress(IEnumerable<bool> encodedInput)
        {
            var tree = new VitterTree();
            tree.CharRead += tree_CharRead;
            try
            {
                encodedInput.ToList().ForEach(tree.PushBit);
            }
            catch (Exception e)
            {
                return e.Message;
            } 
            return decompressedText.ToString();
        }

        private void tree_CharRead(object sender, CharReadEventArgs e)
        {
            decompressedText.Append(e.Character);
        }
    }
}
