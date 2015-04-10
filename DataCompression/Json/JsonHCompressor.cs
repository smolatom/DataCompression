using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Helpers;
using NUnit.Framework;

namespace DataCompression.Json
{
    /// <summary>
    /// JsonHCompressor implements JSONH compression algorithm. It can compress homogeneous collection of objects. 
    /// </summary>
    [TestFixture]
    public class JsonHCompressor
    {
        [Theory]
        [TestCase(@"[{""a"":12,""b"":""13"",""c"":false}]")]
        [TestCase("}")]
        public void CompressionInputIsValidJson(string jsonString)
        {
            Assert.DoesNotThrow(() => Compress(jsonString));
        }

        [Theory]
        public void CompressionOutputIsJsonArray()
        {
            const string jsonString = "[{}]";
            var jsonHString = Compress(jsonString);
            Assert.IsInstanceOf(typeof(DynamicJsonArray), System.Web.Helpers.Json.Decode(jsonHString));
        }

        [Theory]
        [TestCase("[{}]", "[0]")]
        [TestCase("[]", "[]")]
        [TestCase(@"[{""a"":12}]", @"[1,""a"",12]")]
        [TestCase(@"[{""a"":12,""b"":13}]", @"[2,""a"",""b"",12,13]")]
        [TestCase(@"[{""a"":12,""b"":13},{""a"":14,""b"":15}]", @"[2,""a"",""b"",12,13,14,15]")]
        [TestCase(@"[{""a"":12},{""b"":12}]", "")]
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
            Assert.AreEqual(jsonHString, Compress(jsonString));
        }

