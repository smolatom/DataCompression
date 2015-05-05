using System.IO;
using System.Linq;
using DataCompression.File;
using NUnit.Framework;

namespace UnitTestProject
{
    [TestFixture]
    public class TestCompressedFileReader
    {
        [TestCase(new byte[] { 0, 48, 49, 126, 242, 17, 186, 255 }, new bool[]
        {
            false, false, true, true, false, false, false, true,
            false, true, true, true, true, true, true, false,
            true, true, true, true, false, false, true, false,
            false, false, false, true, false, false, false, true,
            true, false, true, true, true, false, true, false,
            true, true, true, true, true, true, true, true
        })]
        [TestCase(new byte[] { 0, 144, 49, 126, 242, 17, 186, 255, 49, 126, 242, 17, 186, 255, 57, 94, 240, 145, 58, 0 }, new bool[]
        {
            false, false, true, true, false, false, false, true,
            false, true, true, true, true, true, true, false,
            true, true, true, true, false, false, true, false,
            false, false, false, true, false, false, false, true,
            true, false, true, true, true, false, true, false,
            true, true, true, true, true, true, true, true,
            false, false, true, true, false, false, false, true,
            false, true, true, true, true, true, true, false,
            true, true, true, true, false, false, true, false,
            false, false, false, true, false, false, false, true,
            true, false, true, true, true, false, true, false,
            true, true, true, true, true, true, true, true,
            false, false, true, true, true, false, false, true,
            false, true, false, true, true, true, true, false,
            true, true, true, true, false, false, false, false,
            true, false, false, true, false, false, false, true,
            false, false, true, true, true, false, true, false,
            false, false, false, false, false, false, false, false
        })]
        public void CheckReadBytes(byte[] input, bool[] expectedBits)
        {
            var path = @"C:\Users\speedy\Documents\TestFiles\UnitTests\FileReader.lz";
            using (var testWriter = new BinaryWriter(File.Open(path, FileMode.Create)))
            {
                foreach (var byteValue in input)
                {
                    testWriter.Write(byteValue);
                }
                testWriter.Close();
                testWriter.Dispose();
            }

            var testReader = new CompressedFileReader(path);
            var bits = testReader.ReadBlock();
            testReader.Close();
            CollectionAssert.AreEqual(expectedBits.ToList(), bits);
        }
    }
}
