using System.IO;
using DataCompression.Json;
using NUnit.Framework;

namespace UnitTestProject
{
    [TestFixture]
    class TestJsonH
    {
        [Theory]
        [TestCase(@"[{""a"":12,""b"":""13"",""c"":false}]")]
        [TestCase("}")]
        public void CompressionInputIsValidJson(string jsonString)
        {
            var compressor = new JsonH();
            Assert.DoesNotThrow(() => compressor.Compress(jsonString));
        }

        [Theory]
        [TestCase("[{}]", "[0]")]
        [TestCase("[]", "[]")]
        [TestCase(@"[{""a"":12}]", @"[1,""a"",12]")]
        [TestCase(@"[{""a"":12,""b"":13}]", @"[2,""a"",""b"",12,13]")]
        [TestCase(@"[{""a"":12,""b"":13},{""a"":14,""b"":15}]", @"[2,""a"",""b"",12,13,14,15]")]
        [TestCase(@"[{""a"":12},{""b"":12}]", "[JSON object doesn't have required pattern.]")]
        [TestCase(@"[{""a"":null},{""a"":12}]", @"[1,""a"",null,12]")]
        [TestCase(@"[{""a"":[13,14,""jahoda""], ""b"":""hi"", ""c"":true}]",
            @"[3,""a"",""b"",""c"",[13,14,""jahoda""],""hi"",true]")]
        [TestCase(
            @"[{""a"":{""jahoda"":13, ""kiwi"":""jahoda""}, ""b"":""hi"", ""c"":true},{""a"":13, ""b"":""hide"", ""c"":false}]",
            @"[3,""a"",""b"",""c"",{""jahoda"":13,""kiwi"":""jahoda""},""hi"",true,13,""hide"",false]")]
        [TestCase(
            @"[{""a"":{""jahoda"":13, ""kiwi"":{""a"":13, ""b"":""hide"", ""c"":false}}, ""b"":""hi"", ""c"":true}]",
            @"[3,""a"",""b"",""c"",{""jahoda"":13,""kiwi"":{""a"":13,""b"":""hide"",""c"":false}},""hi"",true]")]
        public void CompressionOutputEqualsToJsonH(string jsonString, string jsonHString)
        {
            const string jsonFile = @"C:\Users\speedy\Documents\TestFiles\UnitTests\CompressionTest.js";
            using (var writer = new StreamWriter(jsonFile))
            {
                writer.Write(jsonString);
            }
            var compressor = new JsonH();
            compressor.Compress(jsonFile);
            const string jsonHFile = @"C:\Users\speedy\Documents\TestFiles\UnitTests\CompressionTest-jsonh.js";
            using (var reader = new StreamReader(jsonHFile))
            {
                var jsonh = reader.ReadToEnd();
                Assert.AreEqual(jsonHString, jsonh);
            }
        }

        [Theory]
        [TestCase("[]", "[]")]
        [TestCase("[0]", "[{}]")]
        [TestCase(@"[1,""a"",12]", @"[{""a"":12}]")]
        [TestCase(@"[2,""a"",""b"",12,13]", @"[{""a"":12,""b"":13}]")]
        [TestCase(@"[2,""a"",""b"",12,13,14,15]", @"[{""a"":12,""b"":13},{""a"":14,""b"":15}]")]
        [TestCase(@"[1,""a"",null,12]", @"[{""a"":null},{""a"":12}]")]
        [TestCase(@"[3,""a"",""b"",""c"",[13,14,""jahoda""],""hi"",true]",
            @"[{""a"":[13,14,""jahoda""],""b"":""hi"",""c"":true}]")]
        [TestCase(@"[3,""a"",""b"",""c"",{""jahoda"":13,""kiwi"":""jahoda""},""hi"",true,13,""hide"",false]",
            @"[{""a"":{""jahoda"":13,""kiwi"":""jahoda""},""b"":""hi"",""c"":true},{""a"":13,""b"":""hide"",""c"":false}]"
            )]
        [TestCase(@"[3,""a"",""b"",""c"",{""jahoda"":13,""kiwi"":{""a"":13,""b"":""hide"",""c"":false}},""hi"",true]",
            @"[{""a"":{""jahoda"":13,""kiwi"":{""a"":13,""b"":""hide"",""c"":false}},""b"":""hi"",""c"":true}]")]
        public void DecompressionOutputEqualsToJson(string jsonH, string json)
        {
            const string jsonHFile = @"C:\Users\speedy\Documents\TestFiles\UnitTests\DecompressionTest-jsonh.js";
            using (var writer = new StreamWriter(jsonHFile))
            {
                writer.Write(jsonH);
            }
            var decompressor = new JsonH();
            decompressor.Decompress(jsonHFile);
            const string jsonFile = @"C:\Users\speedy\Documents\TestFiles\UnitTests\DecompressionTest.js";
            using (var reader = new StreamReader(jsonFile))
            {
                var decompressedJson = reader.ReadToEnd();
                Assert.AreEqual(json, decompressedJson);
            }
        }

    }
}
