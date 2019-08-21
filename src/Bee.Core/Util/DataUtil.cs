using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using Bee.Data;
using System.Data.OleDb;
using Bee.Core;
using Bee.Web;

namespace Bee.Util
{
    public enum CompareObjectType
    {
        None,
        SrcPropertyNullIgnore,
        TargetPropertyNullIgnore,
        EitherPropertyNullIgnore
    }

    /// <summary>
    /// The Util for data.
    /// </summary>
    public static class DataUtil
    {
        public static string CompareObject<T>(T src, T target, CompareObjectType compareObjectType)
             where T : class
        {
            StringBuilder builder = new StringBuilder();

            if (src == null)
            {
                builder.AppendFormat("新增:{0}", CommonToString(target));
            }
            else if (target == null)
            {
                builder.AppendFormat("删除:{0}", CommonToString(src));
            }
            else
            {
                builder.AppendFormat("修改：");
                IEntityProxy entityProxy = EntityProxyManager.Instance.GetEntityProxy<T>();
                List<PropertySchema> list = entityProxy.GetPropertyList();

                foreach (PropertySchema schema in list)
                {
                    object srcValue = entityProxy.GetPropertyValue(src, schema.Name);
                    object targetValue = entityProxy.GetPropertyValue(target, schema.Name);

                    ModelPropertyAttribute modelProperty = schema.GetCustomerAttribute<ModelPropertyAttribute>();
                    string title = schema.Name;

                    if (modelProperty != null)
                    {
                        if (!string.IsNullOrEmpty(modelProperty.Description))
                        {
                            title = modelProperty.Description;
                        }
                    }
                    if (schema.PropertyType == typeof(DateTime))
                    {
                        continue;
                    }

                    if (schema.PropertyType.IsValueType)
                    {
                        srcValue = srcValue == null ? "null" : srcValue.ToString();
                        targetValue = targetValue == null ? "null" : targetValue.ToString();
                    }

                    if (srcValue != null)
                    {
                        if (targetValue == null &&
                            (compareObjectType == CompareObjectType.TargetPropertyNullIgnore
                            || compareObjectType == CompareObjectType.EitherPropertyNullIgnore))
                        {
                            continue;
                        }

                        if (!srcValue.Equals(targetValue))
                        {
                            builder.AppendFormat("{0}:{1}->{2},", title, srcValue, targetValue);
                        }
                    }
                    else
                    {
                        if (compareObjectType == CompareObjectType.SrcPropertyNullIgnore
                            || compareObjectType == CompareObjectType.EitherPropertyNullIgnore)
                        {
                            continue;
                        }
                        if (targetValue != null)
                        {
                            builder.AppendFormat("{0}:{1}->{2},", title, srcValue, targetValue);
                        }
                    }
                }
            }
            if (builder.Length == 3)
            {
                return string.Empty;
            }
            else
            {
                return builder.ToString();
            }
        }

        public static string CommonToString<T>(T value) where T : class
        {
            if (value == null) return "null";
            IEntityProxy entityProxy = EntityProxyManager.Instance.GetEntityProxy<T>();
            List<PropertySchema> list = entityProxy.GetPropertyList();
            StringBuilder builder = new StringBuilder();
            foreach (PropertySchema schema in list)
            {
                object targetValue = entityProxy.GetPropertyValue(value, schema.Name);

                ModelPropertyAttribute modelProperty = schema.GetCustomerAttribute<ModelPropertyAttribute>();
                string title = schema.Name;

                if (modelProperty != null)
                {
                    if (!string.IsNullOrEmpty(modelProperty.Description))
                    {
                        title = modelProperty.Description;
                    }
                }
                if (schema.PropertyType == typeof(DateTime))
                {
                    continue;
                }

                if (schema.PropertyType.IsValueType)
                {
                    targetValue = targetValue == null ? "null" : targetValue.ToString();
                }

                builder.AppendFormat("{0}:{1},", title, targetValue);
            }

            return builder.ToString();
        }

