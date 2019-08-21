using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Web.Script.Serialization;

namespace Bee.Data
{
    public enum DbObjectType
    {
        Table,
        View,
        SP
    }

    public enum DbDriverType
    {
        SqlServer,
        Oracle,
        Sqlite,
        Mysql
    }

    public class DbObject
    {
        // Fields
        private string objectName;
        private DbObjectType objectType;

        // Methods
        public DbObject(string objectName, DbObjectType objectType)
        {
            this.objectName = objectName;
            this.objectType = objectType;
        }

        public override bool Equals(object obj)
        {
            bool result = false;
            DbObject schema = obj as DbObject;
            if ((schema != null) && ((string.Compare(this.objectName, schema.objectName, true) == 0) && (this.objectType == schema.objectType)))
            {
                result = true;
            }
            return result;
        }

        public override int GetHashCode()
        {
            return ((this.objectName.GetHashCode() * 0x11) + (int)this.objectType);
        }

        public override string ToString()
        {
            return string.Format("DbObjectName:{0},DbObjectType:{1}", this.objectName, this.objectType.ToString());
        }

        // Properties
        public string DbObjectName
        {
            get
            {
                return this.objectName;
            }
        }

        public DbObjectType ObjectType
        {
            get
            {
                return this.objectType;
            }
        }
    }

    public class SPSchema
    {
        // Fields
        private List<SPParameter> parameterList;
        private string spName;

        // Methods
        public SPSchema(string spName)
        {
            this.spName = spName.ToLower();
        }

        public override bool Equals(object obj)
        {
            bool result = false;
            SPSchema schema = obj as SPSchema;
            if ((schema != null) && (string.Compare(this.spName, schema.spName, true) == 0))
            {
                result = true;
            }
            return result;
        }

        public override int GetHashCode()
        {
            return this.spName.ToLower().GetHashCode();
        }

        public override string ToString()
        {
            return string.Format("SPName:{0}", this.spName);
        }

        public List<SPParameter> ParameterList
        {
            get
            {
                if (this.parameterList == null)
                {
                    this.parameterList = new List<SPParameter>();
                }
                return this.parameterList;
            }
        }

        public string Name
        {
            get
            {
                return this.spName;
            }
        }
    }

    public class SPParameter
    {
        private ParameterDirection direction;
        private string paraDbType;
        private string paraName;
        private int maxLength;

        public SPParameter(string name, string dbType, int maxLength, ParameterDirection direction)
        {
            this.paraName = name;
            this.paraDbType = dbType;
            this.direction = direction;
            this.maxLength = maxLength;
        }

        public override bool Equals(object obj)
        {
            bool result = false;
            SPParameter parameter = obj as SPParameter;
            if ((parameter != null) && (string.Compare(parameter.Name, this.Name, true) == 0))
            {
                result = true;
            }
            return result;
        }

        public override int GetHashCode()
        {
            return this.Name.GetHashCode();
        }

        public override string ToString()
        {
            return ("SPParameter:" + this.paraName);
        }

        // Properties
        public string DbType
        {
            get
            {
                return this.paraDbType;
            }
        }

        public ParameterDirection Direction
        {
            get
            {
                return this.direction;
            }
            set
            {
                this.direction = value;
            }
        }

        public string Name
        {
            get
            {
                return this.paraName;
            }
        }

        public int MaxLength
        {
            get
            {
                return this.maxLength;
            }
        }
    }

    public class TableSchema
    {
        // Fields
        private List<ColumnSchema> columnList;
        private List<ForeignKey> foreignKeyList;
        private string tableName;

        // Methods
        public TableSchema(string tableName)
        {
            this.tableName = tableName;
        }

        public override bool Equals(object obj)
        {
            bool result = false;
            TableSchema schema = obj as TableSchema;
            if ((schema != null) && (string.Compare(this.tableName, schema.tableName, true) == 0))
            {
                result = true;
            }
            return result;
        }

        public ColumnSchema GetColumn(string columnName)
        {
            ColumnSchema result = null;

            result = (from item in columnList
                      where string.Compare(columnName, item.ColumnName, true) == 0
                      select item).FirstOrDefault();

            return result;
        }

        public bool ContainsColumn(string columnName)
        {
            return ColumnList.Contains(new ColumnSchema(columnName));
        }

