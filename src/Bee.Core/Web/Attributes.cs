using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Bee.Web
{
    public enum ModelQueryType
    {
        Equal,
        GreaterThan,
        LessThan,
        GreaterThanOrEqual,
        LessThanOrEqual,
        Between,
        Contains,
        StartWith,
        EndWith,
        In
    }

    [AttributeUsage(AttributeTargets.Class)]
    public class ModelAttribute : Attribute
    {
        public string DefaultOrderField = "Id";
        public bool DefaultOrderAscFlag = false;
        public int PageSize = 20;

    }

    [AttributeUsage(AttributeTargets.Property)]
    public class ModelPropertyAttribute : Attribute
    {
        public bool Visible = true;
        public string Description;
        public bool OrderableFlag;
        public bool ReadonlyFlag;
        public int ColumnWidth;
        public string Align;
        public bool Queryable;
        public ModelQueryType QueryType;
        public string MappingName;
    }

    /// <summary>
    /// 加入Area特性， AreaName与ControllerName以【|】拼接
    /// </summary>
    public class BeeControllerAttribute : Attribute
    {
        public bool DefaultFlag = false;
        /// <summary>
        /// 自定义ControllerName。 默认是Controller的type名称去除Controller
        /// </summary>
        public string ControllerName = string.Empty;
        /// <summary>
        /// 加入Area特性， AreaName与ControllerName以【|】拼接
        /// </summary>
        public string AreaName = string.Empty;

        public string Description = string.Empty;
    }

    public class OutputCacheAttribute : Attribute
    {
    }

    public class ActionAttribute : Attribute
    {
        public bool DefaultFlag;
        public string RequestMethod;

        public string Description = string.Empty;

        public ActionAttribute(bool defaultFlag, string requestMethod)
        {
        }
    }
}
