/*
 * 创建人：hukaifeng (hkf7703@163.com)

 * 最后更新：2012/9/7 9:28:51 
 * 功能说明： ScriptParser类
 * 
 
 * 主要类、属性，成员及其功能
    1. 
 * 历史修改记录：
	1 hukaifeng, 2012/9/7 9:28:51 ,  1.0.0.0, create   
	2 

*/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Bee.Core;
using System.IO;

namespace Bee
{
    internal enum ExpressionType
    {
        Text,
        Code,
        Comment,
        Expression,
        Import,
        Using,
        Language,
        DataSource,
        Function
    }

    public class ScriptParser
    {
        #region Constants

        private static readonly string CodeGroupName = "code";
        //private static readonly string LanguageGroupName = "Language";
        private static readonly string ImportGroupName = "Import";
        private static readonly string UsingGroupName = "Using";
        private static readonly string DataSourceIdGroupName = "DataSourceId";
        private static readonly string DataSourceTypeGroupName = "DataSourceType";
        private static readonly string DataSourceConfigGroupName = "Config";
        private static readonly string FunctionGroupName = "Function";

        #endregion

        #region Fields

        private static readonly Regex LanguageRegex =
            new Regex(@"\G<%@\s+(Language)=""(?<Language>.*?)""\s+%>", RegexOptions.IgnoreCase);
        private static readonly Regex ImportRegex =
            new Regex(@"\G<%@\s+(Import)=""(?<Import>.*?)""\s+%>", RegexOptions.IgnoreCase);
        private static readonly Regex UsingRegex =
            new Regex(@"\G<%@\s+(Using)=""(?<Using>.*?)""\s+%>", RegexOptions.IgnoreCase);
        private static readonly Regex CodeRegex = new Regex(@"\G<%(?<code>[^?!@=][\s\S]*?)%>");
        private static readonly Regex ExprRegex = new Regex(@"\G<%s*?=(?<code>.*?)?%>");
        private static readonly Regex TextRegex = new Regex(@"\G[^<]+");
        private static readonly Regex CommentRegex = new Regex(@"\G<%--(([^-]*)-)*?-%>");
        private static readonly Regex FunctionRegex = new Regex(@"\G<%@\s+Function\s+%>(?<Function>[\s\S]*?)<%@\s+Function\s+%>", RegexOptions.IgnoreCase);

        private StringBuilder stringBuilder = new StringBuilder();

        private List<string> references = new List<string>();
        private List<string> usingList = new List<string>();
        private List<string> functionList = new List<string>();

        #endregion

        #region Constructors

        public ScriptParser()
        {
        }

        #endregion

        #region Public Methods

        public void ParseTemplate(string template)
        {
            this.CleanUp();
            int startAt = 0;
            while (startAt < template.Length)
            {
                if (this.RegexParse(LanguageRegex, template, ExpressionType.Language, ref startAt))
                {
                    this.IgnoreTextMatch(template, ref startAt);
                }
                else
                {
                    if (this.RegexParse(ImportRegex, template, ExpressionType.Import, ref startAt))
                    {
                        this.IgnoreTextMatch(template, ref startAt);
                        continue;
                    }
                    if (this.RegexParse(UsingRegex, template, ExpressionType.Using, ref startAt))
                    {
                        this.IgnoreTextMatch(template, ref startAt);
                        continue;
                    }

                    if ((((!this.RegexParse(CommentRegex, template, ExpressionType.Comment, ref startAt)
                        && !this.RegexParse(CodeRegex, template, ExpressionType.Code, ref startAt))
                        && (!this.RegexParse(ExprRegex, template, ExpressionType.Expression, ref startAt)
                        && !this.RegexParse(TextRegex, template, ExpressionType.Text, ref startAt)))
                        && !this.RegexParse(FunctionRegex, template, ExpressionType.Function, ref startAt))
                        && (startAt < template.Length))
                    {
                        int index = template.IndexOf('\r', startAt);
                        throw new CoreException("Invalid template!", new ApplicationException(template.Substring(startAt, index - startAt)));
                    }
                }
            }

        }

        #endregion

        #region Private Methods

        private void CleanUp()
        {
            stringBuilder = new StringBuilder();
            references.Clear();
            usingList.Clear();
        }

        private Match IgnoreTextMatch(string source, ref int startAt)
        {
            Match match = TextRegex.Match(source, startAt);
            if (match.Success)
            {
                startAt += match.Length;
            }
            return match;
        }

