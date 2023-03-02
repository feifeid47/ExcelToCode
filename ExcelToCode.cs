using System.Text;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ClosedXML.Excel;
using Newtonsoft.Json;
using System.Threading.Tasks;

namespace Feif
{
    public class ExcelToCode
    {
        private static HashSet<string> supportDefine = new HashSet<string>()
        {
            "int",
            "float",
            "double",
            "long",
            "byte",
            "bool",
            "uint",
            "ushort",
            "ulong",
            "sbyte",
            "string",
        };

        private static Dictionary<string, Type> defineTypeDic = new Dictionary<string, Type>()
        {
            {"int",typeof(int)},
            {"float",typeof(float)},
            {"double",typeof(double)},
            {"long",typeof(long)},
            {"byte",typeof(byte)},
            {"bool",typeof(bool)},
            {"uint",typeof(uint)},
            {"ushort",typeof(ushort)},
            {"ulong",typeof(ulong)},
            {"sbyte",typeof(sbyte)},
            {"string",typeof(string)},
        };

        private static Dictionary<string, string> fileCache = new Dictionary<string, string>();
        private static Dictionary<string, string> typeCache = new Dictionary<string, string>();
        private static Dictionary<(string type, string value), object> valueCache = new Dictionary<(string type, string value), object>();

        public static void Generate(string excelPath, string codePath, string dataPath, string templatePath, string suffix, string cachePath)
        {
            try
            {
                var startTime = DateTime.Now;

                if (!File.Exists(Path.Combine(templatePath, Setting.Default.TemplateFileName1)))
                {
                    Console.WriteLine(Setting.Default.Lang1.Replace("{value}", Path.Combine(templatePath, Setting.Default.TemplateFileName1)));
                    return;
                }
                if (!File.Exists(Path.Combine(templatePath, Setting.Default.TemplateFileName2)))
                {
                    Console.WriteLine(Setting.Default.Lang1.Replace("{value}", Path.Combine(templatePath, Setting.Default.TemplateFileName2)));
                    return;
                }
                if (!File.Exists(Path.Combine(templatePath, Setting.Default.TemplateFileName3)))
                {
                    Console.WriteLine(Setting.Default.Lang1.Replace("{value}", Path.Combine(templatePath, Setting.Default.TemplateFileName3)));
                    return;
                }
                if (!File.Exists(Path.Combine(templatePath, Setting.Default.TemplateFileName4)))
                {
                    Console.WriteLine(Setting.Default.Lang1.Replace("{value}", Path.Combine(templatePath, Setting.Default.TemplateFileName4)));
                    return;
                }
                if (!string.IsNullOrEmpty(cachePath))
                {
                    Directory.CreateDirectory(cachePath);
                    if (File.Exists(Path.Combine(cachePath, Setting.Default.CatalogName)))
                    {
                        fileCache = JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(Path.Combine(cachePath, Setting.Default.CatalogName)));
                    }
                }

                string dataFieldTemplate = File.ReadAllText(Path.Combine(templatePath, Setting.Default.TemplateFileName1));
                string tableFieldTemplate = File.ReadAllText(Path.Combine(templatePath, Setting.Default.TemplateFileName2));
                string dataTemplate = File.ReadAllText(Path.Combine(templatePath, Setting.Default.TemplateFileName3));
                using var reader = new StringReader(File.ReadAllText(Path.Combine(templatePath, Setting.Default.TemplateFileName4)));

                var tableFileName = reader.ReadLine().Replace("FILENAME=", string.Empty).Trim('#');
                var tableTemplate = reader.ReadToEnd();

                var tasks = new List<Task>();
                var tablesFields = new StringBuilder();
                Directory.CreateDirectory(dataPath);
                Directory.CreateDirectory(codePath);

                foreach (var path in Utils.Search(excelPath, item => item.EndsWith(".xls") || item.EndsWith(".xlsx")))
                {
                    var fileHash = Utils.GetFileHash(path);
                    if (fileCache.ContainsKey(fileHash))
                    {
                        if (File.Exists(Path.Combine(cachePath, $"C{fileHash}")) && File.Exists(Path.Combine(cachePath, $"D{fileHash}")))
                        {
                            var fileName = Utils.GetClass(fileCache[fileHash]);
                            File.Copy(Path.Combine(cachePath, $"C{fileHash}"), Path.Combine(codePath, fileName + ".cs"), true);
                            File.Copy(Path.Combine(cachePath, $"D{fileHash}"), Path.Combine(dataPath, fileName + suffix), true);
                            tablesFields.AppendLine(tableFieldTemplate.Replace(Setting.Default.ReplaceType, fileCache[fileHash]).Replace(Setting.Default.ReplaceName, fileName));
                            continue;
                        }
                    }

                    var table = CreateTable(path);
                    var data = JsonConvert.SerializeObject(table);
                    tasks.Add(File.WriteAllBytesAsync(Path.Combine(dataPath, Utils.GetClass(table.Type) + suffix), Encoding.UTF8.GetBytes(data)));

                    var script = dataTemplate.Replace(Setting.Default.ReplaceClass, Utils.GetClass(table.Type)).Replace(Setting.Default.ReplaceNamespace, Utils.GetNamespace(table.Type));
                    var builder = new StringBuilder();
                    foreach (var item in table.Fields)
                    {
                        builder.AppendLine(dataFieldTemplate.Replace(Setting.Default.ReplaceType, item.Value.Define).Replace(Setting.Default.ReplaceName, item.Key));
                    }
                    tasks.Add(File.WriteAllTextAsync(Path.Combine(codePath, Utils.GetClass(table.Type) + ".cs"), script.Replace(Setting.Default.ReplaceProperties, builder.ToString().TrimEnd())));
                    if (!string.IsNullOrEmpty(cachePath))
                    {
                        tasks.Add(File.WriteAllTextAsync(Path.Combine(cachePath, $"C{fileHash}"), script.Replace(Setting.Default.ReplaceProperties, builder.ToString().TrimEnd())));
                        tasks.Add(File.WriteAllBytesAsync(Path.Combine(cachePath, $"D{fileHash}"), Encoding.UTF8.GetBytes(data)));
                    }
                    fileCache[fileHash] = table.Type;

                    tablesFields.AppendLine(tableFieldTemplate.Replace(Setting.Default.ReplaceType, table.Type).Replace(Setting.Default.ReplaceName, Utils.GetClass(table.Type)));
                }
                tasks.Add(File.WriteAllTextAsync(Path.Combine(codePath, tableFileName + ".cs"), tableTemplate.Replace(Setting.Default.ReplaceProperties, tablesFields.ToString())));

                Task.WaitAll(tasks.ToArray());
                if (!string.IsNullOrEmpty(cachePath))
                {
                    File.WriteAllText(Path.Combine(cachePath, Setting.Default.CatalogName), JsonConvert.SerializeObject(fileCache));
                }
                Console.WriteLine(Setting.Default.Lang2.Replace("{value}", (DateTime.Now - startTime).TotalSeconds.ToString("f2")));
            }
            catch (Exception e)
            {
                Console.WriteLine(Setting.Default.Lang3.Replace("{value}", e.Message));
            }
        }

