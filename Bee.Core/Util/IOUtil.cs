using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Bee.Util
{
    public static class IOUtil
    {
        public static string GetFileContent(string filePath)
        {
            return GeneralUtil.CatchAll<string>(delegate
            {
                if (!File.Exists(filePath))
                {
                    return null;
                }
                StreamReader reader = new StreamReader(filePath, Encoding.Default);
                string str = reader.ReadToEnd();
                reader.Close();
                return str;
            });
        }

        public static void SafeCreateDirectory(string path)
        {
            GeneralUtil.CatchAll(delegate
            {
                if (!System.IO.Directory.Exists(path))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(path));
                }
            });
        }

        public static void SafeDeleteDirectory(string path)
        {
            GeneralUtil.CatchAll(delegate
            {
                string[] files = Directory.GetFiles(path);
                foreach (string str in files)
                {
                    File.Delete(str);
                }


                Directory.Delete(path);
            });
        }

        public static string CombinePath(params string[] paths)
        {
            if (paths.Length == 0)
            {
                throw new ArgumentException("please input path");
            }
            else
            {
                StringBuilder builder = new StringBuilder();
                string spliter = "\\";

                string firstPath = paths[0];

                if (firstPath.StartsWith("HTTP", StringComparison.OrdinalIgnoreCase))
                {
                    spliter = "/";
                }

                if (!firstPath.EndsWith(spliter))
                {
                    firstPath = firstPath + spliter;
                }
                builder.Append(firstPath);

                for (int i = 1; i < paths.Length; i++)
                {
                    string nextPath = paths[i];
                    if (nextPath.StartsWith("/") || nextPath.StartsWith("\\"))
                    {
                        nextPath = nextPath.Substring(1);
                    }

                    if (i != paths.Length - 1)//not the last one
                    {
                        if (nextPath.EndsWith("/") || nextPath.EndsWith("\\"))
                        {
                            nextPath = nextPath.Substring(0, nextPath.Length - 1) + spliter;
                        }
                        else
                        {
                            nextPath = nextPath + spliter;
                        }
                    }

                    builder.Append(nextPath);
                }

                return builder.ToString();
            }
        }

        public static string SafeFileName(string name)
        {
            if (name == null)
            {
                return null;
            }
            StringBuilder builder = new StringBuilder();
            foreach (char ch in name)
            {
                if (((((ch >= ' ') && (ch != '/')) && ((ch != '\\') && (ch != ':'))) && (((ch != '*') && (ch != '?')) && ((ch != '\'') && (ch != '"')))) && (((ch != '<') && (ch != '>')) && ((ch != '|') && !char.IsControl(ch))))
                {
                    builder.Append(ch);
                }
                else
                {
                    builder.Append('-');
                }
            }
            return builder.ToString();
        }




        public static string SafeFilePath(string path)
        {
            if (path == null)
            {
                return null;
            }
            StringBuilder builder = new StringBuilder();
            foreach (string str in path.Split(new char[] { '/', '\\' }))
            {
                string str2 = SafeFileName(str);
                if (!string.IsNullOrEmpty(str2))
                {
                    builder.Append(str2);
                    builder.Append('\\');
                }
            }
            if (builder.Length == 0)
            {
                return string.Empty;
            }
            return builder.ToString(0, builder.Length - 1);
        }

 

 



    }
}
