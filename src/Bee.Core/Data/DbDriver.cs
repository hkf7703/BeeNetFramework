using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Common;
using Bee.Util;
using System.Configuration;
using System.Text.RegularExpressions;
using System.IO;
using System.Data;

namespace Bee.Data
{
    internal enum DBServerType
    {
        Sqlserver,
        Oracle,
        MySql,
        Sqlite,
        Ole,
        Pgsql
    }

    internal class DbDriver
    {
        protected Regex connectionStringRegex = new Regex("(?<name>data source)=(?<value>.*?);", RegexOptions.IgnoreCase);
        protected Regex GrepLengthRegex = new Regex(@"(?<name>.*)\((?<length>\d+)\)");
        protected DbProviderFactory innerProviderFactory;
        protected string connectionString;
        protected string connectionName;
        protected Dictionary<string, Type> dbTypeMap = new Dictionary<string, Type>(StringComparer.CurrentCultureIgnoreCase);

        protected static readonly NotSupportedException DoesNotSupportException =
            new NotSupportedException("does not support now!");

        public DbDriver(DbProviderFactory dbProviderFactory, string connectionString, string connectionName)
        {
            this.innerProviderFactory = dbProviderFactory;
            this.connectionString = connectionString;
            this.connectionName = connectionName;
        }

        internal string ConnectionName
        {
            get
            {
                return this.connectionName;
            }
        }

        public virtual DbConnection CreateConnection()
        {
            DbConnection dbConnection = innerProviderFactory.CreateConnection();
            dbConnection.ConnectionString = this.connectionString;
            return dbConnection;
        }

        public virtual DbCommand CreateCommand()
        {
            return innerProviderFactory.CreateCommand();
        }

        public virtual DbParameter CreateParameter()
        {
            return innerProviderFactory.CreateParameter();
        }


        public virtual DbDataAdapter CreateDataAdapter()
        {
            return this.innerProviderFactory.CreateDataAdapter();
        }

        public virtual DbCommandBuilder CreateCommandBuilder()
        {
            return innerProviderFactory.CreateCommandBuilder();
        }

        public virtual string DbNowString
        {
            get { return "getdate()"; }
        }

        public virtual Dictionary<string, Type> DbTypeMap
        {
            get
            {
                return this.dbTypeMap;
            }
        }

        public virtual string ParameterPrefix
        {
            get
            {
                return "@";
            }
        }

        public virtual string FormatField(string field)
        {
            return string.Format("[{0}]", field);
        }

        public virtual string IdentitySelectString
        {
            get { return "SCOPE_IDENTITY()"; }
        }

        public virtual string GetPagedSelectCmdText(string cmdText, string orderbySql, int pageIndex, int pageSize)
        {
            throw DoesNotSupportException;
        }

        public virtual string GetPagedSelectCmdText(string columnsSql, string fromSql, string whereSql, string orderbySql,
            int pageIndex, int pageSize)
        {
            throw DoesNotSupportException;
        }

        public virtual List<DbObject> GetDbObjectList()
        {
            throw DoesNotSupportException;
        }

        public virtual TableSchema GetTableSchema(string tableName)
        {
            throw DoesNotSupportException;
        }

        public virtual SPSchema GetSpSchema(string spName)
        {
            throw DoesNotSupportException;
        }

        public virtual string ToCreateTableScript(TableSchema tableSchema)
        {
            return string.Empty;
        }

        public override string ToString()
        {
            return this.connectionName;
        }

        protected virtual DataTable ExecuteCommand(string cmdText, BeeDataAdapter dataAdapter)
        {
            return DbSession.Current.ExecuteCommand(cmdText, dataAdapter);
        }

    }

    internal class SqlServer2000Driver : DbDriver
    {
        public SqlServer2000Driver(DbProviderFactory dbProviderFactory, string connectionString, string connectionKey)
            :base(dbProviderFactory, connectionString, connectionKey)
        {
            dbTypeMap.Add("bit", typeof(bool));
            dbTypeMap.Add("bigint", typeof(long));
            dbTypeMap.Add("binary", typeof(byte[]));
            dbTypeMap.Add("char", typeof(string));
            dbTypeMap.Add("datetime", typeof(DateTime));
            dbTypeMap.Add("decimal", typeof(decimal));
            dbTypeMap.Add("numeric", typeof(decimal));
            dbTypeMap.Add("float", typeof(double));
            dbTypeMap.Add("real", typeof(double));
            dbTypeMap.Add("int", typeof(int));
            dbTypeMap.Add("image", typeof(byte[]));
            dbTypeMap.Add("money", typeof(decimal));
            dbTypeMap.Add("smallmoney", typeof(decimal));
            dbTypeMap.Add("nchar", typeof(string));
            dbTypeMap.Add("smallint", typeof(int));
            dbTypeMap.Add("smalldatetime", typeof(DateTime));
            dbTypeMap.Add("text", typeof(string));
            dbTypeMap.Add("ntext", typeof(string));
            dbTypeMap.Add("tinyint", typeof(int));
            dbTypeMap.Add("varbinary", typeof(byte[]));
            dbTypeMap.Add("varchar", typeof(string));
            dbTypeMap.Add("nvarchar", typeof(string));
            dbTypeMap.Add("uniqueidentifier", typeof(Guid));
        }

        public override string GetPagedSelectCmdText(string columnsSql, string fromSql, string whereSql, string orderbySql
            ,int pageIndex, int pageSize) 
        {
            pageIndex = pageIndex < 1 ? 1 : pageIndex;
            string result = string.Empty;

                result = string.Format(
@"select top {4} {0} 
from {1} 
where {2} 
{3}",
    columnsSql, fromSql, whereSql,
    orderbySql, pageIndex * pageSize);
 
            return result;
        }


