using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Bee.Core;
using Bee.Logging;
using Bee.Util;
using System.Data.Common;
using System.Threading;
using System.Data;
using Bee.Caching;
using System.Data.SqlClient;

namespace Bee.Data
{
    /// <summary>
    ///  默认采用第一个连接字符串配置。
    ///  若没有， 则采用第一个注册的连接配置。
    ///  管理Data Provider， Connection， Transaction
    /// </summary>
    public sealed class DbSession : IDisposable
    {
        #region Fields

        // Fields of the Scope
        private bool _disposed;
        private DbSession _parent;
        private static DbSession defaultDbSession;
        //private static DbSession 

        [ThreadStatic]
        private static DbSession _head;

        // 
        private string connectionName;
        private DbDriver dbDriver;
        private bool useTransaction = false;
        private bool isCommited = false;
        private DbConnection dbConnection;
        private DbTransaction dbTransaction;
        private IsolationLevel isolationLevel = IsolationLevel.Unspecified;
        private int commandTimeout = 30;

        #endregion

        #region Constructors

        private DbSession(DbDriver dbDriver)
        {
            this.connectionName = dbDriver.ConnectionName;
            this.dbDriver = dbDriver;
        }

        /// <summary>
        /// 使用连接字符串名构造, 不适用事务。
        /// </summary>
        /// <param name="connectionName">连接字符串名</param>
        public DbSession(string connectionName)
            : this(connectionName, false)
        {

        }

        /// <summary>
        /// 使用连接字符串名构造， 支持是否使用事务。
        /// 注意：使用事务的情况下， 请务必调用CommitTransaction方法。
        /// </summary>
        /// <param name="connectionName">连接字符串名</param>
        /// <param name="useTransaction">是否使用事务</param>
        public DbSession(string connectionName, bool useTransaction)
            : this(connectionName, useTransaction, IsolationLevel.Unspecified)
        {
        }

        /// <summary>
        /// 使用连接字符串名构造， 支持是否使用事务。
        /// 注意：使用事务的情况下， 请务必调用CommitTransaction方法。
        /// </summary>
        /// <param name="connectionName">连接字符串名</param>
        /// <param name="useTransaction">是否使用事务</param>
        public DbSession(string connectionName, bool useTransaction, IsolationLevel isolationLevel)
        {
            this.connectionName = connectionName;
            dbDriver = DbDriverFactory.Instance.GetInstance(connectionName);

            Thread.BeginThreadAffinity();

            _parent = _head;
            _head = this;

            this.useTransaction = useTransaction;
            this.isolationLevel = isolationLevel;
        }

        static DbSession()
        {
            InitDefaultDbSession();
        }

        #endregion

        #region Properties

        /// <summary>
        /// 连接字符串名
        /// </summary>
        public string ConnectionName
        {
            get
            {
                return this.connectionName;
            }
        }

        internal DbDriver DbDriver
        {
            get
            {
                return this.dbDriver;
            }
        }

        public int CommandTimeout
        {
            get
            {
                return this.commandTimeout;
            }
            set
            {
                this.commandTimeout = value;
            }
        }

        /// <summary>
        /// 使用该实例，请注意：
        /// 无DbSession 上下文的情况下， 该实例线程不安全。
        /// </summary>
        public static DbSession Current
        {
            get
            {
                DbSession current = _head != null ? _head : null;

                if (current == null)
                {
                    InitDefaultDbSession();
                    current = defaultDbSession;
                }

                if (current == null)
                {
                    throw new DataException("No valid connection");
                }

                return current;

            }
        }

        internal bool IsDefaultSession
        {
            get
            {
                return object.ReferenceEquals(Current, defaultDbSession);
            }
        }

        private DbConnection DbConnection
        {
            get
            {
                if ((dbConnection == null) || (dbConnection.State != ConnectionState.Open))
                {
                    dbConnection = OpenConnection();
                }
                return dbConnection;
            }
        }

