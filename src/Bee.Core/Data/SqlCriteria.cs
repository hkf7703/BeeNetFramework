using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Bee.Util;
using System.Data.Common;
using System.Text.RegularExpressions;
using System.Linq.Expressions;

namespace Bee.Data
{
    internal enum CriterionType
    {
        Equal,
        GreaterThan,
        LessThan,
        NotEqual,
        GreaterThanOrEqual,
        LessThanOrEqual,
        IsNull,
        IsNotNull,
        Contains,
        StartWith,
        EndWith,
        In
    }

    internal enum CriterionModel
    {
        Normal,
        Column
    }

    internal class Criterion
    {
        private string leftExpression;

        private CriterionType criterionType;

        private CriterionModel criterionModel;

        private object value;

        private string parameterName;

        public Criterion(string name, object value, CriterionType type)
            : this(name, value, type, CriterionModel.Normal)
        {

        }

        public Criterion(string name, object value, CriterionType type, CriterionModel model)
        {
            ThrowExceptionUtil.ArgumentNotNullOrEmpty("name", name);

            // 若是Enum， 转换成数字匹配
            if (value is Enum)
            {
                value = (int)value;
            }

            this.leftExpression = name;
            this.value = value;
            this.criterionType = type;
            this.criterionModel = model;
        }

        public string LeftExpression
        {
            get
            {
                return leftExpression;
            }
        }

        public CriterionType CriterionType
        {
            get
            {
                return this.criterionType;
            }
        }

        public CriterionModel CriterionModel
        {
            get
            {
                return this.criterionModel;
            }
        }

        public bool NeedParameter
        {
            get
            {
                return criterionModel == CriterionModel.Normal
                    && criterionType != CriterionType.IsNotNull
                    && criterionType != CriterionType.IsNull
                    && criterionType != CriterionType.In;
            }
        }

        public object Value
        {
            get
            {
                object result = value;
                switch (this.CriterionType)
                {
                    case CriterionType.Contains:
                        result = string.Format("%{0}%", value);
                        break;

                    case CriterionType.EndWith:
                        result = string.Format("%{0}", value);
                        break;

                    case CriterionType.StartWith:
                        result = string.Format("{0}%", value);
                        break;
                    default:
                        break;
                }

                return result;
            }
        }

        public string ParameterName
        {
            get
            {
                return this.parameterName;
            }
            set
            {
                this.parameterName = value;
            }
        }

    }

    /// <summary>
    /// Sql条件
    /// 一般应用场合只有在单线程下， 多线程不安全。
    /// </summary>
    public class SqlCriteria
    {
        private DbSession owner;

        private static readonly string parameterNameTemplate = "p{0}{1}";
        private List<Criterion> innerList = new List<Criterion>();
        private Dictionary<Guid, SqlCriteria> root = new Dictionary<Guid, SqlCriteria>();
        private Guid guid;
        private string expression;
        private bool blurFlag = false;

        private string whereClause;
        private string filterClause;
        private List<DbParameter> dbParameterList = new List<DbParameter>();

        public SqlCriteria()
        {
            guid = Guid.NewGuid();
            root.Add(guid, this);
            expression = string.Format("({0})", guid);
        }

        internal DbSession Owner
        {
            get
            {
                DbSession result = owner;
                if (result == null)
                {
                    result = DbSession.Current;
                }

                return result;
            }
            set
            {
                if (owner != value)
                {
                    this.blurFlag = true;
                    this.owner = value;
                }
            }
        }

        #region Operation Methods

        public static SqlCriteria From<T>(Expression<Func<T, bool>> predicate) where T : class
        {
            return new SqlCriteriaVisitor<T>().Visit(predicate);
        }

        public static SqlCriteria New
        {
            get
            {
                return new SqlCriteria();
            }
        }

        public SqlCriteria Equal(string name, object value)
        {
            blurFlag = true;
            innerList.Add(new Criterion(name, value, CriterionType.Equal));
            return this;
        }

        public SqlCriteria CEqual(string name, string value)
        {
            blurFlag = true;
            innerList.Add(new Criterion(name, value, CriterionType.Equal, CriterionModel.Column));
            return this;
        }

