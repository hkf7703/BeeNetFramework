/*
 * 
 * 
 * Code from http://www.codeproject.com/Tips/355513/Invent-your-own-Dynamic-LINQ-parser
 * 
 * 
 * 
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text.RegularExpressions;

namespace Bee.Util
{
    public abstract class PredicateParser
    {
        #region scanner

        /// <summary>tokenizer pattern: Optional-SpaceS...Token...Optional-Spaces</summary>
        private static readonly string _pattern = @"\s*(" + string.Join("|", new string[]
          {
              // operators and punctuation that are longer than one char: longest first
              string.Join("|", new string[] { "||", "&&", "==", "!=", "<=", ">=" }.Select(e => Regex.Escape(e)).ToArray()),
              @"""(?:\\.|[^""])*""",  // string
              @"\d+(?:\.\d+)?",       // number with optional decimal part
              @"\w+",                 // word
              @"\S",                  // other 1-char tokens (or eat up one character in case of an error)
          }) + @")\s*";

        /// <summary>get 1st char of current token (or a Space if no 1st char is obtained)</summary>
        private char Ch
        {
            get
            {
                return string.IsNullOrEmpty(Curr) ? ' ' : Curr[0];
            }
        }
        /// <summary>move one token ahead</summary><returns>true = moved ahead, false = end of stream</returns>
        private bool Move()
        {
            return _tokens.MoveNext();
        }

        /// <summary>the token stream implemented as IEnumerator<string></summary>
        private IEnumerator<string> _tokens;

        /// <summary>constructs the scanner for the given input string</summary>
        protected PredicateParser(string s)
        {
            _tokens = Regex.Matches(s, _pattern, RegexOptions.Compiled).Cast<Match>()
                      .Select(m => m.Groups[1].Value).GetEnumerator();
            Move();
        }
        protected bool IsNumber
        {
            get
            {
                return char.IsNumber(Ch);
            }
        }
        protected bool IsDouble
        {
            get
            { return IsNumber && Curr.Contains('.'); }
        }
        protected bool IsString
        {
            get { return Ch == '"'; }
        }
        protected bool IsIdent
        {
            get
            {
                char c = Ch;
                return char.IsLower(c) || char.IsUpper(c) || c == '_';
            }
        }
        /// <summary>throw an argument exception</summary>
        protected void Abort(string msg)
        {
            throw new ArgumentException("Error: " + (msg ?? "unknown error"));
        }
        /// <summary>get the current item of the stream or an empty string after the end</summary>
        protected string Curr
        {
            get
            { return _tokens.Current ?? string.Empty; }
        }
        /// <summary>get current and move to the next token (error if at end of stream)</summary>
        protected string CurrAndNext
        {
            get
            { string s = Curr; if (!Move()) Abort("data expected"); return s; }
        }
        /// <summary>get current and move to the next token if available</summary>
        protected string CurrOptNext
        {
            get { string s = Curr; Move(); return s; }
        }
        /// <summary>moves forward if current token matches and returns that (next token must exist)</summary>
        protected string CurrOpAndNext(params string[] ops)
        {
            string s = ops.Contains(Curr) ? Curr : null;
            if (s != null && !Move()) Abort("data expected");
            return s;
        }
        #endregion
    }
    public class PredicateParser<TData> : PredicateParser
    {
        #region code generator

        private static readonly Type _bool = typeof(bool);
        private static readonly Type[] _prom = new Type[]
          { typeof(decimal), typeof(double), typeof(float), typeof(ulong), typeof(long), typeof(uint),
            typeof(int), typeof(ushort), typeof(char), typeof(short), typeof(byte), typeof(sbyte) };

        /// <summary>enforce the type on the expression (by a cast) if not already of that type</summary>
        private static Expression Coerce(Expression expr, Type type)
        {
            return expr.Type == type ? expr : Expression.Convert(expr, type);
        }

        /// <summary>casts if needed the expr to the "largest" type of both arguments</summary>
        private static Expression Coerce(Expression expr, Expression sibling)
        {
            if (expr.Type != sibling.Type)
            {
                Type maxType = MaxType(expr.Type, sibling.Type);
                if (maxType != expr.Type) expr = Expression.Convert(expr, maxType);
            }
            return expr;
        }

        /// <summary>returns the first if both are same, or the largest type of both (or the first)</summary>
        private static Type MaxType(Type a, Type b)
        {
            return a == b ? a : (_prom.FirstOrDefault(t => t == a || t == b) ?? a);
        }

        /// <summary>
        /// Code generation of binary and unary epressions, utilizing type coercion where needed
        /// </summary>
        private static readonly Dictionary<string, Func<Expression, Expression, Expression>> _binOp =
            new Dictionary<string, Func<Expression, Expression, Expression>>()
          {
              { "||", (a,b)=>Expression.OrElse(Coerce(a, _bool), Coerce(b, _bool)) },
              { "&&", (a,b)=>Expression.AndAlso(Coerce(a, _bool), Coerce(b, _bool)) },
              { "==", (a,b)=>Expression.Equal(Coerce(a,b), Coerce(b,a)) },
              { "!=", (a,b)=>Expression.NotEqual(Coerce(a,b), Coerce(b,a)) },
              { "<", (a,b)=>Expression.LessThan(Coerce(a,b), Coerce(b,a)) },
              { "<=", (a,b)=>Expression.LessThanOrEqual(Coerce(a,b), Coerce(b,a)) },
              { ">=", (a,b)=>Expression.GreaterThanOrEqual(Coerce(a,b), Coerce(b,a)) },
              { ">", (a,b)=>Expression.GreaterThan(Coerce(a,b), Coerce(b,a)) },
          };

        private static readonly Dictionary<string, Func<Expression, Expression>> _unOp =
            new Dictionary<string, Func<Expression, Expression>>()
          {
              { "!", a=>Expression.Not(Coerce(a, _bool)) },
          };
        /// <summary>create a constant of a value</summary>
        private static ConstantExpression Const(object v)
        {
            return Expression.Constant(v);
        }
        /// <summary>create lambda parameter field or property access</summary>
        private MemberExpression ParameterMember(string s)
        {
            return Expression.PropertyOrField(_param, s);
        }
        /// <summary>create lambda expression</summary>
        private Expression<Func<TData, bool>> Lambda(Expression expr)
        {
            return Expression.Lambda<Func<TData, bool>>(expr, _param);
        }
        /// <summary>the lambda's parameter (all names are members of this)</summary>
        private readonly ParameterExpression _param = Expression.Parameter(typeof(TData), "_p_");

        #endregion

        #region parser

        /// <summary>initialize the parser (and thus, the scanner)</summary>
        private PredicateParser(string s)
            : base(s)
        {
        }
        /// <summary>main entry point</summary>
        public static Expression<Func<TData, bool>> Parse(string s)
        {
            return new PredicateParser<TData>(s).Parse();
        }
        private Expression<Func<TData, bool>> Parse()
        {
            return Lambda(ParseExpression());
        }
        private Expression ParseExpression()
        {
            return ParseOr();
        }
        private Expression ParseOr()
        {
            return ParseBinary(ParseAnd, "||");
        }
        private Expression ParseAnd()
        {
            return ParseBinary(ParseEquality, "&&");
        }
        private Expression ParseEquality()
        {
            return ParseBinary(ParseRelation, "==", "!=");
        }
        private Expression ParseRelation()
        {
            return ParseBinary(ParseUnary, "<", "<=", ">=", ">");
        }
        private Expression ParseUnary()
        {
            return CurrOpAndNext("!") != null ? _unOp["!"](ParseUnary())
            : ParsePrimary();
        }
        private Expression ParseIdent()
        {
            return ParameterMember(CurrOptNext);
        }
        private Expression ParseString()
        {
            return Const(Regex.Replace(CurrOptNext, "^\"(.*)\"$",
            m => m.Groups[1].Value));
        }
        private Expression ParseNumber()
        {
            if (IsDouble) return Const(double.Parse(CurrOptNext));
            return Const(int.Parse(CurrOptNext));
        }
        private Expression ParsePrimary()
        {
            if (IsIdent) return ParseIdent();
            if (IsString) return ParseString();
            if (IsNumber) return ParseNumber();
            return ParseNested();
        }
        private Expression ParseNested()
        {
            if (CurrAndNext != "(") Abort("(...) expected");
            Expression expr = ParseExpression();
            if (CurrOptNext != ")") Abort("')' expected");
            return expr;
        }
        /// <summary>generic parsing of binary expressions</summary>
        private Expression ParseBinary(Func<Expression> parse, params string[] ops)
        {
            Expression expr = parse();
            string op;
            while ((op = CurrOpAndNext(ops)) != null) expr = _binOp[op](expr, parse());
            return expr;
        }
        #endregion
    }

    public static class ExpressionUtil
    {
        
    }
}