        internal DBServerType DbServerType
        {
            get
            {
                if (dbDriver.GetType() == typeof(OracleDriver))
                {
                    return DBServerType.Oracle;
                }
                else if (dbDriver.GetType() == typeof(SqlServer2000Driver)
                    || dbDriver.GetType() == typeof(SqlServer2005Driver)
                    || dbDriver.GetType() == typeof(SqlServer2008Driver))
                {
                    return DBServerType.Sqlserver;
                }
                else if (dbDriver.GetType() == typeof(MySqlDriver))
                {
                    return DBServerType.MySql;
                }
                else if (dbDriver.GetType() == typeof(SqliteDriver))
                {
                    return DBServerType.Sqlite;
                }
                else if (dbDriver.GetType() == typeof(PgsqlDriver))
                {
                    return DBServerType.Pgsql;
                }
                else
                {
                    return DBServerType.Ole;
                }
            }
        }

        #endregion

        #region Internal Methods

        internal DbCommand CreateCommand()
        {
            DbCommand dbCommand = null;
            if (useTransaction && dbTransaction == null)
            {
                this.dbTransaction = DbConnection.BeginTransaction(isolationLevel);
            }

            dbCommand = DbConnection.CreateCommand();
            dbCommand.CommandTimeout = this.CommandTimeout;

            if (dbTransaction != null)
            {
                dbCommand.Transaction = this.dbTransaction;
            }
            return dbCommand;
        }

        internal DbConnection OpenConnection()
        {
            DbConnection connection = DbDriver.CreateConnection();
            int times = 2;
            while (times > 0)
            {
                times--;
                try
                {
                    if (connection.State != ConnectionState.Open)
                    {
                        connection.Open();
                        if (ConnectionState.Open == connection.State)
                        {
                            times = -1;
                        }
                    }
                    else
                    {
                        times = -1;
                    }
                }
                catch (Exception ex)
                {
                    if (times == 0)
                    {
                        throw new DataException("Connection Failed." + connectionName, ex);
                    }
                    Thread.Sleep(0x3e8);
                }
            }
            return connection;
        }

        internal void CloseConnection()
        {
            if (dbConnection != null && dbConnection.State != ConnectionState.Closed)
            {
                // 假如使用了事务， 若未递交， 则回滚
                if (dbTransaction != null && !isCommited)
                {
                    dbTransaction.Rollback();
                }

                this.dbConnection.Close();
                this.dbConnection.Dispose();
                this.dbConnection = null;
            }
        }

        #endregion

        #region Public Methods

        #region SQL Access


        /// <summary>
        /// 递交事务
        /// </summary>
        public void CommitTransaction()
        {
            if (this.dbTransaction != null && this.dbConnection != null
                && this.dbConnection.State != ConnectionState.Closed)
            {
                this.dbTransaction.Commit();
                this.isCommited = true;

                this.dbTransaction.Dispose();
                this.dbTransaction = null;
            }
        }

        /// <summary>
        /// 新增数据。
        /// 默认的自增列为Id，所有的表约定必须有名为Id的自增列。
        /// </summary>
        /// <param name="tableName">表名</param>
        /// <param name="dataAdapter">单条数据集</param>
        /// <returns>插入返回的自增列</returns>
        public int Insert(string tableName, BeeDataAdapter dataAdapter)
        {
            return Insert(tableName, dataAdapter, true);
        }

        /// <summary>
        /// 新增数据。
        /// 默认的自增列为Id，所有的表约定必须有名为Id的自增列。
        /// 
        /// </summary>
        /// <param name="tableName">表名</param>
        /// <param name="dataAdapter">单条数据集</param>
        /// <param name="removeIdentity">该值若为false， 则Insert语句中将可能包含自增列。</param>
        /// <returns>插入返回的自增列</returns>
        public int Insert(string tableName, BeeDataAdapter dataAdapter, bool removeIdentity)
        {
            return InsertT<int>(tableName, dataAdapter, removeIdentity);
        }

