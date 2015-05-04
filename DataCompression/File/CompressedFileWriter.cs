using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using NUnit.Framework;

namespace DataCompression.File
{
    [TestFixture]
    public class CompressedFileWriter
    {
        [TestCase(new[] { true, false, true, true, false, false, true, false }, 178)]
        [TestCase(new[] { true, false, true, true, false, false, false, true }, 177)]
        [TestCase(new[] { true, false, true, true, true, false, false, true }, 185)]
        [TestCase(new[] { true, true, true, true, true, true, true, true }, 255)]
        [TestCase(new[] { false, false, false, false, false, false, false, false }, 0)]
        public void TestBinaryShift(bool[] input, byte expectedValue)
        {
            var value = input.Select((t, i) => t ? 1 << (7 - i) : 0).Sum();
            Assert.AreEqual(expectedValue, value);
        }

        [TestCase(new bool[] { }, new byte[] { })]
        [TestCase(new[]
        {
            true, false, true, true, false, false, true, false,
            true, false, true, true, true, false, false, true,
            true, true, true
        }, new byte[] { 0, 19, 178, 185, 224 })]
        [TestCase(new[]
        {
            true, true, true, true, true, true, true, true,
            false, false, false, false, false, false, false, false,
            true, true, true, true, true, true, true, true,
            true, false, true, true, false, false, false, true,
            false, false, false, true

        }, new byte[] { 0, 36, 255, 0, 255, 177, 16 })]
        public void CheckWrittenBytes(bool[] input, byte[] output)
        {
            var path = @"C:\Users\speedy\Documents\TestFiles\UnitTests\FileWritter.wr";
            var testWriter = new CompressedFileWriter(path);
            testWriter.WriteBlock(input.ToList());
            testWriter.Close();
            using (var testReader = new BinaryReader(System.IO.File.Open(path, FileMode.Open)))
            {
                var byteBuffer = new byte[8192];
                var readBytesCount = testReader.Read(byteBuffer, 0, 8192);
                var expectedValue = new byte[8192];
                for (int i = 0; i < output.Length; i++)
                {
                    expectedValue[i] = output[i];
                }
                CollectionAssert.AreEqual(expectedValue, byteBuffer);
                Assert.AreEqual(readBytesCount, output.Length);
            }
        }

        private BinaryWriter writer;

        public CompressedFileWriter()
        {
                
        }

        public CompressedFileWriter(string path)
        {
            writer = new BinaryWriter(System.IO.File.Open(path, FileMode.Create));
        }

        public void Close()
        {
            writer.Close();
        }

        public void WriteBlock(List<bool> bits)
        {
            if (bits.Count <= 65520)
                writer.Write(bitsToBytes(bits));
        }

        private byte[] bitsToBytes(List<bool> bits)
        {
            var partialByteNeeded = bits.Count % 8 > 0;
            var valueBytesCount = bits.Count / 8 + (partialByteNeeded ? 1 : 0);
            if (valueBytesCount > 0)
            {
                var totalBytesCount = valueBytesCount + 2;
                var byteArray = new byte[totalBytesCount];
                addCountOfBits(bits.Count, byteArray);
                addBytes(bits, byteArray);
                return byteArray;
            }
            return new byte[0];
        }

        private void addCountOfBits(int bitsCount, byte[] bytes)
        {
            var upperByte = (byte)(bitsCount / 256);
            var lowerByte = (byte)(bitsCount - upperByte * 256);
            bytes[0] = upperByte;
            bytes[1] = lowerByte;
        }

        private void addBytes(List<bool> bits, byte[] byteArray)
        {
            var extraBitsCount = 8 - bits.Count % 8;
            if (extraBitsCount < 8)
                for (int i = 0; i < extraBitsCount; i++)
                    bits.Add(false);
            var index = 2;
            for (int i = 0; i < bits.Count; i += 8, index++)
            {
                byteArray[index] = getByte(bits.GetRange(i, 8));
            }

        }

        private byte getByte(List<bool> bits)
        {
            return (byte)bits.Select((t, i) => t ? 1 << (7 - i) : 0).Sum();
        }
    }
}
