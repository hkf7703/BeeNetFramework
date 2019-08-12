using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Bee.Data;
using Bee.Logging;
using Bee.Util;
using Bee.Core;
using System.Data;

namespace Bee.Web
{
    /// <summary>
    /// The base controller with type.
    /// </summary>
    /// <typeparam name="T">the type.</typeparam>
    public class ControllerBase<T> : ControllerBase where T : class
    {
        private static BeeAutoModelInfo autoModelInfo;

        public ControllerBase()
        {
            if (OrmUtil.CheckCacheFlag<T>())
            {
                DbSession.RegisterCacheTable(TableName, true);
            }

            EntityProxy<T> entityProxy = EntityProxyManager.Instance.GetEntityProxy<T>();

            foreach (PropertySchema item in entityProxy.GetPropertyList())
            {
                if (item.PropertyType.IsEnum)
                {
                    Dictionary<string, string> dict = new Dictionary<string, string>();

                    foreach (object enumItem in Enum.GetValues(item.PropertyType))
                    {
                        long l = Convert.ToInt64(enumItem);
                        dict.Add(l.ToString(), enumItem.ToString());
                    }

                    DataMapping.Instance.Add(item.PropertyType.ToString(), dict);
                }
            }
        }

        protected string TableName
        {
            get
            {
                return OrmUtil.GetTableName<T>();
            }
        }

        protected override void InitPagePara(BeeDataAdapter dataAdapter)
        {
            EntityProxy<T> entityProxy = EntityProxyManager.Instance.GetEntityProxy<T>();
            ModelAttribute modelAttribute = entityProxy.GetCustomerAttribute<ModelAttribute>();
            string identityColumnName = OrmUtil.GetIdentityColumnName<T>();
            int defaultPageSize = 20;
            string defaultOrderField = identityColumnName;
            string defaultOrderDirection = "desc";
            if (modelAttribute != null)
            {
                defaultPageSize = modelAttribute.PageSize;
                defaultOrderField = modelAttribute.DefaultOrderField;
                defaultOrderDirection = modelAttribute.DefaultOrderAscFlag ? "asc" : "desc";
            }

            ViewData.TryGetValue<int>("pagenum", 1, true);
            ViewData.TryGetValue<int>("pagesize", defaultPageSize, true);
            ViewData.TryGetValue<int>("recordcount", 0, true);
            ViewData.TryGetValue<string>("orderField", defaultOrderField, true);
            ViewData.TryGetValue<string>("orderDirection", defaultOrderDirection, true);
        }

        public virtual PageResult Index(BeeDataAdapter dataAdapter)
        {
            InitPagePara(dataAdapter);

            return new PageResult(ControllerName, "BeeAutoList");
        }

        public virtual int Save(T obj)
        {
            int identity = -1;
            using (DbSession dbSession = GetDbSession())
            {
                identity = dbSession.Save(obj);
            }

            return identity;
        }

        public virtual void Delete(int id)
        {
            using (DbSession dbSession = GetDbSession())
            {
                dbSession.Delete<T>(SqlCriteria.New.Equal(OrmUtil.GetIdentityColumnName<T>(), id));
            }
        }

        public virtual PageResult Show(int id)
        {
            BeeDataAdapter dataAdapter = new BeeDataAdapter();
            if (id >= 0)
            {
                using (DbSession dbSession = GetDbSession())
                {
                    T result =
                        dbSession.Query<T>(SqlCriteria.New.Equal(OrmUtil.GetIdentityColumnName<T>(), id)).FirstOrDefault();
                    dataAdapter = BeeDataAdapter.From<T>(result);
                }
            }

            ViewData.Merge(dataAdapter, true);

            return View("BeeAutoShow");
        }

