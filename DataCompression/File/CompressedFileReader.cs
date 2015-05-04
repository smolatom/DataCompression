using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;

namespace DataCompression.File
{
    [TestFixture]
    public class CompressedFileReader
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
            using (var testWriter = new BinaryWriter(System.IO.File.Open(path, FileMode.Create)))
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

        public bool CanReadBlock
        {
            get { return reader.BaseStream.Position < reader.BaseStream.Length - 3; }
        }

        private BinaryReader reader;

        public CompressedFileReader()
        {

        }

        public CompressedFileReader(string path)
        {
            if (System.IO.File.Exists(path) && path.EndsWith(".lz"))
            {
                reader = new BinaryReader(System.IO.File.Open(path, FileMode.Open));
            }
            else
                throw new FileNotFoundException(String.Format("File with a path {0} and an extension lz dosn't exist.", path));
        }

        public void Close()
        {
            reader.Close();
        }

        public List<bool> ReadBlock()
        {
            if (CanReadBlock)
            {
                var valueBitsCount = getBitsCountInBlock();
                var partialByteNeeded = valueBitsCount % 8 > 0;
                var valueBytesCount = valueBitsCount / 8 + (partialByteNeeded ? 1 : 0);
                var bytes = new byte[valueBytesCount];
                reader.Read(bytes, 0, valueBytesCount);
                var bits = new List<bool>();
                foreach (var byteValue in bytes)
                {
                    bits.AddRange(getBits(byteValue));
                }
                return bits.GetRange(0, valueBitsCount);
            }
            return new List<bool>();
        }

        private ushort getBitsCountInBlock()
        {
            var upperByte = reader.ReadByte();
            var lowerByte = reader.ReadByte();
            return (ushort)(upperByte * 256 + lowerByte);
        }

        private IEnumerable<bool> getBits(byte value)
        {
            var bitsArray = new BitArray(new int[] { value });
            var intArray = new bool[32];
            bitsArray.CopyTo(intArray, 0);
            var bits = intArray.Take(8).Reverse().ToList();
            return bits;
        }
    }
}