        public SqlCriteria In(string name, string value)
        {
            blurFlag = true;
            innerList.Add(new Criterion(name, value, CriterionType.In));

            return this;
        }

        public SqlCriteria GreaterThan(string name, object value)
        {
            blurFlag = true;
            innerList.Add(new Criterion(name, value, CriterionType.GreaterThan));
            return this;
        }

        public SqlCriteria CGreaterThan(string name, string value)
        {
            blurFlag = true;
            innerList.Add(new Criterion(name, value, CriterionType.GreaterThan, CriterionModel.Column));
            return this;
        }

        public SqlCriteria LessThan(string name, object value)
        {
            blurFlag = true;
            innerList.Add(new Criterion(name, value, CriterionType.LessThan));
            return this;
        }

        public SqlCriteria CLessThan(string name, string value)
        {
            blurFlag = true;
            innerList.Add(new Criterion(name, value, CriterionType.LessThan, CriterionModel.Column));
            return this;
        }

        public SqlCriteria NotEqual(string name, object value)
        {
            blurFlag = true;
            innerList.Add(new Criterion(name, value, CriterionType.NotEqual));
            return this;
        }

        public SqlCriteria CNotEqual(string name, string value)
        {
            blurFlag = true;
            innerList.Add(new Criterion(name, value, CriterionType.NotEqual, CriterionModel.Column));
            return this;
        }

        public SqlCriteria GreaterThanOrEqual(string name, object value)
        {
            blurFlag = true;
            innerList.Add(new Criterion(name, value, CriterionType.GreaterThanOrEqual));
            return this;
        }


        public SqlCriteria CGreaterThanOrEqual(string name, string value)
        {
            blurFlag = true;
            innerList.Add(new Criterion(name, value, CriterionType.GreaterThanOrEqual, CriterionModel.Column));
            return this;
        }


        public SqlCriteria LessThanOrEqual(string name, object value)
        {
            blurFlag = true;
            innerList.Add(new Criterion(name, value, CriterionType.LessThanOrEqual));
            return this;
        }

        public SqlCriteria CLessThanOrEqual(string name, string value)
        {
            blurFlag = true;
            innerList.Add(new Criterion(name, value, CriterionType.LessThanOrEqual, CriterionModel.Column));
            return this;
        }

        public SqlCriteria IsNull(string name)
        {
            blurFlag = true;
            innerList.Add(new Criterion(name, null, CriterionType.IsNull));
            return this;
        }

        public SqlCriteria IsNotNull(string name)
        {
            blurFlag = true;
            innerList.Add(new Criterion(name, null, CriterionType.IsNotNull));
            return this;
        }

        public SqlCriteria Contains(string name, string value)
        {
            blurFlag = true;
            innerList.Add(new Criterion(name, value, CriterionType.Contains));
            return this;
        }

        public SqlCriteria StartWith(string name, string value)
        {
            blurFlag = true;
            innerList.Add(new Criterion(name, value, CriterionType.StartWith));
            return this;
        }

        public SqlCriteria EndWith(string name, string value)
        {
            blurFlag = true;
            innerList.Add(new Criterion(name, value, CriterionType.EndWith));
            return this;
        }

        public SqlCriteria And(SqlCriteria sqlCriteria)
        {
            ThrowExceptionUtil.ArgumentNotNull(sqlCriteria, "sqlCriteria");
            blurFlag = true;
            if (sqlCriteria.innerList.Count != 0 || sqlCriteria.root.Count != 0)
            {
                root.Add(sqlCriteria.guid, sqlCriteria);

                expression = string.Format("({0} and {1})", expression, sqlCriteria.guid);
            }

            return this;
        }

        public SqlCriteria Or(SqlCriteria sqlCriteria)
        {
            ThrowExceptionUtil.ArgumentNotNull(sqlCriteria, "sqlCriteria");
            blurFlag = true;
            if (sqlCriteria.innerList.Count != 0 || sqlCriteria.root.Count != 0)
            {
                root.Add(sqlCriteria.guid, sqlCriteria);

                expression = string.Format("({0} or {1})", expression, sqlCriteria.guid);
            }

            return this;
        }