        public T InsertT<T>(string tableName, BeeDataAdapter dataAdapter, bool removeIdentity)
        {
            ThrowExceptionUtil.ArgumentNotNullOrEmpty(tableName, "tableName");
            ThrowExceptionUtil.ArgumentNotNull(dataAdapter, "dataAdapter");

            if (removeIdentity)
            {
                TableSchema tableSchema = GetTableSchema(tableName);
                ThrowExceptionUtil.ArgumentConditionTrue(tableSchema != null, "tableName", "can not find table. Name:{0}".FormatWith(tableName));
                if (tableSchema.IdentityColumn != null)
                {
                    dataAdapter.RemoveKey(tableSchema.IdentityColumn.ColumnName);
                }
                else
                {
                    dataAdapter.RemoveKey(Constants.DefaultIdentityColumnName);
                }
            }

            T result = default(T);
            InsertBeeCommand insertBeeCommand =
                new InsertBeeCommand(this, tableName, dataAdapter, Constants.DefaultIdentityColumnName);
            result = insertBeeCommand.Excute<T>();

            return result;
        }

        /// <summary>
        /// 更新数据
        /// </summary>
        /// <param name="tableName">表名</param>
        /// <param name="dataAdapter">数据集</param>
        /// <param name="sqlCriteria">条件集</param>
        public void Update(string tableName, BeeDataAdapter dataAdapter, SqlCriteria sqlCriteria)
        {
            ThrowExceptionUtil.ArgumentNotNullOrEmpty(tableName, "tableName");
            ThrowExceptionUtil.ArgumentNotNull(dataAdapter, "dataAdapter");

            UpdateBeeCommand updateBeeCommand =
                new UpdateBeeCommand(this, tableName, dataAdapter, sqlCriteria);

            updateBeeCommand.Excute<int>();
        }

        /// <summary>
        /// 删除数据
        /// </summary>
        /// <param name="tableName">表名</param>
        /// <param name="sqlCriteria">条件集</param>
        public void Delete(string tableName, SqlCriteria sqlCriteria)
        {
            ThrowExceptionUtil.ArgumentNotNullOrEmpty(tableName, "tableName");

            DeleteBeeCommand deleteBeeCommand =
                new DeleteBeeCommand(this, tableName, sqlCriteria);
            deleteBeeCommand.Excute<int>();
        }

        /// <summary>
        /// 调用存储过程。
        /// 一般不推荐返回值， 直接将该信息反映在结果集中更好。
        /// </summary>
        /// <param name="spName">存储过程名</param>
        /// <param name="dataAdapter">数据集</param>
        /// <returns>返回结果集</returns>
        public DataTable CallSP(string spName, BeeDataAdapter dataAdapter)
        {
            ThrowExceptionUtil.ArgumentNotNullOrEmpty(spName, "spName");

            SPBeeCommand spBeeCommand = new SPBeeCommand(this, spName, dataAdapter);
            return spBeeCommand.Excute<DataTable>();
        }

        /// <summary>
        /// 调用存储过程。
        /// 一般不推荐返回值， 直接将该信息反映在结果集中更好。
        /// </summary>
        /// <param name="spName">存储过程名</param>
        /// <param name="dataAdapter">数据集</param>
        /// <returns>返回结果集</returns>
        public List<T> CallSP<T>(string spName, BeeDataAdapter dataAdapter)
        {
            List<T> list = new List<T>();

            DataTable dataTable = CallSP(spName, dataAdapter);

            if (dataTable != null)
            {
                list = ConvertUtil.ConvertDataToObject<T>(dataTable);
            }

            return list;
        }

        /// <summary>
        /// 查询数据
        /// </summary>
        /// <param name="tableName">表名</param>
        /// <param name="sqlCriteria">条件集</param>
        /// <returns>结果集</returns>
        public DataTable Query(string tableName, SqlCriteria sqlCriteria)
        {
            int recordCount = 0;
            return Query(tableName, null, sqlCriteria, null, -1, -1, ref recordCount);
        }

