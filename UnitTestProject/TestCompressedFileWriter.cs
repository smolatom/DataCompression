using System.IO;
using System.Linq;
using DataCompression.File;
using NUnit.Framework;

namespace UnitTestProject
{
    [TestFixture]
    public class TestCompressedFileWriter
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

    }
}
