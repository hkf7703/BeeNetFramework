using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Common;
using Bee.Core;

namespace Bee.Data
{
    internal sealed class DataAdapterParser
    {
        // Fields
        private string columnClause;
        private List<DbParameter> dbParameterList;
        private string parameterClause;
        private string updateClause;
        private DbSession owner;

        public DataAdapterParser(DbSession owner, BeeDataAdapter dataAdapter)
        {
            this.owner = owner;
            this.dbParameterList = new List<DbParameter>();
            this.Init(dataAdapter);
        }

        private void Init(BeeDataAdapter dataAdapter)
        {
            StringBuilder columnClauseBuilder = new StringBuilder();
            StringBuilder parameterClauseBuilder = new StringBuilder();
            StringBuilder updateClauseBuilder = new StringBuilder();
            if (dataAdapter != null && dataAdapter.Count > 0)
            {
                int index = 0;

                foreach (string fieldName in dataAdapter.Keys)
                {
                    index++;

                    string columnName = owner.DbDriver.FormatField(fieldName);
                    string parameterName = string.Format("{0}{1}", owner.DbDriver.ParameterPrefix, fieldName);
                    columnClauseBuilder.Append(columnName);
                    parameterClauseBuilder.Append(parameterName);
                    updateClauseBuilder.Append(columnName).Append("=").Append(parameterName);
                    columnClauseBuilder.Append(",");
                    parameterClauseBuilder.Append(",");
                    updateClauseBuilder.Append(",");

                    DbParameter parameter = owner.DbDriver.CreateParameter();
                    parameter.ParameterName = fieldName;
                    parameter.Value = dataAdapter[fieldName];
                    if (parameter.Value is DateTime)
                    {
                        parameter.DbType = System.Data.DbType.DateTime;
                    }
                    else
                    {
                        //  to do nothing
                    }

                    if (parameter != null)
                    {
                        this.dbParameterList.Add(parameter);
                    }
                }
                columnClauseBuilder.Remove(columnClauseBuilder.Length - 1, 1);
                parameterClauseBuilder.Remove(parameterClauseBuilder.Length - 1, 1);
                updateClauseBuilder.Remove(updateClauseBuilder.Length - 1, 1);
            }
            this.parameterClause = parameterClauseBuilder.ToString();
            this.columnClause = columnClauseBuilder.ToString();
            this.updateClause = updateClauseBuilder.ToString();
        }

        // Properties
        internal string ColumnClause
        {
            get
            {
                return this.columnClause;
            }
        }

        internal List<DbParameter> DbParameterList
        {
            get
            {
                return this.dbParameterList;
            }
        }

        internal string ParameterClause
        {
            get
            {
                return this.parameterClause;
            }
        }

        internal string UpdateClause
        {
            get
            {
                return this.updateClause;
            }
        }
    }


}
