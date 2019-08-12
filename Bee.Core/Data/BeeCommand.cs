using System;
using System.Collections.Generic;
using System.Text;
using System.Data.Common;
using System.Data;
using System.Diagnostics;
using System.Threading;
using Bee.Logging;
using Bee.Core;
using Bee.Util;
using Bee.Caching;
using System.Data.SqlClient;

namespace Bee.Data
{
    internal enum BeeDbCommandBehavior
    {
        NoQuery,
        Query
    }

    /// <summary>
    /// Base BeeCommand.
    /// The default command behavior is query.
    /// </summary>
    internal abstract class BaseBeeCommand : IDisposable
    {
        #region Fields

        protected DbCommand dbCommand;
        protected BeeDbCommandBehavior commandBehavior;
        protected DbSession owner;
        protected DbDataReader dbDataReader;

        #endregion

        #region Constructors

        public BaseBeeCommand(DbSession owner)
            : this(owner, BeeDbCommandBehavior.Query)
        {
        }

        public BaseBeeCommand(DbSession owner, BeeDbCommandBehavior commandBehavior)
        {
            this.owner = owner;
            this.commandBehavior = commandBehavior;
        }

        #endregion

        #region Public Methods

        public virtual T Excute<T>()
        {
            T result = default(T);
            try
            {
                dbCommand = owner.CreateCommand();

                BuildCommand();

                Type type = typeof(T);
                if (commandBehavior == BeeDbCommandBehavior.NoQuery)
                {
                    object obj = dbCommand.ExecuteScalar();

                    if (obj != null)
                    {
                        if (type == typeof(int) || type == typeof(long))
                        {
                            result = (T)ConvertUtil.Convert(obj, type);
                        }
                        else
                        {
                            throw new DataException("Not support the return type :" + type.Name);
                        }
                    }
                }
                else
                {
                    if (type == typeof(DataTable))
                    {
                        result = (T)ExecuteDataTable();
                    }
                    else if (type == typeof(DataSet))
                    {
                        result = (T)ExecuteDataSet();
                    }
                    else
                    {
                        throw new DataException("Not support the return type :" + type.Name);
                    }
                }

                object retVal = FinishCommand();
                if (retVal != null)
                {
                    result = (T)ConvertUtil.Convert(retVal, type);
                }

                //Logger.Log(LogLevel.Core, this.ToSqlString());
            }
            catch (DbException dbException)
            {
                //if (dbCommand != null)
                //{
                //    Logger.Debug(string.Format("DbCmdText:{0}\r\nconnection:{1}", dbCommand.CommandText, owner.DbDriver), dbException);
                //}
                throw new DataException(string.Format("{0}\r\n{1}", dbException.Message, this.ToString()), dbException);
            }
            catch (Exception ex)
            {
                //if (dbCommand != null)
                //{
                //    Logger.Debug(string.Format("DbCmdText:{0}", dbCommand.CommandText), ex);
                //}
                throw new DataException(string.Format("{0}\r\n{1}", ex.Message, this.ToString()), ex);
            }
            finally
            {
                if (dbCommand != null)
                {
                    dbCommand.Parameters.Clear();
                    dbCommand.Dispose();
                }

                // 若是默认session， 则关闭数据库连接
                if (owner.IsDefaultSession)
                {
                    owner.CloseConnection();
                }
            }

            return result;
        }

        public IEnumerable<T> DataRead<T>()
            where T : class
        {
            dbCommand = owner.CreateCommand();

            BuildCommand();

            if (dbDataReader == null)
            {
                this.dbDataReader = dbCommand.ExecuteReader();
            }

            while (dbDataReader.Read())
            {
                yield return ExecuteDataReader<T>(dbDataReader);
            }

        }

        #endregion

        #region Proptected Methods

        protected abstract void BuildCommand();

        protected virtual object FinishCommand()
        {
            return null;
        }

        protected object GetDefaultValue(Type type)
        {
            if (type == typeof(string))
            {
                return "";
            }
            else
            {
                return Activator.CreateInstance(type);
            }
        }

        protected virtual string ToSqlString()
        {
            return string.Empty;
        }

        #endregion

        #region Private Methods

        private object ExecuteDataTable()
        {
            DataTable dataTable = new DataTable();
            using (DbDataAdapter adapter = owner.DbDriver.CreateDataAdapter())
            {
                adapter.SelectCommand = dbCommand;

                adapter.Fill(dataTable);

            }
            return dataTable;
        }