        #endregion

        public string WhereClause
        {
            get
            {
                string result = "1=1";
                InitParameterName();

                if (!string.IsNullOrEmpty(whereClause))
                {
                    result = whereClause;
                }

                return result;
            }
        }

        public string FilterClause
        {
            get
            {
                string result = string.Empty;
                InitParameterName();

                if (!string.IsNullOrEmpty(filterClause))
                {
                    result = filterClause;
                }

                return result;
            }
        }

        public List<DbParameter> DbParameters
        {
            get
            {
                InitParameterName();

                return this.dbParameterList;
            }
        }

        private string FormatExpression(Criterion criterion)
        {
            string str = Owner.DbDriver.FormatField(criterion.LeftExpression); ;
            string result = string.Empty;
            switch (criterion.CriterionType)
            {
                case CriterionType.Equal:
                    if (criterion.CriterionModel == CriterionModel.Normal)
                    {
                        result = string.Format(" {0} = {1}{2}\r\n", str,
                            Owner.DbDriver.ParameterPrefix, criterion.ParameterName);
                    }
                    else
                    {
                        result = string.Format(" {0} = {1}\r\n", criterion.LeftExpression, criterion.Value);
                    }
                    break;
                case CriterionType.GreaterThan:
                    if (criterion.CriterionModel == CriterionModel.Normal)
                    {
                        result = string.Format(" {0} > {1}{2}\r\n", str,
                            Owner.DbDriver.ParameterPrefix, criterion.ParameterName);
                    }
                    else
                    {
                        result = string.Format(" {0} > {1}\r\n", criterion.LeftExpression, criterion.Value);
                    }
                    break;
                case CriterionType.LessThan:
                    if (criterion.CriterionModel == CriterionModel.Normal)
                    {
                        result = string.Format(" {0} < {1}{2}\r\n", str,
                            Owner.DbDriver.ParameterPrefix, criterion.ParameterName);
                    }
                    else
                    {
                        result = string.Format(" {0} < {1}\r\n", criterion.LeftExpression, criterion.Value);
                    }
                    break;
                case CriterionType.NotEqual:
                    if (criterion.CriterionModel == CriterionModel.Normal)
                    {
                        result = string.Format(" {0} <> {1}{2}\r\n", str,
                            Owner.DbDriver.ParameterPrefix, criterion.ParameterName);
                    }
                    else
                    {
                        result = string.Format(" {0} <> {1}\r\n", criterion.LeftExpression, criterion.Value);
                    }
                    break;
                case CriterionType.GreaterThanOrEqual:
                    if (criterion.CriterionModel == CriterionModel.Normal)
                    {
                        result = string.Format(" {0} >= {1}{2}\r\n", str,
                            Owner.DbDriver.ParameterPrefix, criterion.ParameterName);
                    }
                    else
                    {
                        result = string.Format(" {0} >= {1}\r\n", criterion.LeftExpression, criterion.Value);
                    }
                    break;
                case CriterionType.LessThanOrEqual:
                    if (criterion.CriterionModel == CriterionModel.Normal)
                    {
                        result = string.Format(" {0} <= {1}{2}\r\n", str,
                            Owner.DbDriver.ParameterPrefix, criterion.ParameterName);
                    }
                    else
                    {
                        result = string.Format(" {0} <= {1}\r\n", criterion.LeftExpression, criterion.Value);
                    }
                    break;
                case CriterionType.IsNull:
                    result = string.Format(" {0} is null\r\n", str);
                    break;
                case CriterionType.IsNotNull:
                    result = string.Format(" {0} is not null\r\n", str);
                    break;
                case CriterionType.Contains:
                case CriterionType.StartWith:
                case CriterionType.EndWith:
                    result = string.Format(" {0} like {1}{2}\r\n", str,
                        Owner.DbDriver.ParameterPrefix, criterion.ParameterName);
                    break;
                case CriterionType.In:
                    result = string.Format(" {0} in ({1})\r\n", str, criterion.Value);
                    break;
            }
            return result;
        }

