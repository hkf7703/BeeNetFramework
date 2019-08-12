using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;
using Bee.Util;
using System.Data.Common;
using System.Data;
using System.Reflection;

namespace Bee.Data
{
    /// <summary>
    /// According to the connection string of the setting to use 
    /// the corrected dbdriver.
    /// </summary>
    internal class DbDriverFactory : FlyweightBase<string, DbDriver>
    {
        private static DbDriverFactory instance = new DbDriverFactory();
        private const string DriverClassNameTemplate = "{0}Driver";
        private Dictionary<string, string> ruleDict = new Dictionary<string, string>();
        private Dictionary<string, Type> driverNameDict = new Dictionary<string, Type>(StringComparer.InvariantCultureIgnoreCase);

        private DataTable providerTable;

        private DbDriverFactory()
        {
            ruleDict.Add("Mysql", "MySql");
            ruleDict.Add("Oracle", "Oracle");
            ruleDict.Add("Ole", "Ole");
            ruleDict.Add("Odbc", "Db");
            ruleDict.Add("SqlClient", "SqlServer2008");
            ruleDict.Add("SQLite", "Sqlite");

            ruleDict.Add("Pgsql", "pgsql");
            

            driverNameDict.Add("SqlServer2000Driver", typeof(SqlServer2000Driver));
            driverNameDict.Add("SqlServer2005Driver", typeof(SqlServer2005Driver));
            driverNameDict.Add("SqlServer2008Driver", typeof(SqlServer2008Driver));
            driverNameDict.Add("OleDriver", typeof(OleDriver));
            driverNameDict.Add("MySqlDriver", typeof(MySqlDriver));
            driverNameDict.Add("OracleDriver", typeof(OracleDriver));
            driverNameDict.Add("SqliteDriver", typeof(SqliteDriver));
            driverNameDict.Add("PgsqlDriver", typeof(PgsqlDriver));

            providerTable = DbProviderFactories.GetFactoryClasses();

        }

        public static DbDriverFactory Instance
        {
            get
            {
                return instance;
            }
        }

        internal DbDriver GetDefaultDriver()
        {
            if (InnerDict.Count == 0)
            {
                foreach (ConnectionStringSettings item in ConfigurationManager.ConnectionStrings)
                {
                    if (!string.IsNullOrEmpty(item.ConnectionString)
                        && !string.IsNullOrEmpty(item.ProviderName)
                        && item.ElementInformation.IsPresent)
                    {

                        GeneralUtil.CatchAll(() =>
                        {
                            RegisterDbDriver(item.Name, item.ProviderName, item.ConnectionString);
                        }
                        , true
                        );
                    }
                }
            }

            // 获取去第一个
            foreach (string item in InnerDict.Keys)
            {
                return InnerDict[item];
            }

            return null;
        }

        internal void RegisterDbDriver(string connectionName,
            string providerName, string connectionString)
        {
            string[] keyArray = connectionName.Split("#".ToCharArray());
            if (keyArray.Length >= 1)
            {
                if (!InnerDict.ContainsKey(keyArray[0]))
                {
                    DbDriver driver = GetDbDriver(connectionName, providerName, connectionString);
                    InnerDict.Add(keyArray[0], driver);
                }
                else
                {
                    DbDriver driver = GetDbDriver(connectionName, providerName, connectionString);
                    InnerDict[keyArray[0]] = driver;
                }
            }
        }

        private DbDriver GetDbDriver(string connectionName,
            string providerName, string connectionString)
        {
            DbDriver driver = null;
            string driverName = string.Empty;
            DataRow[] rows = providerTable.Select(string.Format("invariantname='{0}'", providerName));

            DbProviderFactory dbProviderFactory = null;

            if (rows != null && rows.Length > 0)
            {
                dbProviderFactory = DbProviderFactories.GetFactory(rows[0]);
            }
            else
            {
                if (string.Compare(providerName, "System.Data.SQLite", true) == 0)
                {
                    dbProviderFactory = ReflectionUtil.CreateInstance("System.Data.SQLite.SQLiteFactory, System.Data.SQLite") as DbProviderFactory;
                }
                else if (string.Compare(providerName, "Npgsql", true) == 0)
                {
                    dbProviderFactory = ReflectionUtil.CreateInstance("Npgsql.NpgsqlFactory, Npgsql") as DbProviderFactory;
                }
            }

            ThrowExceptionUtil.ArgumentConditionTrue(dbProviderFactory != null, "connectionKey",
                    "the providerName of the connection setting '{0}' error, please check it.".FormatWith(connectionName));

            string[] keyArray = connectionName.Split("#".ToCharArray());
            if (keyArray.Length == 2)
            {
                string key = keyArray[0];
                driverName = keyArray[1];

                driver = ReflectionUtil.CreateInstance(driverNameDict[string.Format(DriverClassNameTemplate, driverName)],
                    dbProviderFactory, connectionString, connectionName) as DbDriver;

            }

            if (driver == null)
            {
                foreach (string key in ruleDict.Keys)
                {
                    if (providerName.IndexOf(key, StringComparison.CurrentCultureIgnoreCase) >= 0)
                    {
                        driverName = ruleDict[key];
                        break;
                    }
                }

                driver = ReflectionUtil.CreateInstance(driverNameDict[string.Format(DriverClassNameTemplate, driverName)],
                    dbProviderFactory, connectionString, connectionName) as DbDriver;
            }

            ThrowExceptionUtil.ArgumentConditionTrue(driver != null, "connectionName",
                    "the connection setting '{0}' error, please check it.".FormatWith(connectionName));

            return driver;
        }

        protected override DbDriver CreateInstance(string connectionName)
        {
            DbDriver driver = null;
            string driverName = string.Empty;

            ConnectionStringSettings setting = ConfigUtil.InnerGetConnectionString(connectionName);

            ThrowExceptionUtil.ArgumentConditionTrue(setting != null, "connectionKey",
                "there is no connectionKey named '{0}'".FormatWith(connectionName));

            driver = GetDbDriver(connectionName, setting.ProviderName, setting.ConnectionString);

            return driver;
        }
    }
}
