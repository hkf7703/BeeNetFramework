using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using Bee.Core;

namespace Bee
{
    public abstract class XmlBuilder<T> where T : XmlBuilder<T>
    {
        // Fields
        protected string ctag;
        protected StringBuilder result;
        protected Stack<string> tags;

        // Methods
        protected XmlBuilder()
        {
            this.result = new StringBuilder();
            this.tags = new Stack<string>();
            this.ctag = string.Empty;
        }

        public T attr(string Name, object Value)
        {
            if ((this.result.Length <= 0) || (this.result[this.result.Length - 1] != '>'))
            {
                throw new CoreException(string.Format("The attribute '{0}' can not be added because there is no tag before it.", Name));
            }
            this.result.Length--;
            this.result.Append(" ").Append(Name);
            if (Value != null)
            {
                this.result.Append("=\"").Append(Value).Append("\"");
            }
            this.result.Append(">");
            return (T)this;
        }

        public T enter()
        {
            return this.include("\r\n");
        }

        public T include(T hb)
        {
            return this.include(hb.ToString());
        }

        public T include(string text)
        {
            this.result.Append(text);
            this.ctag = string.Empty;
            return (T)this;
        }

        public T include(string text, bool cdataFlag)
        {
            if (cdataFlag)
            {
                this.result.AppendFormat("<![CDATA[{0}]]>", text);
            }
            else
            {
                this.result.Append(text);
            }
            this.ctag = string.Empty;
            return (T)this;
        }

        public T tag(string TagName)
        {
            this.result.Append("<").Append(TagName).Append(">");
            this.tags.Push(TagName);
            this.ctag = TagName;
            return (T)this;
        }

        public T text(object obj)
        {
            return this.text(obj.ToString(), false);
        }

        public T text(string text, bool ignoreEncode)
        {
            if (ignoreEncode)
            {
                return this.include(text);
            }
            else
            {
                return this.include(HttpUtility.HtmlEncode(text));
            }
        }

        public override string ToString()
        {
            if (this.tags.Count != 0)
            {
                throw new CoreException("There are some tags not closed!");
            }
            return this.result.ToString();
        }

        public T endTag()
        {
            return this.end;
        }

        // Properties
        public T end
        {
            get
            {
                string str = this.tags.Pop();
                if ((str == this.ctag) && (this.result[this.result.Length - 1] == '>'))
                {
                    this.result.Length--;
                    this.result.Append(" />");
                }
                else
                {
                    this.result.Append("</").Append(str).Append(">");
                }
                this.ctag = string.Empty;
                return (T)this;
            }
        }

        public T newline
        {
            get
            {
                return this.include("\n");
            }
        }

        public T tab
        {
            get
            {
                return this.include("\t");
            }
        }
    }

    public class XmlBuilder : XmlBuilder<XmlBuilder>
    {
        public XmlBuilder()
            //:this("1.0", "utf-8")
        {
        }

        public XmlBuilder(string Version, string Encoding)
        {
            base.result.Append("<?xml version=\"").Append(Version).Append("\" encoding=\"").Append(Encoding).Append("\" ?>\r\n");
        }

        public static XmlBuilder New
        {
            get
            {
                return new XmlBuilder();
            }
        }
    }

    public class HtmlBuilder : XmlBuilder<HtmlBuilder>
    {
        public static HtmlBuilder New
        {
            get
            {
                return new HtmlBuilder();
            }
        }
    }
}