        private object ExecuteDataSet()
        {
            DataSet dataSet = new DataSet();
            using (DbDataAdapter adapter = owner.DbDriver.CreateDataAdapter())
            {
                adapter.SelectCommand = dbCommand;
                adapter.Fill(dataSet);

            }

            return dataSet;
        }

        private T ExecuteDataReader<T>(DbDataReader dataReader)
            where T : class
        {
            var len = dataReader.FieldCount;

            EntityProxy<T> entityProxy = EntityProxyManager.Instance.GetEntityProxy<T>();

            T result = entityProxy.CreateInstance() as T;
            for (var i = 0; i < len; i++)
            {
                string name = dataReader.GetName(i);
                var rawValue = dataReader.GetValue(i);

                try
                {
                    entityProxy.SetPropertyValue(result, name, rawValue);
                }
                catch(Exception)
                {
                    ThrowExceptionUtil.ThrowMessageException("setpropertyerror.name:{0} value:{1}".FormatWith(name, rawValue));
                }
            }

            return result;
        }

        #endregion

        #region IDisposable 成员

        public void Dispose()
        {
            if (dbCommand != null)
            {
                dbCommand.Dispose();
            }
        }

        #endregion
    }

    internal sealed class InsertBeeCommand : BaseBeeCommand
    {
        // Fields
        private BeeDataAdapter dataAdapter;
        private string tableName;
        private string identityColumnName;

        // Methods
        public InsertBeeCommand(DbSession owner,
            string tableName, BeeDataAdapter dataAdapter, string identityColumnName)
            : base(owner, BeeDbCommandBehavior.NoQuery)
        {
            this.tableName = tableName;
            this.dataAdapter = dataAdapter;
            this.identityColumnName = identityColumnName;
        }

        protected override void BuildCommand()
        {
            base.dbCommand.CommandType = CommandType.Text;

            BeeDataAdapter columnDataAdapter = new BeeDataAdapter();
            columnDataAdapter.Merge(this.dataAdapter, true);

            // 移除不存在的列名
            TableSchema tableSchema = owner.GetTableSchema(tableName);
            ThrowExceptionUtil.ArgumentConditionTrue(tableSchema != null, "tableName", "can not find table. Name:{0}".FormatWith(tableName));

            List<string> needToRemovedList = new List<string>();
            foreach (string key in columnDataAdapter.Keys)
            {
                ColumnSchema columnSchema = tableSchema.GetColumn(key);

                if (columnSchema == null)
                {
                    needToRemovedList.Add(key);
                }
                else if (columnSchema.IsComputeField)
                {
                    needToRemovedList.Add(key);
                }
                else
                {
                    // do nothing here.
                }
            }

            foreach (string key in needToRemovedList)
            {
                columnDataAdapter.RemoveKey(key);
            }

            DataAdapterParser dataAdapterParser = new DataAdapterParser(owner, columnDataAdapter);
            base.dbCommand.CommandText = string.Format(@" 
insert into {0} 
({1})
values
({2});"
                , owner.DbDriver.FormatField(this.tableName)
                , dataAdapterParser.ColumnClause, dataAdapterParser.ParameterClause);

            if (owner.DbServerType == DBServerType.Oracle)
            {
                string sqlText = string.Format(@"begin
    {0}
                    end;", base.dbCommand.CommandText);

                sqlText = sqlText.Replace(Environment.NewLine, " ");
                base.dbCommand.CommandText = sqlText;
            }
            else if (owner.DbServerType == DBServerType.Pgsql)
            {
                if (!string.IsNullOrEmpty(identityColumnName))
                {
                    string identitySelectString = string.Format(@"; SELECT {0} ", owner.DbDriver.IdentitySelectString);
                    identitySelectString = string.Format(identitySelectString, tableName).ToLower();

                    base.dbCommand.CommandText = base.dbCommand.CommandText + identitySelectString;
                }
            }
            else
            {
                if (!string.IsNullOrEmpty(identityColumnName))
                {
                    string identitySelectString = string.Format(@" SELECT {0}", owner.DbDriver.IdentitySelectString);

                    base.dbCommand.CommandText = base.dbCommand.CommandText + identitySelectString;
                }
            }

            base.dbCommand.Parameters.AddRange(dataAdapterParser.DbParameterList.ToArray());
        }

        protected override object FinishCommand()
        {
            object result = null;
            foreach (DbParameter paramter in base.dbCommand.Parameters)
            {
                if ((paramter.Direction == ParameterDirection.Output) || (paramter.Direction == ParameterDirection.InputOutput))
                {
                    string paraName = paramter.ParameterName;
                    this.dataAdapter[paraName] = paramter.Value;
                }
            }

            if (owner.DbDriver.GetType() == typeof(OracleDriver))
            {
                string identitySelectString = string.Format(@" SELECT {0} ", owner.DbDriver.IdentitySelectString);
                identitySelectString = string.Format(identitySelectString, tableName);

                DataTable data = owner.ExecuteCommand(identitySelectString, null);
                if (data != null && data.Rows.Count == 1)
                {
                    result = data.Rows[0][0];
                }
            }

            return result;
        }

        public override string ToString()
        {
            return string.Format("Insert {0}, Data {1}", tableName, dataAdapter);
        }

        protected override string ToSqlString()
        {
            string result = dbCommand.CommandText;

            foreach (DbParameter item in dbCommand.Parameters)
            {
                result = result.Replace(owner.DbDriver.ParameterPrefix + item.ParameterName, item.Value == null ? "" : item.Value.ToString());
            }

            return result;
        }
    }