        /// <summary>
        /// 查询数据
        /// </summary>
        /// <param name="tableName">表名</param>
        /// <param name="sqlCriteria">条件集</param>
        /// <param name="orderbyClause">排序集</param>
        /// <returns>结果集</returns>
        public DataTable Query(string tableName, SqlCriteria sqlCriteria, string orderbyClause)
        {
            int recordCount = 0;
            return Query(tableName, null, sqlCriteria, orderbyClause, -1, -1, ref recordCount);
        }

        /// <summary>
        /// 查询数据
        /// </summary>
        /// <param name="tableName">表名</param>
        /// <param name="sqlCriteria">条件集</param>
        /// <param name="orderbyClause">排序集</param>
        /// <param name="topNum">条数</param>
        /// <returns>结果集</returns>
        public DataTable QueryTop(string tableName, SqlCriteria sqlCriteria, string orderbyClause, int topNum)
        {
            int recordCount = 0;
            return Query(tableName, null, sqlCriteria, orderbyClause, 1, topNum, ref recordCount);
        }

        /// <summary>
        /// 查询数据
        /// </summary>
        /// <param name="tableName">表名</param>
        /// <param name="selectClause">需要查询的字段集</param>
        /// <param name="sqlCriteria">条件集</param>
        /// <param name="orderbyClause">排序集</param>
        /// <param name="pageIndex">起始页</param>
        /// <param name="pageSize">页大小</param>
        /// <param name="recordCount">结果集大小</param>
        /// <returns>结果集</returns>
        public DataTable Query(string tableName, string selectClause,
            SqlCriteria sqlCriteria, string orderbyClause, int pageIndex, int pageSize, ref int recordCount)
        {
            ThrowExceptionUtil.ArgumentNotNullOrEmpty(tableName, "tableName");

            DataTable cacheData = CacheManager.Instance.GetEntity<DataTable>(Constants.DBTableCacheName.FormatWith(tableName));
            // 需要被缓存
            if (cacheData != null)
            {
                if (cacheData.Rows.Count == 0)
                {
                    QueryBeeCommand queryBeeCommand = new QueryBeeCommand(this, tableName, "*",
                        null, orderbyClause, -1, -1);

                    cacheData = queryBeeCommand.Excute<DataTable>();
                    CacheManager.Instance.AddEntity<DataTable>(Constants.DBTableCacheName.FormatWith(tableName), cacheData, TimeSpan.MaxValue);
                }

                DataTable result = DataUtil.Query(cacheData, sqlCriteria, orderbyClause,
                    pageIndex, pageSize, ref recordCount);

                return result;
            }
            else
            {
                QueryBeeCommand queryBeeCommand = new QueryBeeCommand(this, tableName, selectClause,
                        sqlCriteria, orderbyClause, pageIndex, pageSize);

                DataTable result = queryBeeCommand.Excute<DataTable>();

                if (pageSize > 0 && pageIndex >= 0)
                {
                    if (recordCount <= 0)
                    {
                        QueryBeeCommand countBeeCommand = new QueryBeeCommand(this, tableName,
                            "count(*) as beecount", sqlCriteria, null, -1, -1);
                        DataTable countTable = countBeeCommand.Excute<DataTable>();
                        if (countTable != null && countTable.Rows.Count == 1)
                        {
                            int.TryParse(countTable.Rows[0]["beecount"].ToString(), out recordCount);
                        }
                    }
                }
                else
                {
                    recordCount = result.Rows.Count;
                }

                return result;
            }
        }

        /// <summary>
        /// sql文查询
        /// </summary>
        /// <param name="cmdText">sql文</param>
        /// <param name="dataAdapter">数据集</param>
        /// <returns>结果集</returns>
        public DataTable ExecuteCommand(string cmdText, BeeDataAdapter dataAdapter)
        {
            ThrowExceptionUtil.ArgumentNotNullOrEmpty(cmdText, "cmdText");

            CmdTextBeeCommand cmdTextBeeCommand = new CmdTextBeeCommand(this, cmdText, dataAdapter);

            return cmdTextBeeCommand.Excute<DataTable>();
        }

