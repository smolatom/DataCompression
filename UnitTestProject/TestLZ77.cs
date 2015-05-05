using System;
using System.IO;
using NUnit.Framework;
using DataCompression.LZ;

namespace UnitTestProject
{
    [TestFixture]
    class TestLZ77
    {
        [TestCase("ABRA", new byte[]
        {
            0, 116,
            255, 224, 2, 15, 255, 0, 16, 191, 248, 0, 165, 255, 192, 4, 16
        }, 17)]
        [TestCase("ABRAB", new byte[]
        {
            0, 116,
            255, 224, 2, 15, 255, 0, 16, 191, 248, 0, 164, 0, 0, 12, 32
        }, 17)]
        public void CompressionOutputIsRight(string input, byte[] expectedValue, int bytesCount)
        {
            var path = @"C:\Users\speedy\Documents\TestFiles\UnitTests\LZ77CompressionTest.xml";
            using (var testWriter = new StreamWriter(path))
            {
                testWriter.Write(input);
            }
            var compressor = new LZ77();
            compressor.Compress(path);

            using (var testReader = new BinaryReader(File.Open(path + ".lz", FileMode.Open)))
            {
                var bytes = testReader.ReadBytes(bytesCount);
                CollectionAssert.AreEqual(expectedValue, bytes);
            }
        }

        [TestCase(new byte[]
        {
            0, 116,
            255, 224, 2, 15, 255, 0, 16, 191, 248, 0, 165, 255, 192, 4, 16
        }, "ABRA", 17)]
        [TestCase(new byte[]
        {
            0, 116,
            255, 224, 2, 15, 255, 0, 16, 191, 248, 0, 164, 0, 0, 12, 32
        }, "ABRAB", 17)]
        public void DecompressionOutputIsRight(byte[] input, string expectedValue, int bytesCount)
        {
            var path = @"C:\Users\speedy\Documents\TestFiles\UnitTests\LZ77CompressionTest.xml.lz";
            using (var testWriter = new BinaryWriter(File.Open(path, FileMode.Create)))
            {
                testWriter.Write(input);
            }
            var compressor = new LZ77();
            compressor.Decompress(path);
            using (var testReader = new StreamReader(path.Replace(".lz", string.Empty)))
            {
                var data = testReader.ReadToEnd();
                StringAssert.AreEqualIgnoringCase(expectedValue, data);
            }
        }

        [TestCase("ABRABABRAK")]
        [TestCase("ASCII, abbreviated from American Standard Code for Information Interchange,[1] is a character-encoding scheme. Originally based on the English alphabet, it encodes 128 specified characters into 7-bit binary integers as shown by the ASCII chart on the right.[2] The characters encoded are numbers 0 to 9, lowercase letters a to z, uppercase letters A to Z, basic punctuation symbols, control codes that originated with Teletype machines, and a space. For example, lowercase j would become binary 1101010 and decimal 106.")]
        public void OutputTextIsEqualToInputAfterCompressionAndDecompression(string input)
        {
            var originalFilePath = @"C:\Users\speedy\Documents\TestFiles\UnitTests\LZ77CompressionTest.xml";
            using (var testWriter = new StreamWriter(originalFilePath))
            {
                testWriter.Write(input);
            }
            var compressor = new LZ77();
            compressor.Compress(originalFilePath);
            var compressedFilePath = @"C:\Users\speedy\Documents\TestFiles\UnitTests\LZ77CompressionTest.xml.lz";
            var decompressor = new LZ77();
            decompressor.Decompress(compressedFilePath);
            using (var testReader = new StreamReader(originalFilePath))
            {
                var decodedText = testReader.ReadToEnd();
                StringAssert.AreEqualIgnoringCase(input, decodedText);
            }
        }

        [TestCase("Employees.xml", "EmployeesOriginal.xml")]
        [TestCase("Employees.js", "EmployeesOriginal.js")]
        [TestCase("base64img.xml", "base64imgOriginal.xml")]
        [TestCase("base64img.js", "base64imgOriginal.js")]
        [TestCase("nasa.xml", "nasaOriginal.xml")]
        [TestCase("nasa.xml", "nasaOriginal.xml")]
        [TestCase("SigmodRecord.xml", "SigmodRecordOriginal.xml")]
        [TestCase("SigmodRecord.js", "SigmodRecordOriginal.js")]
        [TestCase("dblp.xml", "dblpOriginal.xml")]
        [TestCase("dblp.js", "dblpOriginal.js")]
        public void OutputTextIsEqualToInputAfterCompressionAndDecompression2(string fileToBeCompressed, string originalFile)
        {
            var originalFilePath = String.Format(@"C:\Users\speedy\Documents\TestFiles\UnitTests\{0}", fileToBeCompressed);
            var compressor = new LZ77();
            compressor.Compress(originalFilePath);
            var compressedFilePath = string.Format(@"C:\Users\speedy\Documents\TestFiles\UnitTests\{0}.lz", fileToBeCompressed);
            var decompressor = new LZ77();
            decompressor.Decompress(compressedFilePath);
            using (var testReader = new StreamReader(originalFilePath))
            {
                using (var originalReader = new StreamReader(string.Format(@"C:\Users\speedy\Documents\TestFiles\UnitTests\{0}", originalFile)))
                {
                    var originalData = new char[4096];
                    var decodedData = new char[4096];
                    for (int i = 0; i < testReader.BaseStream.Length; i += 4096)
                    {
                        testReader.ReadBlock(decodedData, 0, 4096);
                        originalReader.ReadBlock(originalData, 0, 4096);
                        CollectionAssert.AreEqual(originalData, decodedData);
                    }
                }
            }
        }

    }
}