        public virtual PageResult List(BeeDataAdapter dataAdapter)
        {
            DbSession dbSession = GetDbSession();

            DataTable dataTable = null;
            try
            {
                InitPagePara(dataAdapter);

                #region datetime 处理
                EntityProxy<T> entityProxy = EntityProxyManager.Instance.GetEntityProxy<T>();

                //List<PropertySchema> propertyList = entityProxy.GetPropertyList();

                //propertyList = (from item in propertyList
                // where item.PropertyType == typeof(DateTime)
                // select item).ToList();

                BeeDataAdapter realDataAdapter = new BeeDataAdapter(dataAdapter);

                //foreach (PropertySchema item in propertyList)
                //{
                //    string beginkey = "{0}begin".FormatWith(item.Name);
                //    string endkey = "{0}end".FormatWith(item.Name);
                //    if (realDataAdapter.ContainsKey(item.Name))
                //    {
                //        realDataAdapter[item.Name] = ConvertUtil.CommonConvert<DateTime>(realDataAdapter[item.Name]);
                //    }
                //    if (realDataAdapter.ContainsKey(beginkey))
                //    {
                //        realDataAdapter[beginkey] = ConvertUtil.CommonConvert<DateTime>(realDataAdapter[beginkey]);
                //    }
                //    if (realDataAdapter.ContainsKey(endkey))
                //    {
                //        realDataAdapter[endkey] = ConvertUtil.CommonConvert<DateTime>(realDataAdapter[endkey]);
                //    }
                //}

                #endregion

                string tableName = OrmUtil.GetTableName<T>();

                SqlCriteria sqlCriteria = GetQueryCondition(realDataAdapter);

                int recordCount = dataAdapter.TryGetValue<int>("recordcount", 0);

                string selectClause = GetQuerySelectClause(typeof(T));

                dataTable = InnerQuery(tableName, selectClause, dataAdapter, sqlCriteria);
            }
            catch (Exception e)
            {
                Logger.Error("List object({0}) Error".FormatWith(typeof(T)), e);
            }
            finally
            {
                dbSession.Dispose();
            }

            return View(null, "BeeAutoList", dataTable);
        }

        protected virtual SqlCriteria GetQueryCondition(BeeDataAdapter dataAdapter)
        {
            return GetQueryCondition(typeof(T), dataAdapter);
        }
        /*

        private List<BeeDataAdapter> GetSearchInfo(BeeDataAdapter dataAdapter)
        {
            List<BeeDataAdapter> result = new List<BeeDataAdapter>();

            EntityProxy<T> entityProxy = EntityProxyManager.Instance.GetEntityProxy<T>();
            foreach (PropertySchema propertySchema in entityProxy.GetPropertyList())
            {
                ModelPropertyAttribute modelPropertyAttribute
                    = propertySchema.GetCustomerAttribute<ModelPropertyAttribute>();
                if (modelPropertyAttribute != null)
                {
                    if (!modelPropertyAttribute.Visible)
                    {
                        continue;
                    }

                    if (!modelPropertyAttribute.Queryable)
                    {
                        continue;
                    }

                    BeeDataAdapter dataItem = new BeeDataAdapter();

                    string descriptionInfo = modelPropertyAttribute.Description;
                    if (string.IsNullOrEmpty(descriptionInfo))
                    {
                        descriptionInfo = propertySchema.Name;
                    }

                    dataItem.Add("name", propertySchema.Name);
                    dataItem.Add("Type", propertySchema.PropertyType);
                    dataItem.Add("QueryType", modelPropertyAttribute.QueryType);
                    dataItem.Add("Description", descriptionInfo);
                    dataItem.Add("MappingName", modelPropertyAttribute.MappingName);

                    result.Add(dataItem);
                }
            }

            return result;
        }

        private List<BeeDataAdapter> GetHeaderInfo()
        {
            List<BeeDataAdapter> result = new List<BeeDataAdapter>();
            EntityProxy<T> entityProxy = EntityProxyManager.Instance.GetEntityProxy<T>();

            ModelAttribute modelAttribute = entityProxy.GetCustomerAttribute<ModelAttribute>();

            foreach (PropertySchema propertySchema in entityProxy.GetPropertyList())
            {
                ModelPropertyAttribute modelPropertyAttribute
                    = propertySchema.GetCustomerAttribute<ModelPropertyAttribute>();

                BeeDataAdapter dataAdapter = new BeeDataAdapter();
                string descriptionInfo;
                if (modelPropertyAttribute != null)
                {
                    if (!modelPropertyAttribute.Visible)
                    {
                        continue;
                    }

                    descriptionInfo = modelPropertyAttribute.Description;
                    if (string.IsNullOrEmpty(descriptionInfo))
                    {
                        descriptionInfo = propertySchema.Name;
                    }

                    dataAdapter.Add("description", descriptionInfo);
                    dataAdapter.Add("name", propertySchema.Name);

                    if (modelPropertyAttribute.ColumnWidth != 0)
                    {
                        dataAdapter.Add("width", modelPropertyAttribute.ColumnWidth.ToString());
                    }

                    if (!string.IsNullOrEmpty(modelPropertyAttribute.Align))
                    {
                        dataAdapter.Add("align", modelPropertyAttribute.Align);
                    }

                    if (modelPropertyAttribute.OrderableFlag)
                    {
                        dataAdapter.Add("orderField", propertySchema.Name);
                    }

                }
                else
                {
                    dataAdapter.Add("description", propertySchema.Name);
                    dataAdapter.Add("Name", propertySchema.Name);
                }

                result.Add(dataAdapter);
            }


            return result;
        }

        private List<BeeDataAdapter> GetDetailInfo()
        {
            List<BeeDataAdapter> result = new List<BeeDataAdapter>();

            EntityProxy<T> entityProxy = EntityProxyManager.Instance.GetEntityProxy<T>();

            ModelAttribute modelAttribute = entityProxy.GetCustomerAttribute<ModelAttribute>();
            string identityColumn = OrmUtil.GetIdentityColumnName<T>();

            foreach (PropertySchema propertySchema in entityProxy.GetPropertyList())
            {
                ModelPropertyAttribute modelPropertyAttribute
                    = propertySchema.GetCustomerAttribute<ModelPropertyAttribute>();
                BeeDataAdapter dataAdapter = new BeeDataAdapter();
                string descriptionInfo;
                if (modelPropertyAttribute != null)
                {
                    descriptionInfo = modelPropertyAttribute.Description;
                    if (string.IsNullOrEmpty(descriptionInfo))
                    {
                        descriptionInfo = propertySchema.Name;
                    }
                }
                else
                {
                    descriptionInfo = propertySchema.Name;
                }

                dataAdapter.Add("description", descriptionInfo);
                dataAdapter.Add("name", propertySchema.Name);
                bool readOnly = false;

                if (string.Compare(identityColumn, propertySchema.Name, true) == 0)
                {
                    readOnly = true;
                }

                dataAdapter.Add("readonly", readOnly);
                dataAdapter.Add("mappingname", modelPropertyAttribute.MappingName);

                result.Add(dataAdapter);
            }

            return result;
        }
         * 
         */