        public static DataTable Suggest(DataTable src, string columns, SqlCriteria sqlCriteria, int pageSize)
        {
            int recordCount = 0;

            DataTable dataTable = Query(src, sqlCriteria, null, 1, pageSize, ref recordCount);

            Dictionary<string, string> mapping = new Dictionary<string, string>();
            foreach (string item in columns.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries))
            {
                mapping.Add(item.Trim(), item.Trim());
            }

            return Clone(dataTable, mapping, true);
        }

        public static DataTable Query(DataTable src, SqlCriteria sqlCriteria)
        {
            int recordCount = 0;
            return Query(src, sqlCriteria, null, 0, 0, ref recordCount);
        }

        /// <summary>
        /// Provided for query the data table using condition and  order clause and  pagination in a instance of DataTable.
        /// </summary>
        /// <param name="src">The instance of DataTable.</param>
        /// <param name="sqlCriteria">the condition using the <typeparamref name="SqlCriteria"/></param>
        /// <param name="orderbyClause">Order By Clause.</param>
        /// <param name="pageIndex">the page index.</param>
        /// <param name="pageSize">the page size.</param>
        /// <param name="recordCount">the count of the result.</param>
        /// <returns>the data in the provided condition for the parameter of src.</returns>
        public static DataTable Query(DataTable src, SqlCriteria sqlCriteria, string orderbyClause, int pageIndex, int pageSize, ref int recordCount)
        {
            ThrowExceptionUtil.ArgumentNotNull(src, "src");

            DataTable table = src;

            DataRow[] rows = src.Select(sqlCriteria != null ? sqlCriteria.FilterClause : "", orderbyClause);

            recordCount = rows.Length;
            if (pageIndex >= 0 && pageSize > 0)
            {
                pageIndex = pageIndex == 0 ? 1 : pageIndex;
                rows = rows.Skip((pageIndex - 1) * pageSize).Take(pageSize).ToArray();
            }
            if (rows.Length != 0)
            {
                table = rows.CopyToDataTable();
            }
            else
            {
                table = table.Clone();
            }

            return table;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="dataTable"></param>
        /// <param name="mapping"></param>
        /// <param name="keySrcFlag">是否以mapping的值作为列名</param>
        /// <returns></returns>
        public static DataTable Clone(DataTable dataTable, Dictionary<string, string> mapping, bool keySrcFlag)
        {
            DataTable result = new DataTable();

            if (keySrcFlag)
            {
                foreach (string key in mapping.Keys)
                {
                    DataColumn column = dataTable.Columns[key];
                    ThrowExceptionUtil.ArgumentConditionTrue(column != null, "mapping", "datatable does not contains column:{0}".FormatWith(key));
                    result.Columns.Add(mapping[key], column.DataType);
                }

                foreach (DataRow row in dataTable.Rows)
                {
                    DataRow newRow = result.NewRow();
                    foreach (string key in mapping.Keys)
                    {
                        newRow[mapping[key]] = row[key];
                    }

                    result.Rows.Add(newRow);
                }
            }
            else
            {
                foreach (string key in mapping.Keys)
                {
                    if (dataTable.Columns.Contains(mapping[key]))
                    {
                        result.Columns.Add(key, dataTable.Columns[mapping[key]].DataType);
                    }
                }

                foreach (DataRow row in dataTable.Rows)
                {
                    DataRow newRow = result.NewRow();
                    foreach (DataColumn column in result.Columns)
                    {
                        newRow[column.ColumnName] = row[mapping[column.ColumnName]];
                    }

                    result.Rows.Add(newRow);
                }
            }

            return result;
        }

        /// <summary>
        /// Get a new DataTable contains the new column name and the value in the datarow 
        /// according to the OrmColumnAttribute and ModelPropertyAttribute.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="dataTable"></param>
        /// <returns></returns>
        public static DataTable ForExport<T>(DataTable dataTable) where T : class
        {
            DataTable result = new DataTable();
            Dictionary<string, string> mapping = new Dictionary<string, string>();
            Dictionary<string, string> mappingMapping = new Dictionary<string, string>();
            EntityProxy<T> entityProxy = EntityProxyManager.Instance.GetEntityProxy<T>();
            foreach (PropertySchema propertySchema in entityProxy.GetPropertyList())
            {
                string columnName = propertySchema.Name;
                OrmColumnAttribute ormColumnAttribute = propertySchema.GetCustomerAttribute<OrmColumnAttribute>();
                if (ormColumnAttribute != null && !string.IsNullOrEmpty(ormColumnAttribute.DbColumnName))
                {
                    columnName = ormColumnAttribute.DbColumnName;
                }

                string newColumnName = columnName;
                ModelPropertyAttribute modelPropertyAttribute
                    = propertySchema.GetCustomerAttribute<ModelPropertyAttribute>();
                if (modelPropertyAttribute != null)
                {
                    if (!modelPropertyAttribute.Visible) continue;
                    if (!string.IsNullOrEmpty(modelPropertyAttribute.Description))
                    {
                        newColumnName = modelPropertyAttribute.Description;
                    }

                    if (!string.IsNullOrEmpty(modelPropertyAttribute.MappingName))
                    {
                        mappingMapping.Add(columnName, modelPropertyAttribute.MappingName);
                    }
                }

                mapping.Add(columnName, newColumnName);
                result.Columns.Add(newColumnName);
            }

            foreach (DataRow row in dataTable.Rows)
            {
                DataRow newRow = result.NewRow();

                foreach (string columnName in mapping.Keys)
                {
                    string itemResult = string.Empty;

                    if (mappingMapping.ContainsKey(columnName))
                    {
                        itemResult = DataMapping.Instance.Mapping(mappingMapping[columnName], row[columnName].ToString());
                    }
                    else
                    {
                        itemResult = row[columnName].ToString();
                    }

                    newRow[mapping[columnName]] = itemResult;
                }

                result.Rows.Add(newRow);
            }

            return result;
        }

        public static DataTable ForImport<T>(DataTable dataTable) where T : class
        {
            DataTable result = new DataTable();
            Dictionary<string, string> mapping = new Dictionary<string, string>();
            Dictionary<string, string> mappingMapping = new Dictionary<string, string>();
            EntityProxy<T> entityProxy = EntityProxyManager.Instance.GetEntityProxy<T>();
            foreach (PropertySchema propertySchema in entityProxy.GetPropertyList())
            {
                string columnName = propertySchema.Name;

                string newColumnName = columnName;
                ModelPropertyAttribute modelPropertyAttribute
                    = propertySchema.GetCustomerAttribute<ModelPropertyAttribute>();
                if (modelPropertyAttribute != null)
                {
                    if (!modelPropertyAttribute.Visible) continue;
                    if (!string.IsNullOrEmpty(modelPropertyAttribute.Description))
                    {
                        newColumnName = modelPropertyAttribute.Description;
                    }

                    if (!string.IsNullOrEmpty(modelPropertyAttribute.MappingName))
                    {
                        mappingMapping.Add(newColumnName, modelPropertyAttribute.MappingName);
                    }
                }

                mapping.Add(newColumnName, columnName);
                result.Columns.Add(columnName);
            }

            dataTable.Columns.Add("ImportResult");

            foreach (DataRow row in dataTable.Rows)
            {
                DataRow newRow = result.NewRow();

                foreach (string columnName in mapping.Keys)
                {
                    string itemResult = string.Empty;

                    if (!dataTable.Columns.Contains(columnName))
                    {
                        continue;
                    }

                    if (mappingMapping.ContainsKey(columnName))
                    {
                        DataTable data = DataMapping.Instance.GetMapping(mappingMapping[columnName]) as DataTable;

                        DataRow[] rows = data.Select("name='{0}'".FormatWith(row[columnName]));
                        if (rows.Length == 1)
                        {
                            itemResult = rows[0]["id"].ToString();
                        }
                        else
                        {
                            row["ImportResult"] = "无法匹配{0}:{1}".FormatWith(mappingMapping[columnName], row[columnName]);
                        }
                    }
                    else
                    {
                        itemResult = row[columnName].ToString().Trim();
                    }

                    newRow[mapping[columnName]] = itemResult;
                }

                result.Rows.Add(newRow);
            }

            return result;
        }

        public static DataTable LoadExcel(string path)
        {
            return LoadExcel(path, null);
        }

        public static DataTable LoadExcel(string path, string columns)
        {
            return LoadExcel(path, columns, "Provider=Microsoft.Jet.OLEDB.4.0;Data Source={0};Extended Properties='Excel 8.0;IMEX=1';");
        }

        public static DataTable LoadExcel(string path, string columns, string connStringFormat)
        {
            string connString = connStringFormat.FormatWith(path);
            OleDbConnection connection = new OleDbConnection(connString);
            DataTable dataTable = new DataTable();
            try
            {
                connection.Open();
                dataTable = connection.GetOleDbSchemaTable(OleDbSchemaGuid.Tables, new object[] { null, null, null, "Table" });
                string tname = (from DataRow r in dataTable.Rows select r.Field<string>("TABLE_NAME")).FirstOrDefault();
                if (string.IsNullOrEmpty(tname))
                    return null;

                OleDbDataAdapter adapter = null;
                if (!string.IsNullOrEmpty(columns))
                {

                    dataTable = connection.GetSchema("Columns", new string[] { null, null, tname });
                    foreach (string item in columns.Split(','))
                    {
                        string col = item.Trim();
                        var cs = from DataRow r in dataTable.Rows where string.Compare(col, r.Field<string>("COLUMN_NAME"), true) == 0 select r;

                        if (cs.Count() == 0)
                        {
                            throw new ArgumentException(string.Format("格式不正确，列名【{0}】在Excel文件【{1}】中不存在！", col, path));
                        }
                    }

                    adapter = new OleDbDataAdapter(string.Format("SELECT {0} FROM [{1}]", columns, tname), connection);
                }
                else
                {

                    adapter = new OleDbDataAdapter(string.Format("SELECT * FROM [{0}]", tname), connection);
                }
                dataTable = new DataTable();
                adapter.Fill(dataTable);

            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                connection.Close();
            }

            return dataTable;
        }

        /// <summary>
        /// Provided to format the value for the DataRow.
        /// If the value is DBNull.Value, return string.Empty.
        /// the format of datatime is 'yyyy-MM-dd'.
        /// the format of the double type is '0.00'.
        /// </summary>
        /// <param name="row">The instance of DataRow.</param>
        /// <param name="columnName">the column name.</param>
        /// <returns>the formated result.</returns>
        public static string Format(this DataRow row, string columnName)
        {
            string result = string.Empty;
            if (row.Table.Columns.Contains(columnName))
            {
                object rowValue = row[columnName];
                if (rowValue != DBNull.Value)
                {
                    result = Format(rowValue);
                }
            }

            return result;
        }

        public static string ForDatetime(this DataRow row, string columnName, string format)
        {
            string result = string.Empty;
            if (row.Table.Columns.Contains(columnName))
            {
                object rowValue = row[columnName];

                if (rowValue != DBNull.Value)
                {
                    if (rowValue is DateTime)
                    {
                        result = ((DateTime)rowValue).ToString(format);
                    }
                }
            }
            return result;
        }

        public static string FormatPrice(this DataRow row, string columnName)
        {
            string result = string.Empty;
            object rowValue = row[columnName];
            if (rowValue != DBNull.Value)
            {
                if (rowValue is double)
                {
                    //if ((double)rowValue != 0d)
                    {
                        result = ((double)rowValue).ToString("0.0000");
                    }
                }
                if (rowValue is decimal)
                {
                    //if ((decimal)rowValue != 0)
                    {
                        result = ((decimal)rowValue).ToString("0.0000");
                    }
                }
            }

            return result;
        }

        public static string FormatAmount(this DataRow row, string columnName)
        {
            string result = string.Empty;
            object rowValue = row[columnName];
            if (rowValue != DBNull.Value)
            {
                if (rowValue is double)
                {
                    //if ((double)rowValue != 0d)
                    {
                        result = ((double)rowValue).ToString("0.00");
                    }
                }
                if (rowValue is decimal)
                {
                    //if ((decimal)rowValue != 0)
                    {
                        result = ((decimal)rowValue).ToString("0.00");
                    }
                }
            }

            return result;
        }

        public static string Format(object value)
        {
            string result = string.Empty;
            if (value is double)
            {
                if ((double)value != 0)
                {
                    result = ((double)value).ToString(Constants.NumberFormat);
                }
                else
                {
                    result = "0";
                }
            }
            else if (value is decimal)
            {
                //if ((decimal)rowValue != 0)
                {
                    result = ((decimal)value).ToString(Constants.NumberFormat);
                }
            }
            else if (value is float)
            {
                //if ((decimal)rowValue != 0)
                {
                    result = ((float)value).ToString(Constants.NumberFormat);
                }
            }
            else if (value is DateTime)
            {
                result = ((DateTime)value).ToString(Constants.DateTimeFormat);
            }
            else
            {
                result = value.ToString();
            }

            return result;
        }

        /// <summary>
        /// Provided the extended method for DataTable to find the key value of the children.
        /// </summary>
        /// <param name="dataTable">The instance of the DataTable.</param>
        /// <param name="parentId">the parent id.</param>
        /// <returns>the key values of the children.</returns>
        public static List<int> GetChildren(this DataTable dataTable, int parentId)
        {
            return dataTable.GetChildren("id", "parentid", parentId);
        }

        /// <summary>
        /// Provided the extended method for DataTable to find the key value of the children.
        /// </summary>
        /// <param name="dataTable">The Instance of the DataTable.</param>
        /// <param name="idColumn">the name of id column.</param>
        /// <param name="parentColumn">the name of the parent column.</param>
        /// <param name="parentId">the parent id.</param>
        /// <returns>the key values of the children.</returns>
        public static List<int> GetChildren(this DataTable dataTable, string idColumn, string parentColumn, int parentId)
        {
            List<int> result = new List<int>();
            result.Add(parentId);

            AddChildren(dataTable, result, idColumn, parentColumn, parentId);

            return result;
        }

        private static void AddChildren(DataTable table, List<int> list, string idColumn, string parentColumn, int parentId)
        {
            DataRow[] rows = table.Select(string.Format("{0}={1}", parentColumn, parentId));
            foreach (DataRow item in rows)
            {
                int childId = int.Parse(item[idColumn].ToString());
                list.Add(childId);
                AddChildren(table, list, idColumn, parentColumn, childId);
            }
        }


        public static int CalcLevel(this DataTable table, string idColumn, string parentColumn, int id)
        {
            List<int> result = new List<int>();
            result.Add(id);

            CalcLevel(table, result, idColumn, parentColumn, id);

            return result.Count - 1;
        }

        private static void CalcLevel(DataTable table, List<int> list, string idColumn, string parentColumn, int id)
        {
            DataRow[] rows = table.Select(string.Format("{0}={1}", idColumn, id));

            if (rows.Length == 1)
            {
                int parentId = int.Parse(rows[0][parentColumn].ToString());
                list.Add(parentId);
                CalcLevel(table, list, idColumn, parentColumn, parentId);
            }
        }
    }
}
