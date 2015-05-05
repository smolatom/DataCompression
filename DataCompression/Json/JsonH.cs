using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace DataCompression.Json
{
    /// <summary>
    /// JsonHCompressor implements JSONH compression algorithm. It can compress homogeneous collection of objects. 
    /// </summary>
    public class JsonH
    {
        /// <summary>
        /// Transforms homogeneous collection of object to JSONH array and produces it as a file.
        /// </summary>
        /// <param name="path">A path to the file contaiining JSON</param>
        /// <example>Transforms [{"a":12,"b":13},{"a":14,"b":15}] to [2,"a","b",12,13,14,15].</example>
        /// <returns>Data compressed as JSONH string.</returns>
        public void Compress(string path)
        {
            if (System.IO.File.Exists(path) && Path.GetExtension(path).ToLower() == ".js")
            {
                var resultString = new StringBuilder("[");
                compressFile(path, resultString);
                resultString.Append("]");

                writeToJsonHFile(path, resultString);
            }
        }

        /// <summary>
        /// Transforms JSONH array to homogeneous collection of objects and produces it as a file.
        /// </summary>
        /// <param name="path">A path to the file contaiining JSONH</param>
        /// <example>Transforms [2,"a","b",12,13,14,15] to [{"a":12,"b":13},{"a":14,"b":15}].</example>
        /// <returns>Data decompressed as JSON string.</returns>
        public void Decompress(string path)
        {
            if (System.IO.File.Exists(path) && Path.GetExtension(path).ToLower() == ".js")
            {
                var resultString = new StringBuilder("[");
                decompressFile(path, resultString);
                resultString.Append("]");

                writeToJsonFile(path, resultString);
            }
        }

        private void compressFile(string path, StringBuilder resultString)
        {
            using (var reader = new StreamReader(path))
            {
                var jsonString = reader.ReadToEnd();
                try
                {
                    var json = JArray.Parse(jsonString);
                    resultString.Append(compressDynamicJsonArray(json));
                }
                catch (Exception e)
                {
                    resultString.Append(e.Message);
                }
            }
        }

        private static void writeToJsonHFile(string path, StringBuilder resultString)
        {
            var fileName = Path.GetFileName(path);
            var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(path);
            var newPath = path.Replace(fileName, String.Format("{0}-jsonh.js", fileNameWithoutExtension));
            using (var writer = new StreamWriter(newPath))
            {
                writer.Write(resultString.ToString());
            }
        }

        private void decompressFile(string path, StringBuilder resultString)
        {

            using (var reader = new StreamReader(path))
            {
                var jsonHString = reader.ReadToEnd();
                try
                {
                    var jsonH = JArray.Parse(jsonHString);
                    resultString.Append(decompressDynamicJsonArray(jsonH));
                }
                catch (Exception ex)
                {
                    resultString.Append(ex.Message);
                } 
            }
        }

        private void writeToJsonFile(string path, StringBuilder resultString)
        {
            var fileName = Path.GetFileName(path);
            var newFileName = Path.GetFileNameWithoutExtension(path);
            var newPath = path.Replace("-jsonh.js", ".js");
            using (var writer = new StreamWriter(newPath))
            {
                writer.Write(resultString.ToString());
            }
        }

        private string compressDynamicJsonArray(ICollection<JToken> jsonArray)
        {
            var resultString = new StringBuilder();
            if (jsonArray.Count > 0)
            {
                var memberNames = appendMemberCountAndNamesToResult(jsonArray, resultString);
                appendValuesToResult(jsonArray, memberNames, resultString);
            }
            return resultString.ToString();
        }

        private static List<string> appendMemberCountAndNamesToResult(ICollection<JToken> jsonArray, StringBuilder resultString)
        {
            var memberNames = new List<string>();
            var first = jsonArray.First();
            var o = first as JObject;
            if (o != null)
            {
                var a = o;
                JEnumerable<JToken> members = first.Children();
                memberNames.AddRange(members.ToList().Select(x => ((JProperty)x).Name).ToList());
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

        private void appendValuesToResult(IEnumerable<JToken> json, List<string> patternMemberNames, StringBuilder resultString)
        {
            foreach (var item in json)
            {
                if (item is JObject)
                {
                    var memberNames = item.Children().ToList().Select(x => ((JProperty)x).Name).ToList();
                    foreach (var memberName in patternMemberNames)
                    {
                        resultString.Append(",");
                        if (memberNames.Count == patternMemberNames.Count() && memberNames.Contains(memberName))
                        {
                            var value = ((JObject)item).GetValue(memberName);
                            if (value is JObject)
                                resultString.Append(compressDynamicJsonObject(value));
                            else if (value is JArray)
                                resultString.Append(compressInnerDynamicJsonArray(value));
                            else
                                resultString.Append(compressNonDynamicObject((JValue)value));
                        }
                        else
                            throw new ArgumentException("JSON object doesn't have required pattern.");
                    }
                }
                else
                    throw new ArgumentException("JSON array doesn't contain only objects.");
            }
        }

        private string compressDynamicJsonObject(JToken jsonObject)
        {
            var memberNames = jsonObject.Children().ToList().Select(x => ((JProperty)x).Name).ToList();
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

        private string compressMember(JToken value)
        {
            string stringifiedMember;
            if (value.Type == JTokenType.Object)
                stringifiedMember = compressDynamicJsonObject(value);
            else if (value.Type == JTokenType.Array)
                stringifiedMember = compressInnerDynamicJsonArray(value);
            else
                stringifiedMember = compressNonDynamicObject(value);
            return stringifiedMember;
        }

        private string decompressDynamicJsonArray(JArray jsonH)
        {
            var result = new StringBuilder();
            if (jsonH.Count > 0)
            {
                result.Append(decompressArray(jsonH));
            }
            return result.ToString();
        }

        private StringBuilder decompressArray(JArray jsonHArray)
        {
            var result = new StringBuilder();
            int membersCount = getMembersCount(jsonHArray);
            if (jsonHArray.Count > membersCount + 1 || membersCount == 0)
            {
                var memberNames = getMemberNames(jsonHArray, membersCount);
                result.Append(decompressObjects(jsonHArray, membersCount, memberNames));
            }
            return result;
        }

        private StringBuilder decompressObjects(JArray jsonHArray, int membersCount, List<string> memberNames)
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

        private static bool serializedDataIsConsistent(JArray jsonHArray, int membersCount)
        {
            return (jsonHArray.Count - membersCount - 1) % membersCount == 0;
        }

        private static int getMembersCount(JArray jsonHArray)
        {
            var firstItem = jsonHArray.FirstOrDefault();
            return firstItem == null ? 0 : firstItem.Value<int>();
        }

        private StringBuilder decompressNotEmptyObjects(JArray jsonHArray, int membersCount, List<string> memberNames)
        {
            var result = new StringBuilder();
            for (int i = membersCount + 1; i < jsonHArray.Count; i += membersCount)
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

        private static List<string> getMemberNames(JArray jsonHArray, int membersCount)
        {
            var memberNames = new List<string>();
            for (int i = 0; i < membersCount; i++)
                memberNames.Add(jsonHArray[i + 1].ToString());
            return memberNames;
        }

        private string compressNonDynamicObject(JToken value)
        {
            string result;
            if (value.Type == JTokenType.String)
                result = String.Format(@"""{0}""", value);
            else if (value.Type == JTokenType.Boolean)
                result = value.ToString().ToLower();
            else if (value.Type == JTokenType.Null)
                result = "null";
            else
                result = value.ToString();
            return result;
        }

        private string decompressMember(string memberName, JToken memberValue)
        {
            return String.Format(@"""{0}"":{1},", memberName, decompressMemberValue(memberValue));
        }

        private string decompressMemberValue(JToken memberValue)
        {
            string stringifiedMember;
            if (memberValue.Type == JTokenType.Object)
                stringifiedMember = decompressDynamicJsonObject(memberValue);
            else if (memberValue.Type == JTokenType.Array)
                stringifiedMember = decompressInnerDynamicJsonArray(memberValue);
            else
                stringifiedMember = decompressNonDynamicObject(memberValue);
            return stringifiedMember;
        }

        private string decompressDynamicJsonObject(JToken jsonObject)
        {
            var memberNames = jsonObject.Children().ToList().Select(x => ((JProperty)x).Name).ToList();
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

        private string decompressNonDynamicObject(JToken memberValue)
        {
            string result;
            if (memberValue.Type == JTokenType.String)
                result = String.Format(@"""{0}""", memberValue);
            else if (memberValue.Type == JTokenType.Boolean)
                result = memberValue.ToString().ToLower();
            else if (memberValue.Type == JTokenType.Null)
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