        private string FormatFilterExpression(Criterion criterion)
        {
            string str = criterion.LeftExpression;
            string result = string.Empty;

            object sqlCriteriaValue = criterion.Value;
            bool stringFlag = sqlCriteriaValue is string;

            string template = string.Empty;

            if (stringFlag)
            {
                if (criterion.CriterionModel == CriterionModel.Normal)
                {
                    template = " {{0}} {0} '{{1}}'\r\n";
                }
                else
                {
                    template = " {{0}} {0} {{1}}\r\n";
                }
            }
            else
            {
                template = " {{0}} {0} {{1}}\r\n";
            }

            switch (criterion.CriterionType)
            {
                case CriterionType.Equal:
                    result = string.Format(string.Format(template, "="), str, sqlCriteriaValue);
                    break;
                case CriterionType.GreaterThan:
                    result = string.Format(string.Format(template, ">"), str, sqlCriteriaValue);
                    break;
                case CriterionType.LessThan:
                    result = string.Format(string.Format(template, "<"), str, sqlCriteriaValue);
                    break;
                case CriterionType.NotEqual:
                    result = string.Format(string.Format(template, "<>"), str, sqlCriteriaValue);
                    break;
                case CriterionType.GreaterThanOrEqual:
                    result = string.Format(string.Format(template, ">="), str, sqlCriteriaValue);
                    break;
                case CriterionType.LessThanOrEqual:
                    result = string.Format(string.Format(template, "<="), str, sqlCriteriaValue);
                    break;
                case CriterionType.IsNull:
                    result = string.Format(" {0} is null\r\n", str);
                    break;
                case CriterionType.IsNotNull:
                    result = string.Format(" {0} is not null\r\n", str);
                    break;
                case CriterionType.In:
                    result = string.Format(" {0} in ({1})", str, sqlCriteriaValue);
                    break;
                case CriterionType.Contains:
                case CriterionType.StartWith:
                case CriterionType.EndWith:
                    result = string.Format(" {0} like '{1}'\r\n", str, sqlCriteriaValue);
                    break;
            }
            return result;
        }

        private string GetWhereClause(SqlCriteria sqlCriteria)
        {
            StringBuilder builder = new StringBuilder();

            if (sqlCriteria.innerList.Count != 0)
            {
                for (int i = 0; i < sqlCriteria.innerList.Count; i++)
                {
                    string expression = FormatExpression(sqlCriteria.innerList[i]);
                    if (i == 0)
                    {
                        builder.Append(expression);
                    }
                    else
                    {
                        builder.AppendFormat(" and {0}", expression);
                    }
                }
            }
            else
            {
                builder.Append(" 1=1 ");
            }

            StringBuilder rootBuilder = new StringBuilder();
            rootBuilder.Append(sqlCriteria.expression);
            foreach (SqlCriteria item in sqlCriteria.root.Values)
            {
                if (item.guid == sqlCriteria.guid)
                {
                    rootBuilder.Replace(item.guid.ToString(), builder.ToString());
                }
                else
                {
                    rootBuilder.Replace(item.guid.ToString(), GetWhereClause(item));
                }
            }

            return rootBuilder.ToString();
        }

        private string GetFilterClause(SqlCriteria sqlCriteria)
        {
            StringBuilder builder = new StringBuilder();

            if (sqlCriteria.innerList.Count != 0)
            {
                for (int i = 0; i < sqlCriteria.innerList.Count; i++)
                {
                    if (i == 0)
                    {
                        builder.Append(FormatFilterExpression(sqlCriteria.innerList[i]));
                    }
                    else
                    {
                        builder.AppendFormat(" and {0}", FormatFilterExpression(sqlCriteria.innerList[i]));
                    }
                }
            }
            else
            {
                builder.Append(" 1=1 ");
            }

            StringBuilder rootBuilder = new StringBuilder();
            rootBuilder.Append(sqlCriteria.expression);
            foreach (SqlCriteria item in sqlCriteria.root.Values)
            {
                if (item.guid == sqlCriteria.guid)
                {
                    rootBuilder.Replace(item.guid.ToString(), builder.ToString());
                }
                else
                {
                    rootBuilder.Replace(item.guid.ToString(), GetFilterClause(item));
                }
            }

            return rootBuilder.ToString();
        }