        private bool RegexParse(Regex regex, string source, ExpressionType type, ref int startAt)
        {
            bool result = false;
            if (type == ExpressionType.Language)
            {
                Match match = regex.Match(source, startAt);
                if (match.Success)
                {
                    // Now only support c sharp.
                    startAt += match.Length;

                    result = true;
                }
            }
            else if (type == ExpressionType.Import)
            {
                Match match = regex.Match(source, startAt);
                if (match.Success)
                {
                    references.Add(match.Groups[ImportGroupName].Value);
                    startAt += match.Length;

                    result = true;
                }
            }
            else if (type == ExpressionType.Using)
            {
                Match match = regex.Match(source, startAt);

                if (match.Success)
                {
                    usingList.Add(match.Groups[UsingGroupName].Value);
                    startAt += match.Length;
                    result = true;
                }
            }
            else if (type == ExpressionType.Function)
            {
                Match match = regex.Match(source, startAt);

                if (match.Success)
                {
                    functionList.Add(match.Groups[FunctionGroupName].Value);
                    startAt += match.Length;
                    result = true;
                }
            }
            else
            {
                Match match = regex.Match(source, startAt);
                if (match.Success)
                {
                    startAt += match.Length;
                    Append(match, type);
                    result = true;
                }
            }

            return result;
        }

        private void Append(Match match, ExpressionType type)
        {
            stringBuilder.Append("\r\n            ");
            string script = string.Empty;
            if (type == ExpressionType.Code)
            {
                script = match.Groups[CodeGroupName].Value;
            }
            else if (type == ExpressionType.Expression)
            {
                script = match.Groups[CodeGroupName].Value;
            }
            else if (type == ExpressionType.Text)
            {
                script = match.Value;
            }
            else if (type == ExpressionType.Comment)
            {
                return;
            }
            else
            {
                // Do nothing here.
            }

            BeeStringReader reader = new BeeStringReader(script);
            bool containNewLine = false;

            bool lastLine = false;
            string line = reader.ReadLine(ref containNewLine);
            bool tempContainNewLine = false;
            while (true)
            {
                if (line != null)
                {
                    AppendLine(type, line);
                }
                else
                {
                    break;
                }
                tempContainNewLine = containNewLine;

                line = reader.ReadLine(ref containNewLine);

                lastLine = line == null;

                if (type == ExpressionType.Text && tempContainNewLine && !lastLine)
                {
                    AppendLine(type, "\\r\\n");
                }
            }

            match = null;
        }

        private void AppendLine(ExpressionType type, string scriptLine)
        {
            if (scriptLine.Length == 0)
            {
                return;
            }

            stringBuilder.Append("\r\n            ");
            switch (type)
            {
                case ExpressionType.Code:
                    stringBuilder.Append(scriptLine);
                    break;
                case ExpressionType.Expression:
                    stringBuilder.Append("BeeWriter.Instance.Write(").Append(scriptLine).Append(");");
                    break;
                case ExpressionType.Text:
                    stringBuilder.Append("BeeWriter.Instance.Write(\"").Append(scriptLine.Replace("\"", "\\\"")).Append("\");");
                    break;
                default:
                    break;
            }
        }

        #endregion

        #region Properties

        public List<string> FunctionList
        {
            get
            {
                return this.functionList;
            }
        }

        public string Output
        {
            get
            {
                return stringBuilder.ToString();
            }
        }

        public List<string> ReferenceList
        {
            get
            {
                return this.references;
            }
        }

        public List<string> UsingList
        {
            get
            {
                return this.usingList;
            }
        }

        #endregion
    }

    internal class BeeStringReader : TextReader
    {
        #region Fields

        private string target = null;
        private int length;
        private int position;

        #endregion

        #region Constructors

        public BeeStringReader(string s)
        {
            if (s == null)
            {
                throw new ArgumentNullException("s");
            }
            this.target = s;
            this.length = (s == null) ? 0 : s.Length;
        }

        #endregion

        #region Public Methods

        public override void Close()
        {
            this.Dispose(true);
        }

        protected override void Dispose(bool disposing)
        {
            this.target = null;
            this.length = 0;
            this.position = 0;
            base.Dispose(disposing);
        }

        public string ReadLine(ref bool containNewline)
        {
            containNewline = false;
            int num = this.position;
            while (num < this.length)
            {
                char ch = this.target[num];
                switch (ch)
                {
                    case '\r':
                    case '\n':
                        {
                            string str = this.target.Substring(this.position, num - this.position);
                            this.position = num + 1;
                            if (((ch == '\r') && (this.position < this.length)) && (this.target[this.position] == '\n'))
                            {
                                this.position++;
                            }

                            containNewline = true;
                            return str;
                        }
                }
                num++;
            }
            if (num > this.position)
            {
                string str2 = this.target.Substring(this.position, num - this.position);
                this.position = num;
                return str2;
            }

            return null;
        }

        #endregion

    }
}
