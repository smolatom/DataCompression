using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using NUnit.Framework.Constraints;

namespace DataCompression.Huffman
{
    
    [TestFixture]
    class AdaptiveHuffmanCompressor
    {

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
            true, false, false, false, false, false, true,
            false,
            true, false, false, false, false, true, false,
            false, false,
            true, false, false, false, false, true, true,
            true, false, true,
            true, true,
            false,
            false,
            false, true,
            false, false, false,
            true, false, false, false, true, false, false
        })]
        [TestCase("ABCCBCCBDD", new[]
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
            false, true,
            false, false, false,
            true, false, false, false, true, false, false,
            true, false, false, true
        })]
        [TestCase("ABCCBCCBDDB", new[]
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
            false, true,
            false, false, false,
            true, false, false, false, true, false, false,
            true, false, false, true,
            true, true
        })]
        public void CompressionOutputIsRight(string input, bool[] expectedValue)
        {
            var jahoda = AdaptiveHuffmanCompressor.Compress(input);
            Assert.IsTrue(expectedValue.SequenceEqual(jahoda), String.Join(" ", jahoda));
        }

        public static bool[] Compress(string input)
        {
            var tree = new Tree();
            var asciiCodess = Encoding.ASCII.GetBytes(input);
            var jahoda = asciiCodess.Select(tree.Add).ToList();
            var kiwi = new List<bool>();
            jahoda.ForEach(x => x.ToList().ForEach(kiwi.Add));
            return kiwi.ToArray();
        }
    }
}