        public override int GetHashCode()
        {
            return this.tableName.GetHashCode();
        }

        private bool PredictIdentityColumn(ColumnSchema columnSchema)
        {
            return columnSchema.IsIdentity;
        }

        private bool PredictPrimaryColumn(ColumnSchema columnSchema)
        {
            return columnSchema.IsPrimary;
        }

        public override string ToString()
        {
            return string.Format("TableName :{0}", this.tableName);
        }

        // Properties
        public List<ColumnSchema> ColumnList
        {
            get
            {
                if (this.columnList == null)
                {
                    this.columnList = new List<ColumnSchema>();
                }
                return this.columnList;
            }
        }

        public List<ForeignKey> ForeignKeyList
        {
            get
            {
                if (this.foreignKeyList == null)
                {
                    this.foreignKeyList = new List<ForeignKey>();
                }
                return this.foreignKeyList;
            }
        }

        public ColumnSchema IdentityColumn
        {
            get
            {
                return this.ColumnList.Find(new Predicate<ColumnSchema>(this.PredictIdentityColumn));
            }
        }

        public List<ColumnSchema> PrimaryColumnList
        {
            get
            {
                return this.ColumnList.FindAll(new Predicate<ColumnSchema>(this.PredictPrimaryColumn));
            }
        }

        public string TableName
        {
            get
            {
                return this.tableName;
            }
        }
    }

    public class ColumnSchema
    {
        public ColumnSchema(string columnName)
        {
            // 忘了当时加了ToLower的原因了。 但是导致了生成的Entity都变成小写了， 暂去掉。
            this.ColumnName = columnName;

            IsComputeField = false;
        }
        
        public override bool Equals(object obj)
        {
            bool result = false;
            ColumnSchema schema = obj as ColumnSchema;
            if ((schema != null) && (string.Compare(this.ColumnName, schema.ColumnName, true) == 0))
            {
                result = true;
            }
            return result;
        }

        public override int GetHashCode()
        {
            return this.ColumnName.GetHashCode();
        }

        public override string ToString()
        {
            return string.Format("ColumnName:{0}", this.ColumnName);
        }

        public bool IsNullable
        {
            get;
            set;
        }

        public bool IsIdentity
        {
            get;
            set;
        }

        public string ColumnName
        {
            get;
            set;
        }

        public string ColumnType
        {
            get;
            set;
        }

        public bool IsPrimary
        {
            get;
            set;
        }

        public int MaxLength
        {
            get;
            set;
        }

        public bool IsComputeField
        {
            get;
            set;
        }

        [Bee.Util.BeeJson(IgnoreFlag=true)]
        public Type Type
        {
            get;
            set;
        }

        public string Description
        {
            get;
            set;
        }
    }

    public class ForeignKey
    {
        // Fields
        private string columnName;
        private string foreignColumnName;
        private string foreignTableName;

        // Methods
        public ForeignKey(string columnName, string foreignTableName, string foreignColumnName)
        {
            this.columnName = columnName.ToLower();
            this.foreignTableName = foreignTableName.ToLower();
            this.foreignColumnName = foreignColumnName.ToLower();
        }

        public override bool Equals(object obj)
        {
            bool result = false;
            ForeignKey schema = obj as ForeignKey;
            if ((schema != null) && (((string.Compare(this.columnName, schema.ColumnName, true) == 0) && (string.Compare(this.foreignTableName, schema.ForeignTableName, true) == 0)) && (string.Compare(this.foreignColumnName, schema.ForeignColumnName, true) == 0)))
            {
                result = true;
            }
            return result;
        }

        public override int GetHashCode()
        {
            return (((this.columnName.ToLower().GetHashCode() * 0x11) + (this.foreignTableName.GetHashCode() * 0x11)) + (this.foreignColumnName.GetHashCode() * 0x11));
        }

        public override string ToString()
        {
            return string.Format("ColumnName:{0},ForeignTable:{1},ForeignColumn:{2}", this.columnName, this.foreignTableName, this.foreignColumnName);
        }

        // Properties
        public string ColumnName
        {
            get
            {
                return this.columnName;
            }
        }

        public string ForeignColumnName
        {
            get
            {
                return this.foreignColumnName;
            }
        }

        public string ForeignTableName
        {
            get
            {
                return this.foreignTableName;
            }
        }
    }
}