        public static ExcelTable CreateTable(string path)
        {
            var table = new ExcelTable();
            using var workbook = new XLWorkbook(path);
            var sheet = workbook.Worksheets.First();
            bool onTable = false;
            bool onField = false;
            bool onType = false;
            bool onValue = false;
            bool onComment = false;
            int commentRow = 0;
            int index = 0;
            var filedNames = new List<string>();
            foreach (var cell in sheet.Cells())
            {
                if (cell.IsEmpty()) continue;
                if (onComment && cell.Address.RowNumber == commentRow) continue;

                var cellValue = cell.GetValue<string>().Trim();
                if (cellValue == Setting.Default.Key1) { onTable = true; onField = onType = onValue = onComment = false; continue; }
                if (cellValue == Setting.Default.Key2) { onField = true; onTable = onType = onValue = onComment = false; continue; }
                if (cellValue == Setting.Default.Key3) { onType = true; onTable = onField = onValue = onComment = false; continue; }
                if (cellValue == Setting.Default.Key4) { onValue = true; onTable = onField = onType = onComment = false; continue; }
                if (cellValue == Setting.Default.Key5)
                {
                    onComment = true;
                    commentRow = cell.Address.RowNumber;
                    continue;
                }
                if (onTable) table.Type = $"{Utils.GetNamespace(cellValue)}.{Utils.GetClass(cellValue)}";
                if (onField)
                {
                    table.Fields.Add(cellValue, new TypeValues());
                    filedNames.Add(cellValue);
                }
                if (onType)
                {
                    var field = table.Fields[filedNames[index++ % table.Fields.Count]];
                    field.Define = cellValue.Replace(" ", string.Empty).Replace(",", ", ");
                    if (!TryGetType(field.Define, out field.Type))
                    {
                        var info = new FileInfo(path);
                        Console.WriteLine(Setting.Default.Lang4
                            .Replace("{value1}", info.Name)
                            .Replace("{value2}", cell.Address.ToString())
                            .Replace("{value3}", field.Define));
                        Environment.Exit(1);
                    }

                }
                if (onValue)
                {
                    var field = table.Fields[filedNames[index++ % table.Fields.Count]];
                    if (!TryGetValue(cellValue, field.Type, out var value))
                    {
                        var info = new FileInfo(path);
                        Console.WriteLine(Setting.Default.Lang5
                            .Replace("{value1}", info.Name)
                            .Replace("{value2}", cell.Address.ToString())
                            .Replace("{value3}", cellValue)
                            .Replace("{value4}", field.Type));
                        Environment.Exit(1);
                    }
                    field.Values.Add(value);
                }
            }
            return table;
        }