        [Theory]
        public void DecompressionOutputIsJsonArray()
        {
            const string jsonH = "[]";
            var json = Decompress(jsonH);
            Assert.IsInstanceOf(typeof(DynamicJsonArray), System.Web.Helpers.Json.Decode(json));
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
            @"[{""a"":{""jahoda"":13,""kiwi"":""jahoda""},""b"":""hi"",""c"":true},{""a"":13,""b"":""hide"",""c"":false}]")]
        [TestCase(@"[3,""a"",""b"",""c"",{""jahoda"":13,""kiwi"":{""a"":13,""b"":""hide"",""c"":false}},""hi"",true]",
            @"[{""a"":{""jahoda"":13,""kiwi"":{""a"":13,""b"":""hide"",""c"":false}},""b"":""hi"",""c"":true}]")]
        public void DecompressionOutputEqualsToJson(string jsonH, string json)
        {
            Assert.AreEqual(json, Decompress(jsonH));
        }

        /// <summary>
        /// Transforms homogeneous collection of object to JSONH array.
        /// </summary>
        /// <param name="jsonString">JSON data as string.</param>
        /// <example>Transforms [{"a":12,"b":13},{"a":14,"b":15}] to [2,"a","b",12,13,14,15].</example>
        /// <returns>Data compressed as JSONH string.</returns>
        public string Compress(string jsonString)
        {
            var resultString = new StringBuilder("[");
            try
            {
                var json = System.Web.Helpers.Json.Decode(jsonString);
                resultString.Append(compressDynamicJsonArray(json));
            }
            catch (Exception)
            {
                return String.Empty;
            }
            resultString.Append("]");
            return resultString.ToString();
        }

        /// <summary>
        /// Transforms JSONH array to homogeneous collection of object.
        /// </summary>
        /// <param name="jsonHString">JSONH data as string.</param>
        /// <example>Transforms [2,"a","b",12,13,14,15] to [{"a":12,"b":13},{"a":14,"b":15}].</example>
        /// <returns>Data decompressed as JSON string.</returns>
        public string Decompress(string jsonHString)
        {
            var resultString = new StringBuilder("[");
            try
            {
                var jsonH = System.Web.Helpers.Json.Decode(jsonHString);
                resultString.Append(decompressDynamicJsonArray(jsonH));
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
            resultString.Append("]");
            return resultString.ToString();
        }

        private string compressDynamicJsonArray(dynamic json)
        {
            if (json is DynamicJsonArray)
            {
                var resultString = new StringBuilder();
                var jsonArray = json as DynamicJsonArray;
                if (jsonArray.Length > 0)
                {
                    var memberNames = appendMemberCountAndNamesToResult(jsonArray, resultString);
                    appendValuesToResult(json, memberNames, resultString);
                }
                return resultString.ToString();
            }
            throw new ArgumentException("JSON is not array.");
        }

        private static List<string> appendMemberCountAndNamesToResult(IEnumerable<object> jsonArray, StringBuilder resultString)
        {
            List<string> memberNames;
            var first = jsonArray.First();
            if (first is DynamicJsonObject)
            {
                memberNames = (first as DynamicJsonObject).GetDynamicMemberNames().ToList();
                resultString.Append(memberNames.Count);
                foreach (var memberName in memberNames)
                {
                    resultString.AppendFormat(@",""{0}""", memberName);
                }
            }
            else
            {
                throw new ArgumentException("JSON array doesn't contain object as first item.");
            }
            return memberNames;
        }

        private void appendValuesToResult(dynamic json, List<string> patternMemberNames, StringBuilder resultString)
        {
            foreach (var item in json)
            {
                if (item is DynamicJsonObject)
                {
                    var memberNames = (item as DynamicJsonObject).GetDynamicMemberNames().ToList();
                    foreach (var memberName in patternMemberNames)
                    {
                        resultString.Append(",");
                        if (memberNames.Count == patternMemberNames.Count() && memberNames.Contains(memberName))
                        {
                            var member = item[memberName];
                            if (member is DynamicJsonObject)
                                resultString.Append(compressDynamicJsonObject(member));
                            else if (member is DynamicJsonArray)
                                resultString.Append(compressInnerDynamicJsonArray(member));
                            else
                                resultString.Append(compressNonDynamicObject(member));
                        }
                        else
                            throw new ArgumentException("JSON object doesn't have required pattern.");
                    }
                }
                else
                    throw new ArgumentException("JSON array doesn't contain only objects.");
            }
        }

        private string compressDynamicJsonObject(dynamic jsonObject)
        {
            var memberNames = jsonObject.GetDynamicMemberNames();
            var resultString = new StringBuilder("{");
            foreach (var memberName in memberNames)
            {
                var memberValue = jsonObject[memberName];
                resultString.Append(String.Format(@"""{0}"":", memberName));
                var stringifiedMember = compressMember(memberValue);
                resultString.AppendFormat("{0},", stringifiedMember);
            }
            if (resultString[resultString.Length - 1] == ',')
                resultString.Remove(resultString.Length - 1, 1);
            resultString.Append("}");
            return resultString.ToString();
        }

        private string compressInnerDynamicJsonArray(dynamic jsonArray)
        {
            var resultString = new StringBuilder("[");
            foreach (var item in jsonArray)
            {
                var stringifiedItem = compressMember(item);
                resultString.AppendFormat("{0},", stringifiedItem);
            }
            removeLastComma(resultString);
            resultString.Append("]");
            return resultString.ToString();
        }

        private string compressMember(dynamic member)
        {
            string stringifiedMember;
            if (member is DynamicJsonObject)
                stringifiedMember = compressDynamicJsonObject(member);
            else if (member is DynamicJsonArray)
                stringifiedMember = compressInnerDynamicJsonArray(member);
            else
                stringifiedMember = compressNonDynamicObject(member);
            return stringifiedMember;
        }

        private string decompressDynamicJsonArray(dynamic jsonH)
        {
            if (jsonH is DynamicJsonArray)
            {
                var result = new StringBuilder();
                var jsonHArray = jsonH as DynamicJsonArray;
                if (jsonHArray.Length > 0)
                {
                    result.Append(decompressArray(jsonHArray));
                }
                return result.ToString();
            }
            throw new ArgumentException("JSONH is not array.");
        }

        private StringBuilder decompressArray(DynamicJsonArray jsonHArray)
        {
            var result = new StringBuilder();
            int membersCount = getMembersCount(jsonHArray);
            if (jsonHArray.Length > membersCount + 1 || membersCount == 0)
            {
                var memberNames = getMemberNames(jsonHArray, membersCount);
                result.Append(decompressObjects(jsonHArray, membersCount, memberNames));
            }
            return result;
        }

        private StringBuilder decompressObjects(DynamicJsonArray jsonHArray, int membersCount, List<string> memberNames)
        {
            var result = new StringBuilder();
            if (dataContainOnlyEmptyObject(membersCount))
                result.Append("{}");
            else if (serializedDataIsConsistent(jsonHArray, membersCount))
            {
                result.Append(decompressNotEmptyObjects(jsonHArray, membersCount, memberNames));
            }
            else
                throw new IndexOutOfRangeException("JSONH array doesn't contain enough items.");
            return result;
        }

        private static bool dataContainOnlyEmptyObject(int membersCount)
        {
            return membersCount == 0;
        }

        private static bool serializedDataIsConsistent(DynamicJsonArray jsonHArray, int membersCount)
        {
            return (jsonHArray.Length - membersCount - 1) % membersCount == 0;
        }

        private static dynamic getMembersCount(DynamicJsonArray jsonHArray)
        {
            return jsonHArray[0];
        }

        private StringBuilder decompressNotEmptyObjects(DynamicJsonArray jsonHArray, int membersCount, List<string> memberNames)
        {
            var result = new StringBuilder();
            for (int i = membersCount + 1; i < jsonHArray.Length; i += membersCount)
            {
                result.Append("{");
                for (int j = 0; j < membersCount; j++)
                {
                    result.Append(decompressMember(memberNames[j], jsonHArray[i + j]));
                }
                removeLastComma(result);
                result.Append("},");
            }
            removeLastComma(result);
            return result;
        }

        private static List<string> getMemberNames(DynamicJsonArray jsonHArray, int membersCount)
        {
            var memberNames = new List<string>();
            for (int i = 0; i < membersCount; i++)
                memberNames.Add(jsonHArray[i + 1].ToString());
            return memberNames;
        }

        private string compressNonDynamicObject(dynamic member)
        {
            string result;
            if (member is string)
                result = String.Format(@"""{0}""", member);
            else if (member is bool)
                result = member.ToString().ToLower();
            else if (member == null)
                result = "null";
            else
                result = member.ToString();
            return result;
        }

        private string decompressMember(string memberName, dynamic memberValue)
        {
            return String.Format(@"""{0}"":{1},", memberName, decompressMemberValue(memberValue));
        }

        private string decompressMemberValue(dynamic memberValue)
        {
            string stringifiedMember;
            if (memberValue is DynamicJsonObject)
                stringifiedMember = decompressDynamicJsonObject(memberValue);
            else if (memberValue is DynamicJsonArray)
                stringifiedMember = decompressInnerDynamicJsonArray(memberValue);
            else
                stringifiedMember = decompressNonDynamicObject(memberValue);
            return stringifiedMember;
        }

        private string decompressDynamicJsonObject(dynamic jsonObject)
        {
            var memberNames = jsonObject.GetDynamicMemberNames();
            var resultString = new StringBuilder("{");
            foreach (var memberName in memberNames)
            {
                var memberValue = jsonObject[memberName];
                resultString.Append(String.Format(@"""{0}"":", memberName));
                var stringifiedMember = compressMember(memberValue);
                resultString.AppendFormat("{0},", stringifiedMember);
            }
            if (resultString[resultString.Length - 1] == ',')
                resultString.Remove(resultString.Length - 1, 1);
            resultString.Append("}");
            return resultString.ToString();
        }

        private string decompressInnerDynamicJsonArray(dynamic jsonArray)
        {
            var resultString = new StringBuilder("[");
            foreach (var item in jsonArray)
            {
                var stringifiedItem = decompressMemberValue(item);
                resultString.AppendFormat("{0},", stringifiedItem);
            }
            removeLastComma(resultString);
            resultString.Append("]");
            return resultString.ToString();
        }

        private string decompressNonDynamicObject(object memberValue)
        {
            string result;
            if (memberValue is string)
                result = String.Format(@"""{0}""", memberValue);
            else if (memberValue is bool)
                result = memberValue.ToString().ToLower();
            else if (memberValue == null)
                result = "null";
            else
                result = memberValue.ToString();
            return result;
        }

        private static void removeLastComma(StringBuilder result)
        {
            if (result[result.Length - 1] == ',')
                result.Remove(result.Length - 1, 1);
        }
    }
}