    internal class UpdateBeeCommand : BaseBeeCommand
    {
        // Fields
        private BeeDataAdapter dataAdapter;
        private SqlCriteria sqlCriteria;
        private string tableName;

        // Methods
        public UpdateBeeCommand(DbSession owner, string tableName, BeeDataAdapter data, SqlCriteria sqlCriteria)
            : base(owner, BeeDbCommandBehavior.NoQuery)
        {
            ThrowExceptionUtil.ArgumentNotNullOrEmpty(tableName, "tableName");
            ThrowExceptionUtil.ArgumentConditionTrue(data != null && data.Count != 0, "data", "data should not be null or empty.");

            this.tableName = tableName;
            this.dataAdapter = data;
            this.sqlCriteria = sqlCriteria;
        }

        protected override void BuildCommand()
        {
            BeeDataAdapter columnDataAdapter = new BeeDataAdapter();
            columnDataAdapter.Merge(this.dataAdapter, true);

            // 移除不存在的列名 及 计算列
            TableSchema tableSchema = owner.GetTableSchema(tableName);

            ThrowExceptionUtil.ArgumentConditionTrue(tableSchema != null, "tableName", "can not find table. Name:{0}".FormatWith(tableName));

            List<string> needToRemovedList = new List<string>();
            foreach (string key in columnDataAdapter.Keys)
            {
                ColumnSchema columnSchema = tableSchema.GetColumn(key);

                if (columnSchema == null)
                {
                    needToRemovedList.Add(key);
                }
                else if (columnSchema.IsComputeField)
                {
                    needToRemovedList.Add(key);
                }
                else
                {
                    // do nothing here.
                }
            }

            if (tableSchema.IdentityColumn != null)
            {
                needToRemovedList.Add(tableSchema.IdentityColumn.ColumnName);
            }

            foreach (string key in needToRemovedList)
            {
                columnDataAdapter.RemoveKey(key);
            }

            DataAdapterParser dataAdapterParser = new DataAdapterParser(owner, columnDataAdapter);

            base.dbCommand.Parameters.AddRange(dataAdapterParser.DbParameterList.ToArray());

            string whereClause = string.Empty;
            if (sqlCriteria != null)
            {
                this.sqlCriteria.Owner = owner;
                whereClause = string.Format("where {0}", sqlCriteria.WhereClause);

                base.dbCommand.Parameters.AddRange(sqlCriteria.DbParameters.ToArray());
            }

            if (owner.DbDriver.GetType() == typeof(OracleDriver))
            {
                string sqlText = string.Format("update {0} set {1} {2}",
                    owner.DbDriver.FormatField(this.tableName), dataAdapterParser.UpdateClause, whereClause);

                sqlText = sqlText.Replace(Environment.NewLine, " ");
                base.dbCommand.CommandText = sqlText;
            }
            else
            {

                base.dbCommand.CommandText = string.Format("update {0} \r\nset {1} \r\n{2}",
                    owner.DbDriver.FormatField(this.tableName), dataAdapterParser.UpdateClause, whereClause);
            }
        }

        public override string ToString()
        {
            return string.Format("Update {0}, Data {1}, criteria:{2}", tableName, dataAdapter, sqlCriteria);
        }

