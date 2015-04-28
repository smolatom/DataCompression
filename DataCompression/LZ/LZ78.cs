using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;

namespace DataCompression.LZ
{
    [TestFixture]
    class LZ78
    {
        [TestCase(77)]
        public void CompressionOutputIsRight(byte outputValue)
        {
            //var 
            //var compressesInput = Compress(input);
            var expectedValue = new[] { true, false, false, true, true, false, true };
            Assert.IsTrue(expectedValue.SequenceEqual(convertIndexToBits(outputValue)));
        }

        private IEnumerable<bool> convertIndexToBits(ushort index)
        {
            var bits = new BitArray(new int[] {index});
            var intArray = new bool[32];
            bits.CopyTo(intArray, 0);
            var a = intArray.Take(16).Reverse();
            return a;
        }

        private IEnumerable<bool> convertValueToBits(byte value)
        {
            var bits = Convert.ToString(value, 2).Select(s => s.Equals('1')).ToArray();
            return bits;
        }
    }
}
