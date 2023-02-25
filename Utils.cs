using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;

namespace Feif
{
    public static class Utils
    {
        private static SHA256 sha256 = SHA256.Create();

        public static IEnumerable<string> Search(string path, Predicate<string> predicate)
        {
            foreach (var directory in Directory.GetDirectories(path))
            {
                foreach (var item in Search(directory, predicate))
                {
                    yield return item;
                }
            }
            foreach (var item in Directory.GetFiles(path).Where(item => predicate(item)))
            {
                yield return item;
            }
        }

        public static string GetNamespace(string type)
        {
            if (type.Contains('.'))
            {
                return type.Replace(type.Split('.').Last(), string.Empty).TrimEnd('.');
            }
            else
            {
                return Setting.Default.DefaultNamespace;
            }
        }

        public static string GetFileHash(string path)
        {
            using var stream = File.OpenRead(path);
            return BitConverter.ToString(sha256.ComputeHash(stream)).Replace("-", string.Empty);
        }

        public static string GetClass(string type)
        {
            if (type.Contains('.'))
            {
                return type.Split('.').Last();
            }
            else
            {
                return type;
            }
        }

        public static string GetStringString(string source)
        {
            var result = source.Replace("\\n", "\n")
                               .Replace("\\r", "\r")
                               .Replace("\\t", "\t")
                               .Replace("\\\"", "\"")
                               .Replace("\\\\", "\\")
                               .Replace("\\b", "\b")
                               .Replace("\\f", "\f")
                               .Replace("\\a", "\a")
                               .Replace("\\v", "\v");
            return result.Substring(1, result.Length - 2);
        }

        public static List<string> GetStringList(string source)
        {
            var list = new List<string>();
            int count = 0;
            bool skip = false;
            int index = 0;
            for (int i = 0; i < source.Length; i++)
            {
                var item = source[i];
                if (skip)
                {
                    skip = false;
                    continue;
                }

                if (item == '\\') skip = true;

                if (item == '\"')
                {
                    if (++count % 2 == 0)
                    {
                        list.Add(Utils.GetStringString(source.Substring(index, i - index + 1)));
                    }
                    else
                    {
                        index = i;
                    }
                }
            }
            return list;
        }

        public static List<KeyValuePair<string, string>> GetKeyValues(string source)
        {
            var list = new List<KeyValuePair<string, string>>();

            int count = 0;
            bool skip = false;
            int stringIndex = 0;
            var queue = new Queue<string>();
            bool onString = false;
            bool getKey = true;

            int keyValueIndex = 0;

            for (int i = 0; i < source.Length; i++)
            {
                var item = source[i];

                if (skip)
                {
                    skip = false;
                    continue;
                }
                if (item == '\\')
                {
                    skip = true;
                }

                if (!onString)
                {
                    if (item == '|')
                    {
                        if (getKey)
                        {
                            keyValueIndex = i + 1;
                        }
                        if (queue.Count == 1)
                        {
                            queue.Enqueue(source.Substring(keyValueIndex, i - keyValueIndex));
                            getKey = !getKey;
                            keyValueIndex = i + 1;
                            if (queue.Count >= 2)
                            {
                                list.Add(new KeyValuePair<string, string>(queue.Dequeue(), queue.Dequeue()));
                            }
                        }
                    }
                    if (item == ':')
                    {
                        if (!getKey)
                        {
                            keyValueIndex = i + 1;
                        }
                        if (queue.Count == 0)
                        {
                            queue.Enqueue(source.Substring(keyValueIndex, i - keyValueIndex));
                            getKey = !getKey;
                            keyValueIndex = i + 1;
                            if (queue.Count >= 2)
                            {
                                list.Add(new KeyValuePair<string, string>(queue.Dequeue(), queue.Dequeue()));
                            }
                        }
                    }
                }

                if (item == '\"')
                {
                    if (++count % 2 == 0)
                    {
                        onString = false;
                        queue.Enqueue(source.Substring(stringIndex, i - stringIndex + 1));
                        getKey = !getKey;
                        if (queue.Count >= 2)
                        {
                            list.Add(new KeyValuePair<string, string>(queue.Dequeue(), queue.Dequeue()));
                        }
                    }
                    else
                    {
                        onString = true;
                        stringIndex = i;
                    }
                }
            }
            return list;
        }
    }
}