        protected override string ToSqlString()
        {
            string result = dbCommand.CommandText;

            foreach (DbParameter item in dbCommand.Parameters)
            {
                result = result.Replace(owner.DbDriver.ParameterPrefix + item.ParameterName, item.Value == null ? "" : item.Value.ToString());
            }

            return result;
        }
    }

    internal class DeleteBeeCommand : BaseBeeCommand
    {
        // Fields
        private SqlCriteria sqlCriteria;
        private string tableName;

        // Methods
        public DeleteBeeCommand(DbSession owner, string tableName, SqlCriteria sqlCriteria)
            : base(owner, BeeDbCommandBehavior.NoQuery)
        {
            this.tableName = tableName;
            this.sqlCriteria = sqlCriteria;
        }

        protected override void BuildCommand()
        {
            string whereClause = string.Empty;
            if (sqlCriteria != null)
            {
                this.sqlCriteria.Owner = owner;
                whereClause = string.Format("where {0}", sqlCriteria.WhereClause);
                base.dbCommand.Parameters.AddRange(sqlCriteria.DbParameters.ToArray());
            }

            base.dbCommand.CommandText = string.Format("delete from {0} \r\n{1}",
                owner.DbDriver.FormatField(tableName), whereClause);
        }

        public override string ToString()
        {
            return string.Format("Delete {0}, criteria:{1}", tableName, sqlCriteria);
        }

        protected override string ToSqlString()
        {
            string result = dbCommand.CommandText;

            foreach (DbParameter item in dbCommand.Parameters)
            {
                result = result.Replace(owner.DbDriver.ParameterPrefix + item.ParameterName, item.Value == null ? "" : item.Value.ToString());
            }

            return result;
        }
    }

    internal sealed class SPBeeCommand : BaseBeeCommand
    {
        // Fields
        private BeeDataAdapter dataAdapter;
        private string spName;

        // Methods
        public SPBeeCommand(DbSession owner, string spName)
            : this(owner, spName, null)
        {
        }

        public SPBeeCommand(DbSession owner, string spName, BeeDataAdapter dataAdapter)
            : base(owner, BeeDbCommandBehavior.Query)
        {
            this.spName = spName;
            this.dataAdapter = dataAdapter;
        }


        protected override void BuildCommand()
        {
            base.dbCommand.CommandText = this.spName;
            base.dbCommand.CommandType = CommandType.StoredProcedure;

            SPSchema schema = CacheManager.Instance.GetEntity<SPSchema, string>(owner.ConnectionName,
                spName, TimeSpan.MaxValue,
                (name) =>
                {
                    return owner.DbDriver.GetSpSchema(name);
                });

            ThrowExceptionUtil.ArgumentConditionTrue(schema != null, "tableName", "Can not find sp. Name:{0}".FormatWith(spName));

            DataAdapterParser dataAdapterParser = new DataAdapterParser(owner, dataAdapter);

            if (dataAdapter != null)
            {
                foreach (SPParameter spParameter in schema.ParameterList)
                {
                    DbParameter parameter = owner.DbDriver.CreateParameter();
                    parameter.ParameterName = spParameter.Name;
                    parameter.Direction = spParameter.Direction;
                    //parameter.DbType = spParameter.DbType;
                    if (spParameter.DbType == "REF CURSOR")
                    {
                        IEntityProxy parameterProxy = EntityProxyManager.Instance.GetEntityProxyFromType(parameter.GetType());
                        if (parameter.GetType().Namespace == "System.Data.OracleClient")
                        {
                            parameterProxy.SetPropertyValue(parameter, "OracleType", 5);
                            //parameter.DbType = (DbType)5;
                        }
                        else
                        {
                            parameterProxy.SetPropertyValue(parameter, "OracleDbType", 0x79);
                        }
                    }

                    if (dataAdapter[spParameter.Name] != null)
                    {
                        parameter.Value = dataAdapter[spParameter.Name];
                    }
                    else
                    {
                        if (owner.DbDriver.DbTypeMap.ContainsKey(spParameter.DbType))
                        {
                            Type type = owner.DbDriver.DbTypeMap[spParameter.DbType];
                            parameter.Value = GetDefaultValue(type);

                            if (spParameter.Direction == ParameterDirection.InputOutput && spParameter.MaxLength > 0)
                            {
                                parameter.Size = spParameter.MaxLength * 2;
                            }
                        }
                    }

                    base.dbCommand.Parameters.Add(parameter);
                }
            }
        }