        private void InitParameterName()
        {
            if (blurFlag)
            {
                int index = 0;
                dbParameterList = new List<DbParameter>();
                InitParameterName(this, ref index);

                whereClause = GetWhereClause(this);
                filterClause = GetFilterClause(this);
            }
        }

        private void InitParameterName(SqlCriteria sqlCriteria, ref int index)
        {
            sqlCriteria.blurFlag = false;
            foreach (Criterion item in sqlCriteria.innerList)
            {
                if (item.Value != null && item.NeedParameter)
                {
                    string itemParamName = Regex.Replace(item.LeftExpression, @"([\(\)])+", "_");

                    item.ParameterName = string.Format(parameterNameTemplate, itemParamName, index);

                    DbParameter dbParameter = Owner.DbDriver.CreateParameter();
                    dbParameter.ParameterName = item.ParameterName;

                    dbParameter.Value = item.Value;
                    dbParameterList.Add(dbParameter);
                    index++;
                }
            }

            foreach (SqlCriteria item in sqlCriteria.root.Values)
            {
                if (item.guid != sqlCriteria.guid)
                {
                    InitParameterName(item, ref index);
                }
            }
        }

        public override string ToString()
        {
            return FilterClause;
        }

    }

    internal class SqlCriteriaVisitor<T> where T : class
    {
        public SqlCriteria Visit(Expression predicate)
        {
            var lambda = predicate as LambdaExpression;
            return VisitExpression(lambda.Body);
        }