        public override string ToCreateTableScript(TableSchema tableSchema)
        {
            ThrowExceptionUtil.ArgumentConditionTrue(tableSchema.IdentityColumn != null, tableSchema.TableName, "Need a primary key.");

            StringBuilder stringBuilder = new StringBuilder();

            string tableTemplate = @"
CREATE TABLE [dbo].[{0}](
{1}

)";

            foreach (ColumnSchema columnSchema in tableSchema.ColumnList)
            {
                if (string.IsNullOrEmpty(columnSchema.ColumnType) 
                    || !dbTypeMap.ContainsKey(columnSchema.ColumnType))
                {
                    foreach (string item in DbTypeMap.Keys)
                    {
                        if (DbTypeMap[item] == columnSchema.Type)
                        {
                            columnSchema.ColumnType = item;
                            break;
                        }
                    }
                }

                bool ignoreLengthFlag = columnSchema.ColumnType.IndexOf("char", StringComparison.CurrentCultureIgnoreCase) < 0;

                string columnType = ignoreLengthFlag ? columnSchema.ColumnType
                    : string.Format("{0}({1})", columnSchema.ColumnType, columnSchema.MaxLength);
                if (columnSchema.IsIdentity)
                {
                    columnType = "INT";
                }

                stringBuilder.AppendFormat("[{0}] {1} {2} {3} {4},\r\n", columnSchema.ColumnName, columnType,
                    columnSchema.IsIdentity ? "IDENTITY(1,1)" : "", columnSchema.IsNullable && !columnSchema.IsIdentity ? "NULL" : "NOT NULL"
                    , columnSchema.IsIdentity ? "primary key" : "");
            }

            stringBuilder.Remove(stringBuilder.Length - 3, 3);

            return string.Format(tableTemplate, tableSchema.TableName, stringBuilder.ToString());
        }
        
    }

    internal class SqlServer2005Driver : SqlServer2000Driver
    {
        public SqlServer2005Driver(DbProviderFactory dbProviderFactory, string connectionString, string connectionKey)
            :base(dbProviderFactory, connectionString, connectionKey)
        {
        }

        public override string GetPagedSelectCmdText(string cmdText, string orderbySql, int pageIndex, int pageSize)
        {
            pageIndex = pageIndex < 1 ? 1 : pageIndex;

            string sql = @"with beetemp as ({0})
select *
from (
select *, ROW_NUMBER() over (order by {1}) as beerow_number
from beetemp
) T
where beerow_number > {2} and beerow_number <= {3}
            ".FormatWith(cmdText, orderbySql, (pageIndex - 1) * pageSize, pageIndex * pageSize);

            return sql;
        }

        public override string GetPagedSelectCmdText(string columnsSql, string fromSql, string whereSql,
            string orderbySql, int pageIndex, int pageSize)
        {
            pageIndex = pageIndex < 1 ? 1 : pageIndex;
            string result = string.Empty;

            if (pageIndex == 1)
            {
                result = string.Format(
    @"select top {4} {0} 
from {1} 
where {2} 
{3}",
    columnsSql, fromSql, whereSql,
    orderbySql, pageIndex * pageSize);
            }
            else
            {
                result = string.Format(
    @"select {0}
from (
select {0}, ROW_NUMBER() over ({3}) as beerow_number
from {1}
where {2}
) T
where beerow_number > {4} and beerow_number <= {5}",
    columnsSql, fromSql, whereSql,
    orderbySql, (pageIndex - 1) * pageSize, pageIndex * pageSize);
            }

            return result;
        }

        public override List<DbObject> GetDbObjectList()
        {
            List<DbObject> dbObjectList = new List<DbObject>();
            DataTable dataTable = 
                ExecuteCommand("select [name], [type] from sys.objects where type in('U','V','P') order by name", null);
            if (dataTable != null)
            {
                foreach (DataRow row in dataTable.Rows)
                {
                    string dbObjectName = row["Name"].ToString();
                    string typeName = row["type"].ToString();
                    DbObjectType objectType = DbObjectType.Table;

                    if (!(typeName == "V "))
                    {
                        if (typeName == "P ")
                        {
                            objectType = DbObjectType.SP;
                        }
                    }
                    else
                    {
                        objectType = DbObjectType.View;
                    }
                    dbObjectList.Add(new DbObject(dbObjectName, objectType));
                }
            }

            return dbObjectList;

        }

