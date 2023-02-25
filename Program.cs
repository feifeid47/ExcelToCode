using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Feif
{
    public class Program
    {
        public static string DefaultTemplatePath { get => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Template"); }
        public static string SettingFilePath { get => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "setting.json"); }


        public static void Main(string[] args)
        {
            Setting.Default = JsonConvert.DeserializeObject<Setting>(File.ReadAllText(SettingFilePath));

            if (args == null || args.Length <= 0) { PrintHelp(); return; }

            var command = GetCommandLineArgs();

            if (!command.TryGetValue(Setting.Default.Arg1.Trim('-'), out var excelPath)) { PrintHelp(); return; }
            if (!command.TryGetValue(Setting.Default.Arg2.Trim('-'), out var codePath)) { PrintHelp(); return; }
            if (!command.TryGetValue(Setting.Default.Arg3.Trim('-'), out var dataPath)) { PrintHelp(); return; }
            command.TryGetValue(Setting.Default.Arg4.Trim('-'), out var cachePath);
            command.TryGetValue(Setting.Default.Arg5.Trim('-'), out var templatePath);
            command.TryGetValue(Setting.Default.Arg6.Trim('-'), out var suffix);

            if (string.IsNullOrEmpty(templatePath))
            {
                templatePath = DefaultTemplatePath;
            }
            if (string.IsNullOrEmpty(suffix))
            {
                suffix = Setting.Default.DefaultSuffix;
            }
            ExcelToCode.Generate(excelPath, codePath, dataPath, templatePath, suffix, cachePath);
        }

        public static Dictionary<string, string> GetCommandLineArgs()
        {
            var result = new Dictionary<string, string>();
            var enumerator = Environment.GetCommandLineArgs().ToList().GetEnumerator();
            var key = string.Empty;
            while (enumerator.MoveNext())
            {
                if (enumerator.Current.StartsWith("-"))
                {
                    key = enumerator.Current.TrimStart('-');
                    result[key] = null;
                }
                else if (result.ContainsKey(key) && string.IsNullOrEmpty(result[key]))
                {
                    result[key] = enumerator.Current;
                }
            }
            return result;
        }

        public static void PrintHelp()
        {
            Console.WriteLine($"{Setting.Default.Arg1}\t{Setting.Default.Arg1Info}");
            Console.WriteLine($"{Setting.Default.Arg2}\t{Setting.Default.Arg2Info}");
            Console.WriteLine($"{Setting.Default.Arg3}\t{Setting.Default.Arg3Info}");
            Console.WriteLine($"{Setting.Default.Arg4}\t{Setting.Default.Arg4Info}");
            Console.WriteLine($"{Setting.Default.Arg5}\t{Setting.Default.Arg5Info}");
            Console.WriteLine($"{Setting.Default.Arg6}\t{Setting.Default.Arg6Info}");
        }
    }
}