        public DataTable ExecuteCommandWhere(string cmdTextWithWhere, SqlCriteria sqlCriteria)
        {
            ThrowExceptionUtil.ArgumentNotNullOrEmpty(cmdTextWithWhere, "cmdTextWithWhere");

            CmdTextWithWhereBeeCommand cmdTextBeeCommand = new CmdTextWithWhereBeeCommand(this, cmdTextWithWhere, sqlCriteria);

            return cmdTextBeeCommand.Excute<DataTable>();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="dataTable"></param>
        /// <param name="mapping"></param>
        /// <param name="notifyHandler"></param>
        /// <param name="notifyAfter"></param>
        public void SqlBulkCopy(string tableName, DataTable dataTable, Dictionary<string, string> mapping, SqlRowsCopiedEventHandler notifyHandler, int notifyAfter)
        {
            ThrowExceptionUtil.ArgumentNotNullOrEmpty(tableName, "tableName");
            ThrowExceptionUtil.ArgumentConditionTrue(dataTable != null && dataTable.Rows.Count > 0, "dataTable", "data can not be null or empty");

            SqlServerBulkCopyCommand command = new SqlServerBulkCopyCommand(this, tableName, dataTable, mapping, notifyHandler, notifyAfter);

            command.Excute<int>();
        }

        /// <summary>
        /// 使用一段sql，进行分页查询， 目前只支持sqlserver
        /// </summary>
        /// <param name="sqlText"></param>
        /// <param name="orderbyClause"></param>
        /// <param name="pageIndex"></param>
        /// <param name="pageSize"></param>
        /// <param name="recordCount"></param>
        /// <returns></returns>
        public DataTable Query(string sqlText, string orderbyClause, int pageIndex, int pageSize, ref int recordCount)
        {
            ThrowExceptionUtil.ArgumentNotNullOrEmpty(sqlText, "sql");
            ThrowExceptionUtil.ArgumentNotNullOrEmpty(orderbyClause, "orderbyClause");

            if (pageSize > 0 && pageIndex >= 0)
            {
                if (recordCount <= 0)
                {
                    CmdTextBeeCommand countBeeCommand = new CmdTextBeeCommand(this, "select count(1) as beecount from ( {0} )t".FormatWith(sqlText), null);

                    DataTable countTable = countBeeCommand.Excute<DataTable>();
                    if (countTable != null && countTable.Rows.Count == 1)
                    {
                        int.TryParse(countTable.Rows[0]["beecount"].ToString(), out recordCount);
                    }
                }
            }

            string sql = this.dbDriver.GetPagedSelectCmdText(sqlText, orderbyClause, pageIndex, pageSize);

            CmdTextBeeCommand queryBeeCommand = new CmdTextBeeCommand(this, sql, null);

            return queryBeeCommand.Excute<DataTable>();
        }

        #endregion

        #region ORM Access

        /// <summary>
        /// 新增数据。
        /// 默认的表名就是类名， 若T类型上有指定的表名，则以此为准
        /// 默认的自增列为Id，若T类型上有指定的自增列， 则以此为准。
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <param name="value">类型值</param>
        /// <returns>插入返回的自增列</returns>
        public int Insert<T>(T value) where T : class
        {
            return Insert<T>(value, true);
        }

        /// <summary>
        /// 新增数据。
        /// 默认的表名就是类名， 若T类型上有指定的表名，则以此为准
        /// 默认的自增列为Id，若T类型上有指定的自增列， 则以此为准。
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <param name="value">类型值</param>
        /// <param name="removeIdentity">是否移除标示列，该值若为false， 则Insert语句中将可能包含自增列。</param>
        /// <returns>插入返回的自增列</returns>
        public int Insert<T>(T value, bool removeIdentity) where T : class
        {
            ThrowExceptionUtil.ArgumentNotNull(value, "value");

            Type typeInfo = typeof(T);
            EntityProxy<T> entityProxy = EntityProxyManager.Instance.GetEntityProxy<T>();

            string tableName = OrmUtil.GetTableName<T>();
            string identityColumnName = OrmUtil.GetIdentityColumnName<T>();

            BeeDataAdapter dataAdapter = BeeDataAdapter.From<T>(value);

            int result = Insert(tableName, dataAdapter, removeIdentity);

            if (result != -1)
            {
                entityProxy.SetPropertyValue(value, identityColumnName, result);
            }

            return result;
        }

        /// <summary>
        /// 删除数据
        /// </summary>
        /// <param name="sqlCriteria">条件集</param>
        public void Delete<T>(SqlCriteria sqlCriteria)
            where T : class
        {
            string tableName = OrmUtil.GetTableName<T>();

            DeleteBeeCommand deleteBeeCommand = new DeleteBeeCommand(this, tableName, sqlCriteria);
            deleteBeeCommand.Excute<int>();
        }


        /// <summary>
        /// 保存数据。 
        /// 默认的表名就是类名， 若T类型上有指定的表名，则以此为准
        /// 默认的自增列为Id，若T类型上有指定的自增列， 则以此为准。
        /// 若value中的标识列大于0， 则修改。 若小于等于0， 则新增。
        /// 注：主键为int
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <param name="value">类型值</param>
        /// <returns>新增则返回自增列， 修改则返回本身标示列值。</returns>
        public int Save<T>(T value) where T : class
        {
            ThrowExceptionUtil.ArgumentNotNull(value, "value");

            string identityColumnName = OrmUtil.GetIdentityColumnName<T>();

            string tableName = OrmUtil.GetTableName<T>();

            EntityProxy<T> entityProxy = EntityProxyManager.Instance.GetEntityProxy<T>();

            object identity = entityProxy.GetPropertyValue(value, identityColumnName);

            ThrowExceptionUtil.ArgumentConditionTrue(identity != null, string.Empty, "未指定主键列");

            if ((int)identity <= 0)
            {
                return Insert<T>(value);
            }
            else
            {
                BeeDataAdapter dataAdapter = BeeDataAdapter.From<T>(value);
                dataAdapter.RemoveKey(identityColumnName);

                Update(tableName, dataAdapter, SqlCriteria.New.Equal(identityColumnName, identity));

                return (int)identity;
            }
        }

        /// <summary>
        /// 查询数据.
        /// 若类型特性上说明缓存的， 则走缓存。
        /// 否则直接从数据库中查询， 不走缓存。
        /// </summary>
        /// <typeparam name="T">类型值</typeparam>
        /// <returns>结果集</returns>
        public List<T> Query<T>()
            where T : class
        {
            int recordCount = 0;
            return Query<T>(null, null, -1, -1, ref recordCount);
        }

        /// <summary>
        /// 查询数据.
        /// 直接从数据库中查询， 不走缓存。
        /// </summary>
        /// <typeparam name="T">类型值</typeparam>
        /// <param name="sqlCriteria">条件集</param>
        /// <returns>结果集</returns>
        public List<T> Query<T>(SqlCriteria sqlCriteria)
            where T : class
        {
            int recordCount = 0;
            return Query<T>(sqlCriteria, null, -1, -1, ref recordCount);
        }

        /// <summary>
        /// 查询数据.
        /// 若不分页查询，并且条件集为null， 并且类型特性上说明缓存的， 则走缓存。
        /// 否则直接从数据库中查询， 不走缓存。
        /// </summary>
        /// <typeparam name="T">类型值</typeparam>
        /// <param name="sqlCriteria">条件集</param>
        /// <param name="orderbyClause">排序集</param>
        /// <param name="pageIndex">起始页</param>
        /// <param name="pageSize">页大小</param>
        /// <param name="recordCount">结果集大小</param>
        /// <returns>结果集</returns>
        public List<T> Query<T>(SqlCriteria sqlCriteria, string orderbyClause,
            int pageIndex, int pageSize, ref int recordCount)
            where T : class
        {
            //if (string.IsNullOrEmpty(orderbyClause))
            //{
            //    orderbyClause = OrmUtil.GetIdentityColumnName<T>();
            //}

            List<T> list = new List<T>();
            string tableName = OrmUtil.GetTableName<T>();

            if (OrmUtil.CheckCacheFlag<T>())
            {
                RegisterCacheTable(tableName, true);
            }

            DataTable dataTable = Query(tableName, "*", sqlCriteria, orderbyClause, pageIndex, pageSize, ref recordCount);

            if (dataTable != null)
            {
                list = ConvertUtil.ConvertDataToObject<T>(dataTable);
            }

            return list;
        }

        /// <summary>
        /// sql文查询
        /// </summary>
        /// <param name="cmdText">sql文</param>
        /// <param name="dataAdapter">数据集</param>
        /// <returns>结果集</returns>
        public List<T> ExecuteCommand<T>(string cmdText, BeeDataAdapter dataAdapter)
        {
            List<T> list = new List<T>();
            DataTable dataTable = ExecuteCommand(cmdText, dataAdapter);

            if (dataTable != null)
            {
                list = ConvertUtil.ConvertDataToObject<T>(dataTable);
            }

            return list;
        }

        public IEnumerable<T> ExecuteDataReader<T>(string cmdText, BeeDataAdapter dataAdapter)
               where T : class
        {
            ThrowExceptionUtil.ArgumentNotNullOrEmpty(cmdText, "cmdText");

            CmdTextBeeCommand cmdTextBeeCommand = new CmdTextBeeCommand(this, cmdText, dataAdapter);

            return cmdTextBeeCommand.DataRead<T>();
        }

        #endregion

        #region Schema Access

        public List<DbObject> GetDbObject()
        {
            return dbDriver.GetDbObjectList();
        }

        public TableSchema GetTableSchema(string tableName)
        {
            TableSchema result = null;
            try
            {
                result = CacheManager.Instance.GetEntity<TableSchema, string>(
                    Constants.BeeDataTableSchemaCacheCategory + dbDriver.ConnectionName,
                    tableName, TimeSpan.FromHours(1), tableNamePara =>
                    {
                        return dbDriver.GetTableSchema(tableNamePara);
                    });
            }
            catch (KeyNotFoundException)
            {
                ThrowExceptionUtil.ThrowMessageException("表中含有不支持的数据类型");
            }
            catch (Exception)
            {
                throw;
            }

            return result;
        }

        public SPSchema GetSpSchema(string spName)
        {
            return CacheManager.Instance.GetEntity<SPSchema, string>(
                Constants.BeeDataTableSchemaCacheCategory + dbDriver.ConnectionName,
                spName, TimeSpan.FromHours(1), spNamePara =>
                {
                    return dbDriver.GetSpSchema(spNamePara);
                });
        }

        public string ToCreateTableScript(TableSchema tableSchema)
        {
            return dbDriver.ToCreateTableScript(tableSchema);
        }

        public TableSchema GetEntitySchema(Type type)
        {
            IEntityProxy proxy = EntityProxyManager.Instance.GetEntityProxyFromType(type);
            TableSchema tableSchema = new TableSchema(OrmUtil.GetTableName(type));
            foreach (PropertySchema item in proxy.GetPropertyList())
            {
                ColumnSchema columnSchema = new ColumnSchema(item.Name);
                OrmColumnAttribute ormColumnAttribute = item.GetCustomerAttribute<OrmColumnAttribute>();
                if (ormColumnAttribute != null)
                {
                    if (!string.IsNullOrEmpty(ormColumnAttribute.DbColumnName))
                    {
                        columnSchema.ColumnName = ormColumnAttribute.DbColumnName;
                    }

                    columnSchema.IsPrimary = ormColumnAttribute.PrimaryKeyFlag;
                    columnSchema.IsNullable = ormColumnAttribute.AllowNullFlag;
                }
                columnSchema.Type = item.PropertyType;
                columnSchema.IsIdentity = string.Compare(columnSchema.ColumnName, "id", true) == 0;
                if (columnSchema.Type == typeof(string))
                {
                    columnSchema.MaxLength = 200;
                }

                columnSchema.IsNullable = true;

                tableSchema.ColumnList.Add(columnSchema);
            }

            return tableSchema;
        }

        #endregion

        #region Helper

        public bool IsExist(string tableName, SqlCriteria sqlCriteria)
        {
            bool result = true;
            int recordCount = 0;
            if (sqlCriteria != null)
            {
                DataTable dataTable = Query(tableName, "count(*)", sqlCriteria, null, 0, 0, ref recordCount);

                result = int.Parse(dataTable.Rows[0][0].ToString()) > 0;
            }

            return result;
        }

        #endregion

        public override string ToString()
        {
            string result = connectionName;
            DbSession dbSession = _parent;

            while (dbSession != null)
            {
                result = "{0}---->{1}".FormatWith(dbSession.connectionName, result);
                dbSession = dbSession._parent;
            }

            return result;
        }

        #endregion

        #region Private Methods



        #endregion

        #region Static Methods

        /// <summary>
        /// 根据cacheFlag标示表是否被缓存。
        /// </summary>
        /// <param name="tableName">缓存表</param>
        /// <param name="cacheFlag">是否被缓存</param>
        public static void RegisterCacheTable(string tableName, bool cacheFlag)
        {
            if (cacheFlag)
            {
                string cacheName = Constants.DBTableCacheName.FormatWith(tableName);
                DataTable table = CacheManager.Instance.GetEntity<DataTable>(cacheName);
                if (table == null)
                {
                    CacheManager.Instance.AddEntity<DataTable>(cacheName,
                        new DataTable(), TimeSpan.MaxValue);
                }
            }
            else
            {
                CacheManager.Instance.RemoveCache(tableName);
            }
        }


        /// <summary>
        /// 注册数据源
        /// </summary>
        /// <param name="connectionName">连接字符串名</param>
        /// <param name="providerName">数据源名</param>
        /// <param name="connectionString">连接字符串</param>
        public static void Register(string connectionName,
            string providerName, string connectionString)
        {
            ThrowExceptionUtil.ArgumentNotNullOrEmpty(connectionName, "connectionName");
            ThrowExceptionUtil.ArgumentNotNullOrEmpty(providerName, "providerName");
            ThrowExceptionUtil.ArgumentNotNullOrEmpty(connectionString, "connectionString");

            DbDriverFactory.Instance.RegisterDbDriver(connectionName, providerName, connectionString);

            InitDefaultDbSession();
        }

        public static string ToCreateTableScript(DbDriverType dbType, TableSchema tableSchema)
        {
            string result = "";
            if (dbType == DbDriverType.Mysql)
            {
                result = new MySqlDriver(null, "", "").ToCreateTableScript(tableSchema);
            }
            else if (dbType == DbDriverType.Oracle)
            {
                result = new OracleDriver(null, "", "").ToCreateTableScript(tableSchema);
            }
            else if (dbType == DbDriverType.Sqlite)
            {
                result = new SqliteDriver(null, "", "").ToCreateTableScript(tableSchema);
            }
            else
            {
                result = new SqlServer2000Driver(null, "", "").ToCreateTableScript(tableSchema);
            }

            return result;
        }

        private static void InitDefaultDbSession()
        {
            if (defaultDbSession == null)
            {
                // Generate the default session.
                // use the first one of the connectionstring.
                DbDriver driver = DbDriverFactory.Instance.GetDefaultDriver();
                if (driver != null)
                {
                    Logger.Debug("DefaultDriver:" + driver.ConnectionName);
                    defaultDbSession = new DbSession(driver);
                }
            }
        }

        #endregion

        #region IDisposable 成员

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;

                if (!IsDefaultSession)
                {
                    _head = _parent;
                    Thread.EndThreadAffinity();

                    // 假如非默认session， 则在dispose时关闭dbconnection
                    // 默认的每个方法调用则认为是一个connection, 在command中关闭

                    CloseConnection();
                }

            }
        }

        #endregion
    }
}