        public static bool TryGetType(string define, out string result)
        {
            if (typeCache.TryGetValue(define, out result)) return true;

            define = define.Trim().Replace(", ", ",");

            if (define.Contains("Dictionary"))
            {
                var genericDefine = define.Replace("Dictionary", string.Empty).Replace("<", string.Empty).Replace(">", string.Empty).Split(',');

                if (genericDefine == null || genericDefine.Length < 2) return false;

                if (genericDefine.Any(item => !supportDefine.Contains(item))) return false;

                result = typeof(Dictionary<,>).MakeGenericType(defineTypeDic[genericDefine[0]], defineTypeDic[genericDefine[1]]).ToString();
                typeCache[define] = result;
                return true;
            }
            if (define.Contains("List"))
            {
                var genericType = define.Replace("List", string.Empty).Replace("<", string.Empty).Replace(">", string.Empty);

                if (!supportDefine.Contains(genericType)) return false;

                result = typeof(List<>).MakeGenericType(defineTypeDic[genericType]).ToString();
                typeCache[define] = result;
                return true;
            }
            if (define.Contains("HashSet"))
            {
                var genericType = define.Replace("HashSet", string.Empty).Replace("<", string.Empty).Replace(">", string.Empty);

                if (!supportDefine.Contains(genericType)) return false;

                result = typeof(HashSet<>).MakeGenericType(defineTypeDic[genericType]).ToString();
                typeCache[define] = result;
                return true;
            }

            if (defineTypeDic.ContainsKey(define))
            {
                result = defineTypeDic[define].ToString();
                typeCache[define] = result;
                return true;
            }

            return false;
        }

        public static bool TryGetValue(string cellValue, string fieldType, out object result)
        {
            if (valueCache.TryGetValue((fieldType, cellValue), out result)) return true;

            try
            {
                var type = Type.GetType(fieldType);

                if (!type.IsGenericType)
                {
                    result = ChangeType(cellValue, type);
                    valueCache[(fieldType, cellValue)] = result;
                    return true;
                }

                if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
                {
                    var genericType = type.GetGenericArguments()[0];
                    if (genericType != typeof(string))
                    {
                        result = cellValue.Split('|').Select(item => ChangeType(item, genericType)).ToList();
                        valueCache[(fieldType, cellValue)] = result;
                        return true;
                    }
                    result = Utils.GetStringList(cellValue);
                    valueCache[(fieldType, cellValue)] = result;
                    return true;
                }

                if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(HashSet<>))
                {
                    var genericType = type.GetGenericArguments()[0];
                    if (genericType != typeof(string))
                    {
                        result = cellValue.Split('|').Select(item => ChangeType(item, genericType)).ToHashSet();
                        valueCache[(fieldType, cellValue)] = result;
                        return true;
                    }
                    result = Utils.GetStringList(cellValue).ToHashSet();
                    valueCache[(fieldType, cellValue)] = result;
                    return true;
                }

                if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Dictionary<,>))
                {
                    var genericTypes = type.GetGenericArguments();

                    if (genericTypes[0] != typeof(string) && genericTypes[1] != typeof(string))
                    {
                        result = cellValue.Split('|').ToDictionary(item => ChangeType(item.Split(':')[0], genericTypes[0]), item => ChangeType(item.Split(':')[1], genericTypes[1]));
                        valueCache[(fieldType, cellValue)] = result;
                        return true;
                    }
                    if (genericTypes[0] == typeof(string) && genericTypes[1] != typeof(string))
                    {
                        result = Utils.GetKeyValues(cellValue).ToDictionary(item => Utils.GetStringString(item.Key), item => ChangeType(item.Value, genericTypes[1]));
                        valueCache[(fieldType, cellValue)] = result;
                        return true;
                    }
                    if (genericTypes[0] != typeof(string) && genericTypes[1] == typeof(string))
                    {
                        result = Utils.GetKeyValues(cellValue).ToDictionary(item => ChangeType(item.Key, genericTypes[0]), item => Utils.GetStringString(item.Value));
                        valueCache[(fieldType, cellValue)] = result;
                        return true;
                    }
                    if (genericTypes[0] == typeof(string) && genericTypes[1] == typeof(string))
                    {
                        result = Utils.GetKeyValues(cellValue).ToDictionary(item => Utils.GetStringString(item.Key), item => Utils.GetStringString(item.Value));
                        valueCache[(fieldType, cellValue)] = result;
                        return true;
                    }
                }
                return false;
            }
            catch
            {
                return false;
            }
        }

        public static object ChangeType(string source, Type type)
        {
            source = source.Trim();
            if (source.ToString().StartsWith("0X", true, System.Globalization.CultureInfo.CurrentCulture))
            {
                if (type == typeof(byte)) return Convert.ToByte(source, 16);
                if (type == typeof(short)) return Convert.ToInt16(source, 16);
                if (type == typeof(int)) return Convert.ToInt32(source, 16);
                if (type == typeof(long)) return Convert.ToInt64(source, 16);

                if (type == typeof(sbyte)) return Convert.ToSByte(source, 16);
                if (type == typeof(ushort)) return Convert.ToUInt16(source, 16);
                if (type == typeof(uint)) return Convert.ToUInt32(source, 16);
                if (type == typeof(ulong)) return Convert.ToUInt64(source, 16);
            }
            return Convert.ChangeType(source, type);
        }
    }
}