using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Helpers;

namespace DataCompression
{
    [TestFixture]
    public class JsonHCompressor
    {
        [Theory]
        [TestCase(@"[{""a"":12,""b"":""13"",""c"":false}]")]
        [TestCase("}")]
        public void JsonHCompressionInputIsValidJson(string jsonString)
        {
            Assert.DoesNotThrow(() => ToJsonH(jsonString));
        }

        [Theory]
        public void JsonHCompressionOutputIsJsonArray()
        {
            const string jsonString = "{}";
            var jsonHString = ToJsonH(jsonString);
            Assert.IsInstanceOf(typeof(DynamicJsonArray), Json.Decode(jsonHString));
        }

        [Theory]
        [TestCase(@"[{""a"":12}]", @"[1,""a"",12]")]
        [TestCase(@"[{""a"":[13,14,""jahoda""], ""b"":""hi"", ""c"":true}]",
            @"[3,""a"",""b"",""c"",[13,14,""jahoda""],""hi"",true]")]
        [TestCase(
            @"[{""a"":{""jahoda"":13, ""kiwi"":""jahoda""}, ""b"":""hi"", ""c"":true},{""a"":13, ""b"":""hide"", ""c"":false}]",
            @"[3,""a"",""b"",""c"",{""jahoda"":13,""kiwi"":""jahoda""},""hi"",true,13,""hide"",false]")]
        [TestCase(
            @"[{""a"":{""jahoda"":13, ""kiwi"":{""a"":13, ""b"":""hide"", ""c"":false}}, ""b"":""hi"", ""c"":true}]",
            @"[3,""a"",""b"",""c"",{""jahoda"":13,""kiwi"":{""a"":13,""b"":""hide"",""c"":false}},""hi"",true]")]
        public void JsonHCompressionOutputEqualsToJsonH(string jsonString, string jsonHString)
        {
            Assert.AreEqual(jsonHString, ToJsonH(jsonString));
        }

        public string ToJsonH(string jsonString)
        {
            var resultString = new StringBuilder("[");
            try
            {
                var json = Json.Decode(jsonString);
                resultString.Append(stringifyDynamicJsonArray(json));
            }
            catch (Exception)
            {
                return String.Empty;
            }
            resultString.Append("]");
            return resultString.ToString();
        }

        private string stringifyDynamicJsonArray(dynamic json)
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

        private void appendValuesToResult(dynamic json, List<string> memberNames, StringBuilder resultString)
        {
            foreach (var item in json)
            {
                if (item is DynamicJsonObject)
                {
                    foreach (var memberName in memberNames)
                    {
                        resultString.Append(",");
                        var member = item[memberName];
                        if (member is DynamicJsonObject)
                            resultString.Append(stringifyDynamicJsonObject(member));
                        else if (member is DynamicJsonArray)
                            resultString.Append(stringifyInnerDynamicJsonArray(member));
                        else
                            resultString.Append(stringifyNonDynamicObject(member));
                    }
                }
                else
                    throw new ArgumentException("JSON array doesn't contain only objects.");
            }
        }

        private string stringifyDynamicJsonObject(dynamic jsonObject)
        {
            var memberNames = jsonObject.GetDynamicMemberNames();
            var resultString = new StringBuilder("{");
            foreach (var memberName in memberNames)
            {
                var memberValue = jsonObject[memberName];
                resultString.Append(String.Format(@"""{0}"":", memberName));
                var stringifiedMember = stringifyMember(memberValue);
                resultString.AppendFormat("{0},", stringifiedMember);
            }
            if (resultString[resultString.Length - 1] == ',')
                resultString.Remove(resultString.Length - 1, 1);
            resultString.Append("}");
            return resultString.ToString();
        }

        private string stringifyInnerDynamicJsonArray(dynamic jsonArray)
        {
            var resultString = new StringBuilder("[");
            foreach (var item in jsonArray)
            {
                var stringifiedItem = stringifyMember(item);
                resultString.AppendFormat("{0},", stringifiedItem);
            }
            if (resultString[resultString.Length - 1] == ',')
                resultString.Remove(resultString.Length - 1, 1);
            resultString.Append("]");
            return resultString.ToString();
        }

        private string stringifyMember(dynamic member)
        {
            string stringifiedMember;
            if (member is DynamicJsonObject)
                stringifiedMember = stringifyDynamicJsonObject(member);
            else if (member is DynamicJsonArray)
                stringifiedMember = stringifyInnerDynamicJsonArray(member);
            else
                stringifiedMember = stringifyNonDynamicObject(member);
            return stringifiedMember;
        }

        private string stringifyNonDynamicObject(dynamic member)
        {
            if (member is string)
                return String.Format(@"""{0}""", member);
            if (member is bool)
                return member.ToString().ToLower();
            return member.ToString();
        }
    }
}
