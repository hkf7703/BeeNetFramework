using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Bee
{
    public static class Constants
    {
        public static readonly string LoggingSetting = "Bee.Logging";
        public static readonly string LogFileName = @".\Log\{0}.log";

        public static readonly string DefaultIdentityColumnName = "Id";

        public static readonly string DBTableCacheName = "DB_{0}_Data";

        public static readonly int DefaultCacheDuration = 120;

        public static readonly string BeeModelName = "Bee_Model";
        public static readonly string BeeAreaName = "Bee_AreaName";
        public static readonly string BeeControllerName = "Bee_ControllerName";
        public static readonly string BeeActionName = "Bee_ActionName";
        public static readonly string BeeAutoModelInfo = "Bee_AutoModelInfo";

        public static readonly string BeeDataTableSchemaCacheCategory = "Bee_TableScheam_";
        public static readonly string BeeDataSPSchemaCacheCategory = "Bee_SPScheam_";
        public static readonly string BeeDataTableNameTypeCategory = "Bee_TypeTableName_";

        public static readonly DateTime InitialJavaScriptDateTime = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        public static string DateTimeFormat = "yyyy-MM-dd HH:mm";
        public static string NumberFormat = "0.00";

        public static readonly string BeeReadonly = "Bee_Readonly";
    }

    internal static class ErrorCode
    {
        public static readonly string DataGeneralError = "Data_GeneralError";


        public static readonly string MVCNoAction = "MVC_NoAction";


        public static readonly string WebError404 = "WebError404";
    }
}