        private BeeAutoModelInfo InitBeeAutoModelInfo()
        {
            BeeAutoModelInfo result = new BeeAutoModelInfo();

            List<BeeDataAdapter> headerInfo = new List<BeeDataAdapter>();
            List<BeeDataAdapter> searchInfo = new List<BeeDataAdapter>();
            List<BeeDataAdapter> detailInfo = new List<BeeDataAdapter>();
            Dictionary<string, string> dataMappingInfo = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);

            EntityProxy<T> entityProxy = EntityProxyManager.Instance.GetEntityProxy<T>();

            ModelAttribute modelAttribute = entityProxy.GetCustomerAttribute<ModelAttribute>();

            string identityColumn = OrmUtil.GetIdentityColumnName<T>();

            TableSchema tableSchema = null;
            using (DbSession dbSession = GetDbSession())
            {
                tableSchema = dbSession.GetTableSchema(OrmUtil.GetTableName<T>());
            }

            foreach (PropertySchema propertySchema in entityProxy.GetPropertyList())
            {
                string columnName = propertySchema.Name;
                OrmColumnAttribute ormColumnAttribute = propertySchema.GetCustomerAttribute<OrmColumnAttribute>();
                if (ormColumnAttribute != null && !string.IsNullOrEmpty(ormColumnAttribute.DbColumnName))
                {
                    columnName = ormColumnAttribute.DbColumnName;
                }

                if (tableSchema != null && !tableSchema.ContainsColumn(columnName))
                {
                    continue;
                }

                BeeDataAdapter headerItem = GetHeaderItem(propertySchema);
                if (headerItem != null)
                {
                    headerInfo.Add(headerItem);
                }

                BeeDataAdapter searchItem = GetSearchItem(propertySchema);
                if (searchItem != null)
                {
                    searchInfo.Add(searchItem);
                }

                BeeDataAdapter detailItem = GetDetailItem(propertySchema, identityColumn);
                if (detailItem != null)
                {
                    detailInfo.Add(detailItem);
                }

                ModelPropertyAttribute modelPropertyAttribute
                    = propertySchema.GetCustomerAttribute<ModelPropertyAttribute>();
                if (modelPropertyAttribute != null && !string.IsNullOrEmpty(modelPropertyAttribute.MappingName))
                {
                    dataMappingInfo.Add(propertySchema.Name, modelPropertyAttribute.MappingName);
                }

                if (propertySchema.PropertyType.IsEnum && !dataMappingInfo.ContainsKey("mappingname"))
                {
                    dataMappingInfo.Add(propertySchema.Name, propertySchema.PropertyType.ToString());
                }
            }

            result.DetailInfo = detailInfo;
            result.HeaderInfo = headerInfo;
            result.SearchInfo = searchInfo;
            result.DataMappingInfo = dataMappingInfo;

            return result;
        }