        private SqlCriteria VisitExpression(Expression expr)
        {
            SqlCriteria sqlCriteria = new SqlCriteria();

            if (expr.NodeType == ExpressionType.Equal) // ==
            {
                var bin = expr as BinaryExpression;

                ThrowExceptionUtil.ArgumentConditionTrue(bin.Left is MemberExpression, "expr", "left expression should be MemberExpression");

                if (bin.Right is MemberExpression)
                {
                    return sqlCriteria.CEqual(this.VisitMember(bin.Left), this.VisitMember(bin.Right));
                }
                else
                {
                    return sqlCriteria.Equal(this.VisitMember(bin.Left), this.VisitValue(bin.Right));
                }

            }
            else if (expr is MemberExpression && expr.Type == typeof(bool)) // x.Active
            {
                return sqlCriteria.Equal(this.VisitMember(expr), true);
            }
            else if (expr.NodeType == ExpressionType.Not) // !x.Active
            {
                return sqlCriteria.Equal(this.VisitMember(expr), false);
            }
            else if (expr.NodeType == ExpressionType.NotEqual) // !=
            {
                var bin = expr as BinaryExpression;

                ThrowExceptionUtil.ArgumentConditionTrue(bin.Left is MemberExpression, "expr", "left expression should be MemberExpression");

                if (bin.Right is MemberExpression)
                {
                    return sqlCriteria.CNotEqual(this.VisitMember(bin.Left), this.VisitMember(bin.Right));
                }
                else
                {
                    return sqlCriteria.NotEqual(this.VisitMember(bin.Left), this.VisitValue(bin.Right));
                }
            }
            else if (expr.NodeType == ExpressionType.LessThan) // <
            {
                var bin = expr as BinaryExpression;
                ThrowExceptionUtil.ArgumentConditionTrue(bin.Left is MemberExpression, "expr", "left expression should be MemberExpression");

                if (bin.Right is MemberExpression)
                {
                    return sqlCriteria.CLessThan(this.VisitMember(bin.Left), this.VisitMember(bin.Right));
                }
                else
                {
                    return sqlCriteria.LessThan(this.VisitMember(bin.Left), this.VisitValue(bin.Right));
                }
            }
            else if (expr.NodeType == ExpressionType.LessThanOrEqual) // <=
            {
                var bin = expr as BinaryExpression;
                ThrowExceptionUtil.ArgumentConditionTrue(bin.Left is MemberExpression, "expr", "left expression should be MemberExpression");

                if (bin.Right is MemberExpression)
                {
                    return sqlCriteria.CLessThanOrEqual(this.VisitMember(bin.Left), this.VisitMember(bin.Right));
                }
                else
                {
                    return sqlCriteria.LessThanOrEqual(this.VisitMember(bin.Left), this.VisitValue(bin.Right));
                }
            }
            else if (expr.NodeType == ExpressionType.GreaterThan) // >
            {
                var bin = expr as BinaryExpression;
                ThrowExceptionUtil.ArgumentConditionTrue(bin.Left is MemberExpression, "expr", "left expression should be MemberExpression");

                if (bin.Right is MemberExpression)
                {
                    return sqlCriteria.CGreaterThan(this.VisitMember(bin.Left), this.VisitMember(bin.Right));
                }
                else
                {
                    return sqlCriteria.GreaterThan(this.VisitMember(bin.Left), this.VisitValue(bin.Right));
                }
            }
            else if (expr.NodeType == ExpressionType.GreaterThanOrEqual) // >=
            {
                var bin = expr as BinaryExpression;

                ThrowExceptionUtil.ArgumentConditionTrue(bin.Left is MemberExpression, "expr", "left expression should be MemberExpression");

                if (bin.Right is MemberExpression)
                {
                    return sqlCriteria.CGreaterThanOrEqual(this.VisitMember(bin.Left), this.VisitMember(bin.Right));
                }
                else
                {
                    return sqlCriteria.GreaterThanOrEqual(this.VisitMember(bin.Left), this.VisitValue(bin.Right));
                }
            }
            else if (expr is MethodCallExpression)
            {
                var met = expr as MethodCallExpression;
                var method = met.Method.Name;

                // StartsWith
                if (method == "StartsWith")
                {
                    var value = this.VisitValue(met.Arguments[0]).ToString();

                    return sqlCriteria.StartWith(this.VisitMember(met.Object), value);
                }
                // Contains
                else if (method == "Contains")
                {
                    var value = this.VisitValue(met.Arguments[0]).ToString();

                    return sqlCriteria.Contains(this.VisitMember(met.Object), value);
                }
                // Equals
                else if (method == "Equals")
                {
                    if (met.Arguments[0] is MemberExpression)
                    {
                        return sqlCriteria.CEqual(this.VisitMember(met.Object), this.VisitMember(met.Arguments[0]));
                    }
                    else
                    {
                        var value = this.VisitValue(met.Arguments[0]);
                        return sqlCriteria.Equal(this.VisitMember(met.Object), value);
                    }
                }
                // System.Linq.Enumerable methods
                else if (met.Method.DeclaringType.FullName == "System.Linq.Enumerable")
                {
                    //return ParseEnumerableExpression(met);
                }
            }
            else if (expr is BinaryExpression && expr.NodeType == ExpressionType.AndAlso)
            {
                // AND
                var bin = expr as BinaryExpression;
                var left = this.VisitExpression(bin.Left);
                var right = this.VisitExpression(bin.Right);

                return left.And(right);
            }
            else if (expr is BinaryExpression && expr.NodeType == ExpressionType.OrElse)
            {
                // OR
                var bin = expr as BinaryExpression;
                var left = this.VisitExpression(bin.Left);
                var right = this.VisitExpression(bin.Right);

                return left.Or(right);
            }

            throw new NotImplementedException("Not implemented Linq expression");
        }


        private string VisitMember(Expression expr)
        {
            // quick and dirty solution to support x.Name.SubName
            // http://stackoverflow.com/questions/671968/retrieving-property-name-from-lambda-expression

            var str = expr.ToString(); // gives you: "o => o.Whatever"
            var firstDelim = str.IndexOf('.'); // make sure there is a beginning property indicator; the "." in "o.Whatever" -- this may not be necessary?

            var property = firstDelim < 0 ? str : str.Substring(firstDelim + 1).TrimEnd(')');

            return this.GetField(property);
        }

        private object VisitValue(Expression expr)
        {
            object result = null;
            // its a constant; Eg: "fixed string"
            if (expr is ConstantExpression)
            {
                var value = (expr as ConstantExpression);

                result = value.Value;
            }
            else if (expr is MemberExpression)
            {
                result = VisitMember(expr);

            }
            else
            {
                // do nothing here.
            }

            return result;
        }

        private string GetField(string property)
        {
            return property;
        }
    }
}
