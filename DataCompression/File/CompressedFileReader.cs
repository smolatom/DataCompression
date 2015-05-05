using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace DataCompression.File
{
    public class CompressedFileReader
    {
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
