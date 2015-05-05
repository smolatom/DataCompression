using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace DataCompression.File
{
    public class CompressedFileWriter
    {
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