        protected override object FinishCommand()
        {
            foreach (DbParameter paramter in base.dbCommand.Parameters)
            {
                if ((paramter.Direction == ParameterDirection.Output) || (paramter.Direction == ParameterDirection.InputOutput))
                {
                    string paraName = paramter.ParameterName;
                    this.dataAdapter[paraName] = paramter.Value;
                }
            }
            return base.FinishCommand();
        }

        public override string ToString()
        {
            return string.Format("Sp {0}, Data {1}", spName, dataAdapter);
        }

        protected override string ToSqlString()
        {
            string result = dbCommand.CommandText;

            foreach (DbParameter item in dbCommand.Parameters)
            {
                result += "{0}{1}={2}".FormatWith(owner.DbDriver.ParameterPrefix, item.ParameterName, item.Value);
            }

            return result;
        }
    }

    internal sealed class QueryBeeCommand : BaseBeeCommand
    {
        private string tableName;
        private string selectClause;
        private string orderbyClause;
        private SqlCriteria sqlCriteria;
        private int pageIndex;
        private int pageSize;

        public QueryBeeCommand(DbSession owner, string tableName, string selectClause, SqlCriteria sqlCriteria, string orderbyClause)
            : this(owner, tableName, selectClause, sqlCriteria, orderbyClause, -1, -1)
        {

        }

        /// <summary>
        /// 构造查询类
        /// </summary>
        /// <param name="tableName">表名，不能为空</param>
        /// <param name="selectClause"></param>
        /// <param name="sqlCriteria"></param>
        /// <param name="orderbyClause"></param>
        /// <param name="pageIndex"></param>
        /// <param name="pageSize"></param>
        public QueryBeeCommand(DbSession owner, string tableName, string selectClause,
            SqlCriteria sqlCriteria, string orderbyClause, int pageIndex, int pageSize)
            : base(owner)
        {
            this.tableName = tableName;
            this.selectClause = selectClause;
            this.sqlCriteria = sqlCriteria;
            this.orderbyClause = orderbyClause == null ? null : orderbyClause.Trim();
            this.pageIndex = pageIndex;
            this.pageSize = pageSize;

            if (string.IsNullOrEmpty(selectClause))
            {
                this.selectClause = "*";
            }
        }

        protected override void BuildCommand()
        {
            string whereClause = string.Empty;
            string orderbyClauseValue = string.Empty;
            if (sqlCriteria != null)
            {
                this.sqlCriteria.Owner = owner;
                whereClause = string.Format("{0}", sqlCriteria.WhereClause);

                base.dbCommand.Parameters.AddRange(sqlCriteria.DbParameters.ToArray());


                TableSchema tableSchema = owner.GetTableSchema(tableName);
            }
            else
            {
                whereClause = "1=1";
            }

            if (!string.IsNullOrEmpty(orderbyClause))
            {
                orderbyClauseValue = string.Format("order by {0}", orderbyClause);
            }

            // 分页的三个条件， 起始页， 页大小， 排序规则
            if (pageSize > 0 && pageIndex >= 0)
            {
                if (string.IsNullOrEmpty(orderbyClause))
                {
                    orderbyClauseValue = string.Format("order by {0}", Constants.DefaultIdentityColumnName);
                }

                if (tableName.IndexOf(" join ") < 0)
                {
                    tableName = owner.DbDriver.FormatField(tableName);
                }

                base.dbCommand.CommandText = owner.DbDriver.GetPagedSelectCmdText(selectClause, tableName,
                    whereClause, orderbyClauseValue, pageIndex, pageSize);
            }
            else
            {

                base.dbCommand.CommandText = string.Format(
@"select {0} 
from {1} 
where {2}
{3}", selectClause, owner.DbDriver.FormatField(tableName), whereClause, orderbyClauseValue);
            }
        }

        public override string ToString()
        {
            return string.Format("Query {0}, columns {1}, criteria:{2}, orderby:{3}, pageInfo:{4}({5})", tableName
                , selectClause, sqlCriteria, orderbyClause, pageIndex, pageSize);
        }

        protected override string ToSqlString()
        {
            string result = dbCommand.CommandText;
            if (dbCommand.Parameters != null)
            {
                foreach (DbParameter item in dbCommand.Parameters)
                {
                    result = result.Replace(owner.DbDriver.ParameterPrefix + item.ParameterName, item.Value == null ? "" : item.Value.ToString());
                }
            }

            return result;
        }

    }