        private static BeeDataAdapter GetHeaderItem(PropertySchema propertySchema)
        {
            ModelPropertyAttribute modelPropertyAttribute
                    = propertySchema.GetCustomerAttribute<ModelPropertyAttribute>();

            BeeDataAdapter dataAdapter = new BeeDataAdapter();
            string descriptionInfo;
            if (modelPropertyAttribute != null)
            {
                if (!modelPropertyAttribute.Visible)
                {
                    return null;
                }
                descriptionInfo = modelPropertyAttribute.Description;
                if (string.IsNullOrEmpty(descriptionInfo))
                {
                    descriptionInfo = propertySchema.Name;
                }

                dataAdapter.Add("description", descriptionInfo);
                dataAdapter.Add("name", propertySchema.Name);

                if (modelPropertyAttribute.ColumnWidth != 0)
                {
                    dataAdapter.Add("width", modelPropertyAttribute.ColumnWidth.ToString());
                }

                if (!string.IsNullOrEmpty(modelPropertyAttribute.Align))
                {
                    dataAdapter.Add("align", modelPropertyAttribute.Align);
                }

                if (modelPropertyAttribute.OrderableFlag)
                {
                    dataAdapter.Add("orderField", propertySchema.Name);
                }

            }
            else
            {
                dataAdapter.Add("description", propertySchema.Name);
                dataAdapter.Add("Name", propertySchema.Name);
            }

            return dataAdapter;
        }

        private BeeDataAdapter GetSearchItem(PropertySchema propertySchema)
        {
            BeeDataAdapter dataAdapter = null;
            ModelPropertyAttribute modelPropertyAttribute
                    = propertySchema.GetCustomerAttribute<ModelPropertyAttribute>();
            if (modelPropertyAttribute != null)
            {
                if (!modelPropertyAttribute.Visible)
                {
                    return null;
                }

                if (!modelPropertyAttribute.Queryable)
                {
                    return null;
                }

                dataAdapter = new BeeDataAdapter();

                string descriptionInfo = modelPropertyAttribute.Description;
                if (string.IsNullOrEmpty(descriptionInfo))
                {
                    descriptionInfo = propertySchema.Name;
                }

                dataAdapter.Add("name", propertySchema.Name);
                dataAdapter.Add("Type", propertySchema.PropertyType);
                dataAdapter.Add("QueryType", modelPropertyAttribute.QueryType);
                dataAdapter.Add("Description", descriptionInfo);

                if (!string.IsNullOrEmpty(modelPropertyAttribute.MappingName))
                {
                    dataAdapter.Add("MappingName", modelPropertyAttribute.MappingName);
                }

                if (propertySchema.PropertyType.IsEnum && !dataAdapter.ContainsKey("MappingName"))
                {
                    dataAdapter.Add("MappingName", propertySchema.PropertyType.ToString());
                }
            }

            return dataAdapter;
        }

        private BeeDataAdapter GetDetailItem(PropertySchema propertySchema, string identityColumn)
        {
            ModelPropertyAttribute modelPropertyAttribute
                    = propertySchema.GetCustomerAttribute<ModelPropertyAttribute>();
            BeeDataAdapter dataAdapter = new BeeDataAdapter();
            string descriptionInfo;
            bool readOnly = false;

            if (string.Compare(identityColumn, propertySchema.Name, true) == 0)
            {
                dataAdapter.Add("showonly", true);
                readOnly = true;
            }

            if (modelPropertyAttribute != null)
            {
                descriptionInfo = modelPropertyAttribute.Description;
                if (string.IsNullOrEmpty(descriptionInfo))
                {
                    descriptionInfo = propertySchema.Name;
                }

                if (!modelPropertyAttribute.Visible)
                {
                    dataAdapter.Add("visible", false);
                }
                if (!string.IsNullOrEmpty(modelPropertyAttribute.MappingName))
                {
                    dataAdapter.Add("mappingname", modelPropertyAttribute.MappingName);
                }

                readOnly = readOnly || modelPropertyAttribute.ReadonlyFlag;

                if (!readOnly)
                {
                    if (propertySchema.PropertyType.UnderlyingSystemType == typeof(DateTime))
                    {
                        if (string.Compare("modifytime", propertySchema.Name, true) == 0
                            || string.Compare("updatetime", propertySchema.Name, true) == 0
                            || string.Compare("createtime", propertySchema.Name, true) == 0)
                        {
                            readOnly = true;
                            dataAdapter.Add("showonly", true);
                        }
                    }
                }
            }
            else
            {
                descriptionInfo = propertySchema.Name;
            }

            dataAdapter.Add("description", descriptionInfo);
            dataAdapter.Add("name", propertySchema.Name);
            dataAdapter.Add("readonly", readOnly);

            if (propertySchema.PropertyType == typeof(DateTime))
            {
                dataAdapter.Add("date", true);
            }

            if (propertySchema.PropertyType.IsEnum && !dataAdapter.ContainsKey("mappingname"))
            {
                dataAdapter.Add("mappingname", propertySchema.PropertyType.ToString());
            }

            return dataAdapter;
        }

        internal override BeeAutoModelInfo AutoModelInfo()
        {
            if (autoModelInfo == null)
            {
                autoModelInfo = InitBeeAutoModelInfo();
            }

            return autoModelInfo;
        }
    }
}