        public override TableSchema GetTableSchema(string tableName)
        {
            TableSchema tableSchema = new TableSchema(tableName);

            
            BeeDataAdapter dataAdapter = new BeeDataAdapter();
            dataAdapter.Add("objId", tableName);

            /* Sqlserver 2005 不支持
            DataTable dataTable = ExecuteCommand("sp_pkeys @objId", dataAdapter);
             * 
             */
            DataTable dataTable = ExecuteCommand("sp_pkeys [{0}]".FormatWith(tableName), null);
            string primaryKey = string.Empty;
            if (dataTable != null && dataTable.Rows.Count != 0)
            {
                primaryKey = dataTable.Rows[0]["column_name"].ToString();
            }

            dataTable = ExecuteCommand(@"select a.name, type_name(user_type_id) type, is_nullable, is_identity, 
COLUMNPROPERTY(object_id, a.name,'PRECISION') as maxlength, b.value as description, a.is_computed
from sys.columns a left join sys.extended_properties b
on object_id =  major_id and  column_id = minor_id                                
where object_id(@objId) = object_id", dataAdapter);

            if (dataTable != null)
            {
                foreach (DataRow row in dataTable.Rows)
                {
                    string columnName = row["Name"].ToString();
                    string columnType = row["type"].ToString();
                    bool allowNull = (bool)row["is_nullable"];
                    bool autoIncreased = (bool)row["is_identity"];
                    int maxlength = int.Parse(row["maxlength"].ToString());
                    string description = row["description"].ToString();

                    bool isComputed = row["is_computed"].ToString() == "1";

                    bool isPrimary = false;
                    if (string.Compare(columnName, primaryKey, true) == 0)
                    {
                        isPrimary = true;
                    }
                    ColumnSchema columnSchema = new ColumnSchema(columnName);
                    columnSchema.ColumnType = columnType;
                    columnSchema.Description = description;
                    columnSchema.IsIdentity = autoIncreased;
                    columnSchema.IsNullable = allowNull;
                    columnSchema.IsPrimary = isPrimary;
                    columnSchema.MaxLength = maxlength;
                    columnSchema.IsComputeField = isComputed;
                    columnSchema.Type = this.DbTypeMap[columnType];
                    tableSchema.ColumnList.Add(columnSchema);

                }
            }

            //DataTable fkeysTable = ExecuteCommand("sp_fkeys @objId", dataAdapter);
            DataTable fkeysTable = ExecuteCommand("sp_fkeys [{0}]".FormatWith(tableName), null);
            if (fkeysTable != null)
            {
                foreach (DataRow row in fkeysTable.Rows)
                {
                    string columnName = row["PKColumn_Name"].ToString();
                    string foreignTableName = row["FKTable_Name"].ToString();
                    string foreignColumnName = row["FKColumn_Name"].ToString();
                    ForeignKey foreignKey = new ForeignKey(columnName, foreignTableName, foreignColumnName);
                    tableSchema.ForeignKeyList.Add(foreignKey);
                }
            }

            if (tableSchema != null && tableSchema.ColumnList.Count == 0)
            {
                tableSchema = null;
            }

            return tableSchema;
        }

        public override SPSchema GetSpSchema(string spName)
        {
            SPSchema result = new SPSchema(spName);
            
            BeeDataAdapter dataAdapter = new BeeDataAdapter();
            dataAdapter.Add("objId", spName);

            DataTable dataTable = ExecuteCommand(@"select name,TYPE_NAME(user_type_id) type,is_output
, COLUMNPROPERTY(object_id, name, 'PRECISION') as max_length
from sys.parameters
where object_id(@objId) = object_id", dataAdapter);

            if (dataTable != null)
            {
                foreach (DataRow row in dataTable.Rows)
                {
                    string name = row["name"].ToString().Substring(1);
                    string dbType = row["type"].ToString();
                    ParameterDirection direction = row["is_output"].ToString() == "1" || row["is_output"].ToString() == "True"
                        ? ParameterDirection.InputOutput : ParameterDirection.Input;
                    int maxLength = 0;
                    if (row["max_length"] != null)
                    {
                        int.TryParse(row["max_length"].ToString(), out maxLength);
                    }

                    SPParameter parameter = new SPParameter(name, dbType, maxLength, direction);

                    result.ParameterList.Add(parameter);
                }
            }

            return result;
        }
    }

    internal class SqlServer2008Driver : SqlServer2005Driver
    {
        public SqlServer2008Driver(DbProviderFactory dbProviderFactory, string connectionString, string connectionKey)
            :base(dbProviderFactory, connectionString, connectionKey)
        {
        }
    }

    internal class OleDriver : SqlServer2000Driver
    {
        public OleDriver(DbProviderFactory dbProviderFactory, string connectionString, string connectionKey)
            :base(dbProviderFactory, connectionString, connectionKey)
        {
        }

        public override string DbNowString
        {
            get
            {
                return "Now()";
            }
        }

        public override string IdentitySelectString
        {
            get { return "@@IDENTITY"; }
        }

    }

    internal class MySqlDriver : DbDriver
    {
        public MySqlDriver(DbProviderFactory dbProviderFactory, string connectionString, string connectionKey)
            :base(dbProviderFactory, connectionString, connectionKey)
        {
            dbTypeMap.Add("bit", typeof(bool));
            dbTypeMap.Add("bigint", typeof(long));
            dbTypeMap.Add("binary", typeof(byte[]));
            dbTypeMap.Add("char", typeof(string));
            dbTypeMap.Add("datetime", typeof(DateTime));
            dbTypeMap.Add("date", typeof(DateTime));
            dbTypeMap.Add("decimal", typeof(decimal));
            dbTypeMap.Add("numeric", typeof(decimal));
            dbTypeMap.Add("float", typeof(double));
            dbTypeMap.Add("real", typeof(double));
            dbTypeMap.Add("int", typeof(int));
            dbTypeMap.Add("image", typeof(byte[]));
            dbTypeMap.Add("money", typeof(decimal));
            dbTypeMap.Add("smallmoney", typeof(decimal));
            dbTypeMap.Add("nchar", typeof(string));
            dbTypeMap.Add("smallint", typeof(int));
            dbTypeMap.Add("smalldatetime", typeof(DateTime));
            dbTypeMap.Add("text", typeof(string));
            dbTypeMap.Add("ntext", typeof(string));
            dbTypeMap.Add("tinyint", typeof(int));
            dbTypeMap.Add("varbinary", typeof(byte[]));
            dbTypeMap.Add("varchar", typeof(string));
            dbTypeMap.Add("nvarchar", typeof(string));
            dbTypeMap.Add("uniqueidentifier", typeof(Guid));
        }

        public override string IdentitySelectString
        {
            get { return "LAST_INSERT_ID()"; }
        }

        public override string ParameterPrefix
        {
            get
            {
                return "?";
            }
        }

        public override string DbNowString
        {
            get
            {
                return "now()";
            }
        }

        public override string FormatField(string field)
        {
            return string.Format("`{0}`", field);
        }

        public override string GetPagedSelectCmdText(string columnsSql, string fromSql, string whereSql, string orderbySql
            , int pageIndex, int pageSize)
        {
            pageIndex = pageIndex < 1 ? 1 : pageIndex;
            string result = string.Empty;

            result = string.Format(
@"select {0} 
from {1} 
where {2} 
{3}
limit {4},{5}",
columnsSql, fromSql, whereSql,
orderbySql, (pageIndex-1) * pageSize, pageSize);

            return result;
        }

        public override List<DbObject> GetDbObjectList()
        {
            string db = "test";
            List<DbObject> dbObjectList = new List<DbObject>();
            DataTable dataTable =
                ExecuteCommand(string.Format(@"select table_name as name, table_type as type 
from information_schema.tables where table_schema='{0}'
union all 
select name, 'p' as type
from mysql.proc
;", db), null);
            if (dataTable != null)
            {
                foreach (DataRow row in dataTable.Rows)
                {
                    string dbObjectName = row["Name"].ToString();
                    string typeName = row["type"].ToString();
                    DbObjectType objectType = DbObjectType.Table;

                    if (!(typeName == "VIEW"))
                    {
                        if (typeName == "p")
                        {
                            objectType = DbObjectType.SP;
                        }
                    }
                    else
                    {
                        objectType = DbObjectType.View;
                    }
                    dbObjectList.Add(new DbObject(dbObjectName, objectType));
                }
            }

            return dbObjectList;
        }

        public override TableSchema GetTableSchema(string tableName)
        {
            TableSchema tableSchema = new TableSchema(tableName);

            BeeDataAdapter dataAdapter = new BeeDataAdapter();
            dataAdapter.Add("objId", tableName);

            DataTable dataTable = null;

            dataTable = ExecuteCommand(@"select column_name as name, data_type as type
, case is_nullable when 'YES' then true else false end as is_nullable
, extra , column_key, column_comment, character_maximum_length as maxlength
#select * 
from information_schema.columns
where table_name = ?objId", dataAdapter);

            if (dataTable != null)
            {
                foreach (DataRow row in dataTable.Rows)
                {
                    string columnName = row["name"].ToString();
                    string columnType = row["type"].ToString();
                    bool allowNull = row["is_nullable"].ToString() == "1";
                    bool autoIncreased = false;
                    if (row["extra"] != null && row["extra"].ToString().Contains("auto_increment"))
                    {
                        autoIncreased = true;
                    }
                    int maxlength = 0;
                    int.TryParse(row["maxlength"].ToString(), out maxlength);
                    string description = row["column_comment"].ToString();
                    bool isPrimary = false;
                    if (row["column_key"] != null && row["column_key"].ToString().Contains("PRI"))
                    {
                        isPrimary = true;
                    }
                    ColumnSchema columnSchema = new ColumnSchema(columnName);
                    columnSchema.ColumnType = columnType;
                    columnSchema.Description = description;
                    columnSchema.IsIdentity = autoIncreased;
                    columnSchema.IsNullable = allowNull;
                    columnSchema.IsPrimary = isPrimary;
                    columnSchema.MaxLength = maxlength;
                    columnSchema.Type = this.DbTypeMap[columnType];
                    tableSchema.ColumnList.Add(columnSchema);

                }
            }

            return tableSchema;
        }

        public override SPSchema GetSpSchema(string spName)
        {
            SPSchema result = new SPSchema(spName);

            BeeDataAdapter dataAdapter = new BeeDataAdapter();
            dataAdapter.Add("objId", spName);

            DataTable dataTable = ExecuteCommand(@"select param_list
from mysql.proc
where name = ?objId", dataAdapter);

            if (dataTable != null && dataTable.Rows.Count == 1)
            {
                byte[] data = dataTable.Rows[0][0] as byte[];

                string dataValue = Encoding.UTF8.GetString(data);

                foreach (string item in dataValue.Split(','))
                {
                    string name = string.Empty;
                    string dbType = string.Empty;
                    int maxLength = 0;
                    ParameterDirection direction = ParameterDirection.Input;

                    string[] paramArray = item.Trim().Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    if (paramArray.Length == 2)
                    {
                        name = paramArray[0];
                        dbType = paramArray[1];
                    }
                    else if(paramArray.Length == 3)
                    {
                        if (paramArray[0].IndexOf("out", StringComparison.CurrentCultureIgnoreCase) >= 0)
                        {
                            direction = ParameterDirection.InputOutput;
                        }
                        name = paramArray[1];
                        dbType = paramArray[2];
                    }

                    if (string.IsNullOrEmpty(name))
                    {
                        continue;
                    }
                    SPParameter parameter = new SPParameter(name, dbType, maxLength, direction);

                    result.ParameterList.Add(parameter);
                }
            }

            return result;
        }

        public override string ToCreateTableScript(TableSchema tableSchema)
        {
            ThrowExceptionUtil.ArgumentConditionTrue(tableSchema.IdentityColumn != null, tableSchema.TableName, "Need a primary key.");

            StringBuilder stringBuilder = new StringBuilder();

            string tableTemplate = @"

CREATE TABLE `{0}`(
{2}
  PRIMARY KEY  (`{1}`)
) ENGINE=MyISAM DEFAULT CHARSET=utf8;";

            foreach (ColumnSchema columnSchema in tableSchema.ColumnList)
            {
                if (string.IsNullOrEmpty(columnSchema.ColumnType)
                    || !dbTypeMap.ContainsKey(columnSchema.ColumnType))
                {
                    foreach (string item in DbTypeMap.Keys)
                    {
                        if (DbTypeMap[item] == columnSchema.Type)
                        {
                            columnSchema.ColumnType = item;
                            break;
                        }
                    }
                }

                string columnSchemaColumnType = columnSchema.ColumnType.ToLower().Replace("nvar", "var").Replace("ntext", "text");
                bool ignoreLengthFlag = columnSchemaColumnType.IndexOf("char") < 0;

                string columnType = ignoreLengthFlag ? columnSchemaColumnType
                    : string.Format("{0}({1})", columnSchemaColumnType, columnSchema.MaxLength);
                if (columnSchema.IsIdentity)
                {
                    columnType = "INT";
                }

                stringBuilder.AppendFormat("`{0}` {1} {2} {3},\r\n", columnSchema.ColumnName, columnType,
                    columnSchema.IsIdentity ? "auto_increment" : "", columnSchema.IsNullable && !columnSchema.IsIdentity ? "NULL" : "NOT NULL");
            }

            return string.Format(tableTemplate, tableSchema.TableName, tableSchema.IdentityColumn.ColumnName, stringBuilder.ToString());
        }

    }

    internal class OracleDriver : DbDriver
    {
        public OracleDriver(DbProviderFactory dbProviderFactory, string connectionString, string connectionKey)
            :base(dbProviderFactory, connectionString, connectionKey)
        {
            dbTypeMap.Add("NVARCHAR2", typeof(string));
            dbTypeMap.Add("VARCHAR", typeof(string));
            dbTypeMap.Add("VARCHAR2", typeof(string));

            dbTypeMap.Add("DATE", typeof(DateTime));
            dbTypeMap.Add("TIMESTAMP", typeof(DateTime));

            dbTypeMap.Add("NUMBER", typeof(decimal));
            dbTypeMap.Add("NUMERIC", typeof(decimal));

            dbTypeMap.Add("char", typeof(string));
            dbTypeMap.Add("nchar", typeof(string));
            dbTypeMap.Add("clob", typeof(string));
            dbTypeMap.Add("blob", typeof(string));
            dbTypeMap.Add("smallint", typeof(int));

            dbTypeMap.Add("int", typeof(int));
            dbTypeMap.Add("integer", typeof(int));

            dbTypeMap.Add("long", typeof(long));
        }

        public override string DbNowString
        {
            get { return "SYSDATE"; }
        }

        public override string ParameterPrefix
        {
            get
            {
                return ":";
            }
        }

        public override string FormatField(string field)
        {
            return field;
        }

        public override string IdentitySelectString
        {
            get 
            {
                return " seq_{0}_id.currval from dual"; 
            }
        }

        public override string GetPagedSelectCmdText(string columnsSql, string fromSql, string whereSql, string orderbySql,
            int pageIndex, int pageSize)
        {
            pageIndex = pageIndex < 1 ? 1 : pageIndex;
            string result = string.Empty;

            result = string.Format(
@"SELECT {0} FROM 
( SELECT ROW_.*, ROWNUM ROWNUM_ 
FROM ( select {0} 
       from {1}
       where {2}
       {3}) ROW_ 
WHERE ROWNUM <= {5}
) WHERE ROWNUM_ > {4}",
columnsSql, fromSql, whereSql,
orderbySql, (pageIndex - 1) * pageSize, pageSize * pageIndex);

            return result;
        }

        public override TableSchema GetTableSchema(string tableName)
        {
            TableSchema tableSchema = new TableSchema(tableName);

            tableName = tableName.ToLower();
            BeeDataAdapter dataAdapter = new BeeDataAdapter();
            dataAdapter.Add("objId", tableName);

            DataTable dataTable = ExecuteCommand(@"select col.column_name    
from user_constraints con,  user_cons_columns col    
where con.constraint_name = col.constraint_name    
and con.constraint_type='P'    
and lower(col.table_name) = :objId ", dataAdapter);

            string primaryKey = string.Empty;
            if (dataTable != null && dataTable.Rows.Count != 0)
            {
                primaryKey = dataTable.Rows[0]["column_name"].ToString();
            }

            dataTable = ExecuteCommand(@"select a.COLUMN_NAME as Name,DATA_TYPE as type,DATA_PRECISION,DATA_SCALE, NULLABLE as is_nullable, data_length as maxlength, b.comments as description
, 0 as is_identity
from user_tab_columns   a left join  user_col_comments b on a.table_name = b.table_name and a.column_name = b.column_name
where lower(a.table_name) = :objId 
union all 
select a.COLUMN_NAME as Name,DATA_TYPE as type,DATA_PRECISION,DATA_SCALE, NULLABLE as is_nullable, data_length as maxlength, b.comments as description
, 0 as is_identity
from   user_SYNONYMS c left join all_tab_columns a  on a.owner = c.table_owner and a.table_name = c.table_name
left join  user_col_comments b on a.table_name = b.table_name and a.column_name = b.column_name
where lower(c.SYNONYM_NAME) = :objId

", dataAdapter);

            if (dataTable != null)
            {
                foreach (DataRow row in dataTable.Rows)
                {
                    string columnName = row["Name"].ToString();

                    ColumnSchema columnSchema = new ColumnSchema(columnName);
                    
                    string columnType = row["type"].ToString();
                    if (columnType == "NUMBER")
                    {
                        int data_precision = 0;
                        int data_scale = 0;
                        int.TryParse(row["DATA_PRECISION"].ToString(), out data_precision);
                        int.TryParse(row["DATA_SCALE"].ToString(), out data_scale);
                        if (data_scale == 0)
                        {
                            if (data_precision > 10 || data_precision == 0)
                            {
                                columnSchema.Type = typeof(long);
                            }
                            else
                            {
                                columnSchema.Type = typeof(int);
                            }
                        }
                        else
                        {
                            columnSchema.Type = typeof(Decimal);
                        }
                    }
                    else
                    {
                        columnSchema.Type = dbTypeMap[columnType];
                    }

                    bool allowNull = row["is_nullable"].ToString() == "Y";
                    bool autoIncreased = string.Compare("id", columnName, true) == 0;
                    int maxlength = int.Parse(row["maxlength"].ToString());
                    string description = row["description"].ToString();
                    bool isPrimary = false;
                    if (string.Compare(columnName, primaryKey, true) == 0)
                    {
                        isPrimary = true;
                    }
                   
                    columnSchema.ColumnType = columnType;
                    columnSchema.Description = description;
                    columnSchema.IsIdentity = autoIncreased;
                    columnSchema.IsNullable = allowNull;
                    columnSchema.IsPrimary = isPrimary;
                    columnSchema.MaxLength = maxlength;
                    
                    tableSchema.ColumnList.Add(columnSchema);

                }
            }

            return tableSchema;
        }

        public override SPSchema GetSpSchema(string spName)
        {
            spName = spName.ToLower();
            SPSchema result = new SPSchema(spName);
            BeeDataAdapter dataAdapter = new BeeDataAdapter();
            dataAdapter.Add("objId", spName);

            // Oracle 的存储过程参数列表是有顺序的。
            DataTable dataTable = ExecuteCommand(@"
select *
from (
select argument_name as name, data_type as type, in_out as inout, data_length as max_length, sequence
from sys.user_arguments 
where lower(object_name) = :objId
union all
 select argument_name as name, data_type as type, in_out as inout, data_length as max_length, sequence
from   user_SYNONYMS c left join all_arguments  a  on a.owner = c.table_owner and a.object_name = c.table_name
where  lower(object_name) = :objId
) t
order by sequence
", dataAdapter);

            if (dataTable != null)
            {
                foreach (DataRow row in dataTable.Rows)
                {
                    string name = row["name"].ToString();
                    string dbType = row["type"].ToString();
                    string inout = row["inout"].ToString();
                    ParameterDirection direction = ParameterDirection.Input;
                    switch (inout)
                    {
                        case "IN":
                            direction = ParameterDirection.Input;
                            break;
                        case "OUT":
                            direction = ParameterDirection.Output;
                            break;
                        case "IN/OUT":
                            direction = ParameterDirection.InputOutput;
                            break;
                        default:
                            direction = ParameterDirection.Input;
                            break;
                    }

                    int maxLength = 0;
                    if (row["max_length"] != null)
                    {
                        int.TryParse(row["max_length"].ToString(), out maxLength);
                    }

                    SPParameter parameter = new SPParameter(name, dbType, maxLength, direction);

                    result.ParameterList.Add(parameter);
                }
            }

            return result;
        }

        public override List<DbObject> GetDbObjectList()
        {
            DataTable dataTable = ExecuteCommand(@"select *
from (
select table_name as name
from user_tables
union all
select view_name as name
from user_views
union all
select SYNONYM_NAME as name
from user_SYNONYMS 
) t
order by name", null);

            List<DbObject> result = new List<DbObject>();
            if (dataTable != null)
            {
                foreach (DataRow row in dataTable.Rows)
                {
                    DbObject dbObject = new DbObject(row["name"].ToString(), DbObjectType.Table);

                    result.Add(dbObject);
                }
            }

            return result;
        }

        public override string ToCreateTableScript(TableSchema tableSchema)
        {
            ThrowExceptionUtil.ArgumentConditionTrue(tableSchema.IdentityColumn != null, tableSchema.TableName, "Need a primary key.");


            StringBuilder stringBuilder = new StringBuilder();

            string tableTemplate = @"
CREATE TABLE {0}(
{1}
);

CREATE SEQUENCE SEQ_{0}_ID NOMAXVALUE NOCYCLE;

CREATE OR REPLACE TRIGGER TRG_BEF_IST_{0}_ID BEFORE
  INSERT ON {0} FOR EACH ROW 
BEGIN
  IF :NEW.{2} IS NULL THEN
    SELECT SEQ_{0}_ID.NEXTVAL
    INTO :NEW.{2}
    FROM DUAL;
  END IF;
END;
";

            foreach (ColumnSchema columnSchema in tableSchema.ColumnList)
            {
                string columnType = string.Empty;
                if (columnSchema.Type == typeof(string))
                {
                    columnType = string.Format("NVARCHAR2({0})", columnSchema.MaxLength > 0 ? columnSchema.MaxLength : 200);
                }
                else if (columnSchema.Type == typeof(int))
                {
                    columnType = "NUMBER(9,0)";
                }
                else if (columnSchema.Type == typeof(decimal) || columnSchema.Type == typeof(float))
                {
                    columnType = "NUMBER";
                }
                else if (columnSchema.Type == typeof(bool))
                {
                    columnType = "NUMBER(1)";
                }
                else if (columnSchema.Type == typeof(DateTime))
                {
                    columnType = "Date";
                }

                stringBuilder.AppendFormat(@"{0} {1} {2} {3} ,
", columnSchema.ColumnName, columnType
                    , columnSchema.IsNullable && !columnSchema.IsIdentity ? "NULL" : "NOT NULL"
                    , columnSchema.IsIdentity ? "primary key" : "");
            }

            stringBuilder.Remove(stringBuilder.Length - 3, 3);

            return string.Format(tableTemplate, tableSchema.TableName, stringBuilder.ToString(), tableSchema.IdentityColumn.ColumnName);
        }
    }

    internal class SqliteDriver : DbDriver
    {
        public SqliteDriver(DbProviderFactory dbProviderFactory, string connectionString, string connectionKey)
            :base(dbProviderFactory, connectionString, connectionKey)
        {
            
            dbTypeMap.Add("boolean", typeof(bool));
            dbTypeMap.Add("bit", typeof(bool));
            dbTypeMap.Add("bool", typeof(bool));
            dbTypeMap.Add("bigint", typeof(long));
            dbTypeMap.Add("date", typeof(DateTime));
            dbTypeMap.Add("datetime", typeof(DateTime));
            dbTypeMap.Add("decimal", typeof(decimal));
            dbTypeMap.Add("numeric", typeof(decimal));
            dbTypeMap.Add("float", typeof(float));
            dbTypeMap.Add("real", typeof(double));
            dbTypeMap.Add("double", typeof(double));
            dbTypeMap.Add("int", typeof(int));
            dbTypeMap.Add("integer", typeof(int));
            dbTypeMap.Add("image", typeof(byte[]));
            dbTypeMap.Add("money", typeof(decimal));
            dbTypeMap.Add("smallint", typeof(int));
            dbTypeMap.Add("smalldatetime", typeof(DateTime));
            dbTypeMap.Add("nvarchar", typeof(string));
            dbTypeMap.Add("text", typeof(string));
            dbTypeMap.Add("ntext", typeof(string));
            dbTypeMap.Add("tinyint", typeof(int));
            dbTypeMap.Add("varchar", typeof(string));
            
            dbTypeMap.Add("nvarchar2", typeof(string));
            dbTypeMap.Add("guid", typeof(Guid));


            Match match = connectionStringRegex.Match(connectionString);
            if (match.Success)
            {
                if (connectionString.IndexOf(":memory:", StringComparison.InvariantCultureIgnoreCase) < 0)
                {
                    string filepath = match.Groups["value"].Value;

                    filepath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, filepath);

                    this.connectionString = 
                        connectionStringRegex.Replace(connectionString,
                        "data source={0};".FormatWith(filepath));
                }
            }
        }

        public override string DbNowString
        {
            get { return "DATETIME(CURRENT_TIMESTAMP, 'localtime')"; }
        }

        public override string IdentitySelectString
        {
            get { return "LAST_INSERT_ROWID()"; }
        }

        public override List<DbObject> GetDbObjectList()
        {
            List<DbObject> dbObjectList = new List<DbObject>();
            DataTable dataTable =
                ExecuteCommand("select name from sqlite_master where (type='table' or type='view') and name not like 'sqlite_%'", null);

            if (dataTable != null)
            {
                foreach (DataRow row in dataTable.Rows)
                {
                    string dbObjectName = row["Name"].ToString();

                    if (string.Compare(dbObjectName, "sqlite_sequence", true) != 0)
                    {
                        DbObjectType objectType = DbObjectType.Table;

                        dbObjectList.Add(new DbObject(dbObjectName, objectType));
                    }
                }
            }


            return dbObjectList;
        }

        public override TableSchema GetTableSchema(string tableName)
        {
            TableSchema tableSchema = new TableSchema(tableName);

            BeeDataAdapter dataAdapter = new BeeDataAdapter();
            dataAdapter.Add("objId", tableName);
            //DataTable dataTable = ExecuteCommand("PRAGMA table_info(@objId);", dataAdapter);
            DataTable dataTable = ExecuteCommand(string.Format("PRAGMA table_info([{0}]);", tableName), null);

            if (dataTable != null && dataTable.Rows.Count > 0)
            {
                foreach (DataRow row in dataTable.Rows)
                {
                    string columnName = row["Name"].ToString();
                    int maxlength = 0;
                    string columnType = row["type"].ToString();
                    Match match = GrepLengthRegex.Match(columnType);
                    if (match.Success)
                    {
                        columnType = match.Groups["name"].Value;
                        maxlength = int.Parse(match.Groups["length"].Value);
                    }

                    bool allowNull = Convert.ToInt32(row["notnull"]) == 0;
                    bool autoIncreased = Convert.ToInt32(row["pk"]) == 1;

                    if (string.IsNullOrEmpty(columnType))
                    {
                        columnType = "INTEGER";
                    }

                    string description = string.Empty;
                    bool isPrimary = autoIncreased;
                    ColumnSchema columnSchema = new ColumnSchema(columnName);
                    columnSchema.ColumnType = columnType;
                    columnSchema.Description = description;
                    columnSchema.IsIdentity = autoIncreased;
                    columnSchema.IsNullable = allowNull;
                    columnSchema.IsPrimary = isPrimary;
                    columnSchema.MaxLength = maxlength;
                    columnSchema.Type = this.DbTypeMap[columnType];
                    tableSchema.ColumnList.Add(columnSchema);

                }
            }
            else
            {
                tableSchema = null;
            }

            return tableSchema;
        }

        public override string GetPagedSelectCmdText(string columnsSql, string fromSql, string whereSql, string orderbySql
            , int pageIndex, int pageSize)
        {
            pageIndex = pageIndex < 1 ? 1 : pageIndex;
            string result = string.Empty;

            result = string.Format(
@"select {0} 
from {1} 
where {2} 
{3}
limit {4},{5}",
columnsSql, fromSql, whereSql,
orderbySql, (pageIndex - 1) * pageSize, pageSize);

            return result;
        }

        public override string ToCreateTableScript(TableSchema tableSchema)
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.AppendFormat("create table [{0}](", tableSchema.TableName);

            foreach (ColumnSchema columnSchema in tableSchema.ColumnList)
            {
                if (string.IsNullOrEmpty(columnSchema.ColumnType))
                {
                    foreach (string item in DbTypeMap.Keys)
                    {
                        if (DbTypeMap[item] == columnSchema.Type)
                        {
                            columnSchema.ColumnType = item;
                            break;
                        }
                    }
                }

                string columnType = string.Compare(columnSchema.ColumnType, "nvarchar", true) == 0
                    ?string.Format("{0}({1})", columnSchema.ColumnType, columnSchema.MaxLength)
                    : columnSchema.ColumnType ;
                if (columnSchema.IsIdentity)
                {
                    columnType = "INTEGER";
                }

                stringBuilder.AppendFormat("[{0}] {1} {2} {3},\r\n", columnSchema.ColumnName, columnType,
                    columnSchema.IsIdentity ? "PRIMARY KEY AUTOINCREMENT" : "", columnSchema.IsNullable ? "NULL" : "NOT NULL");
            }

            stringBuilder.Remove(stringBuilder.Length - 3, 3);

            stringBuilder.AppendFormat(")");

            return stringBuilder.ToString();
        }
    }

    internal class PgsqlDriver : DbDriver
    {
        public PgsqlDriver(DbProviderFactory dbProviderFactory, string connectionString, string connectionKey)
            : base(dbProviderFactory, connectionString, connectionKey)
        {



            dbTypeMap.Add("int4", typeof(int));
            dbTypeMap.Add("int8", typeof(long));

            dbTypeMap.Add("varchar", typeof(string));

            dbTypeMap.Add("money", typeof(decimal));

            dbTypeMap.Add("timestamp", typeof(DateTime));
            dbTypeMap.Add("timestamptz", typeof(DateTime));

        }

        public override string DbNowString
        {
            get { return "now()"; }
        }

        public override string FormatField(string field)
        {
            return @"""{0}""".FormatWith(field);
        }

        public override string IdentitySelectString
        {
            get { return @" currval('""{0}_Id_seq""')"; }
        }

        public override List<DbObject> GetDbObjectList()
        {
            List<DbObject> dbObjectList = new List<DbObject>();
            DataTable dataTable =
                ExecuteCommand(@"SELECT   tablename as name, 0 as type	   FROM   pg_tables  
WHERE  schemaname = 'public'
union all 
SELECT   viewname as name, 1 as type   FROM   pg_views  
WHERE  schemaname = 'public'", null);

            if (dataTable != null)
            {
                foreach (DataRow row in dataTable.Rows)
                {
                    string dbObjectName = row["Name"].ToString();

                    string type = row["type"].ToString();

                    if(type == "1")
                    {
                        dbObjectList.Add(new DbObject(dbObjectName, DbObjectType.View));
                    }
                    else
                    {
                        dbObjectList.Add(new DbObject(dbObjectName, DbObjectType.Table));
                    }
                }
            }


            return dbObjectList;
        }

        public override TableSchema GetTableSchema(string tableName)
        {
            TableSchema tableSchema = new TableSchema(tableName);

            DataTable dataTable = ExecuteCommand(@"select pg_constraint.conname as pk_name,pg_attribute.attname as colname from 
pg_constraint  inner join pg_class 
on pg_constraint.conrelid = pg_class.oid 
inner join pg_attribute on pg_attribute.attrelid = pg_class.oid 
and  pg_attribute.attnum = pg_constraint.conkey[1]
where pg_class.relname = '{0}' 
and pg_constraint.contype='p'
".FormatWith(tableName), null);

            string primaryKey = "id";
            if (dataTable != null && dataTable.Rows.Count == 1)
            {
                primaryKey = dataTable.Rows[0]["colname"].ToString();
            }

            dataTable = ExecuteCommand(string.Format(@"SELECT c.relname, a.attname as Name,
                a.attnum,
                a.attname AS field,
                t.typname AS type,
                a.attlen AS length,
                a.atttypmod AS lengthvar,
                a.attnotnull AS notnull
        FROM
                pg_class c,
                pg_attribute a,
                pg_type t
        WHERE 1=1
                and c.relname = '{0}'
                and a.attnum > 0
                and a.attrelid = c.oid
                and a.atttypid = t.oid
        ORDER BY a.attnum", tableName), null);

            if (dataTable != null && dataTable.Rows.Count > 0)
            {
                foreach (DataRow row in dataTable.Rows)
                {
                    string columnName = row["Name"].ToString();
                    int maxlength = 0;
                    string columnType = row["type"].ToString();

                    maxlength = int.Parse(row["lengthvar"].ToString());


                    bool allowNull = row["notnull"].ToString() == "f";
                    bool autoIncreased = string.Compare(columnName, primaryKey, true) == 0;

                    string description = string.Empty;
                    bool isPrimary = autoIncreased;
                    ColumnSchema columnSchema = new ColumnSchema(columnName);
                    columnSchema.ColumnType = columnType;
                    columnSchema.Description = description;
                    columnSchema.IsIdentity = autoIncreased;
                    columnSchema.IsNullable = allowNull;
                    columnSchema.IsPrimary = isPrimary;
                    columnSchema.MaxLength = maxlength;
                    columnSchema.Type = this.DbTypeMap[columnType];
                    tableSchema.ColumnList.Add(columnSchema);

                }
            }
            else
            {
                tableSchema = null;
            }

            return tableSchema;
        }

        public override string GetPagedSelectCmdText(string columnsSql, string fromSql, string whereSql, string orderbySql
            , int pageIndex, int pageSize)
        {
            pageIndex = pageIndex < 1 ? 1 : pageIndex;
            string result = string.Empty;

            result = string.Format(
@"select {0} 
from {1} 
where {2} 
{3}
limit {5} offset {4}",
columnsSql, fromSql, whereSql,
orderbySql, (pageIndex - 1) * pageSize, pageSize);

            return result;
        }

        public override string ToCreateTableScript(TableSchema tableSchema)
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.AppendFormat("create table [{0}](", tableSchema.TableName);

            foreach (ColumnSchema columnSchema in tableSchema.ColumnList)
            {
                if (string.IsNullOrEmpty(columnSchema.ColumnType))
                {
                    foreach (string item in DbTypeMap.Keys)
                    {
                        if (DbTypeMap[item] == columnSchema.Type)
                        {
                            columnSchema.ColumnType = item;
                            break;
                        }
                    }
                }

                string columnType = columnSchema.MaxLength == 0 ? columnSchema.ColumnType
                    : string.Format("{0}({1})", columnSchema.ColumnType, columnSchema.MaxLength);
                if (columnSchema.IsIdentity)
                {
                    columnType = "INTEGER";
                }

                stringBuilder.AppendFormat("[{0}] {1} {2} {3},\r\n", columnSchema.ColumnName, columnType,
                    columnSchema.IsIdentity ? "PRIMARY KEY AUTOINCREMENT" : "", columnSchema.IsNullable ? "NULL" : "NOT NULL");
            }

            stringBuilder.Remove(stringBuilder.Length - 3, 3);

            stringBuilder.AppendFormat(")");

            return stringBuilder.ToString();
        }
    }
}