    internal sealed class CmdTextBeeCommand : BaseBeeCommand
    {
        // Fields
        private string cmdText;
        private BeeDataAdapter dataAdapter;

        // Methods
        public CmdTextBeeCommand(DbSession owner, string cmdText, BeeDataAdapter dataAdapter)
            : base(owner)
        {
            this.cmdText = cmdText;
            this.dataAdapter = dataAdapter;
        }


        protected override void BuildCommand()
        {
            base.dbCommand.CommandText = this.cmdText;
            if (this.dataAdapter != null)
            {
                DataAdapterParser dataAdapterParser = new DataAdapterParser(owner, this.dataAdapter);

                base.dbCommand.Parameters.AddRange(dataAdapterParser.DbParameterList.ToArray());
            }
        }

        public override string ToString()
        {
            return string.Format("CmdText {0}, Data {1}", cmdText, dataAdapter);
        }

        protected override string ToSqlString()
        {
            string result = cmdText;
            if (dataAdapter != null)
            {
                foreach (string item in dataAdapter.Keys)
                {
                    result = result.Replace(owner.DbDriver.ParameterPrefix + item, dataAdapter[item].ToString());
                }
            }

            return result;
        }
    }

    internal sealed class CmdTextWithWhereBeeCommand : BaseBeeCommand
    {
        // Fields
        private string cmdText;
        private SqlCriteria sqlCriteria;
        public CmdTextWithWhereBeeCommand(DbSession owner, string cmdText, SqlCriteria sqlCriteria)
            : base(owner)
        {
            this.cmdText = cmdText;
            this.sqlCriteria = sqlCriteria;
        }
        protected override void BuildCommand()
        {
            base.dbCommand.CommandText = this.cmdText;

            if (this.sqlCriteria != null)
            {
                base.dbCommand.CommandText = this.cmdText.Replace("@where", sqlCriteria.WhereClause);

                base.dbCommand.Parameters.AddRange(sqlCriteria.DbParameters.ToArray());
            }
        }

        public override string ToString()
        {
            return string.Format("CmdText {0}, SqlCriteria {1}", cmdText, sqlCriteria);
        }

        protected override string ToSqlString()
        {
            string result = cmdText.Replace("@where", sqlCriteria.WhereClause);

            return result;
        }
    }

    internal sealed class SqlServerBulkCopyCommand : BaseBeeCommand
    {
        private string tableName;
        private DataTable data;
        private SqlRowsCopiedEventHandler notifyHandler;
        private int notifyAfter;
        private Dictionary<string, string> mapping;

        public SqlServerBulkCopyCommand(DbSession owner,
           string tableName, DataTable data)
            : this(owner, tableName, data, null, null, data.Rows.Count)
        {
        }

        public SqlServerBulkCopyCommand(DbSession owner,
            string tableName, DataTable data, Dictionary<string, string> mapping, SqlRowsCopiedEventHandler notifyHandler, int notifyAfter)
            : base(owner, BeeDbCommandBehavior.NoQuery)
        {
            this.tableName = tableName;
            this.data = data;
            this.mapping = mapping;

            this.notifyHandler = notifyHandler;
            this.notifyAfter = notifyAfter;
        }

        protected override void BuildCommand()
        {
            // no need to implement;
        }

        public override T Excute<T>()
        {
            ThrowExceptionUtil.ArgumentConditionTrue(owner.DbDriver is SqlServer2000Driver, string.Empty, "only sqlserver can use bulkcopy.");

            SqlConnection connection = owner.OpenConnection() as SqlConnection;
            SqlBulkCopy sqlBulkCopy = new SqlBulkCopy(connection);
            sqlBulkCopy.BulkCopyTimeout = 50000;
            sqlBulkCopy.BatchSize = 1000;
            if (notifyHandler != null)
            {
                sqlBulkCopy.SqlRowsCopied += notifyHandler;
                sqlBulkCopy.NotifyAfter = notifyAfter;
            }
            if (mapping != null)
            {
                foreach (string key in mapping.Keys)
                {
                    sqlBulkCopy.ColumnMappings.Add(key, mapping[key]);
                }
            }

            sqlBulkCopy.DestinationTableName = tableName;
            sqlBulkCopy.WriteToServer(data);

            return default(T);
        }
    }
}
