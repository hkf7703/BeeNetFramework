using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.SessionState;
using System.Web;
using Bee.Core;
using Bee.Data;
using System.Data;
using Bee.Util;
using System.IO;

namespace Bee.Web
{
    public abstract class BeeControllerBase
    {
        private ControllerInfo controllerInfo;
        private BeeDataAdapter dataAdapter;

        private event EventHandler Inited;

        internal void Init(ControllerInfo controllerInfo, BeeDataAdapter viewData)
        {
            this.controllerInfo = controllerInfo;
            this.dataAdapter = viewData;

            if (Inited != null)
            {
                Inited(this, null);
            }
        }

        /// <summary>
        /// The ViewData.
        /// </summary>
        protected BeeDataAdapter ViewData
        {
            get
            {
                return dataAdapter;
            }
        }

        /// <summary>
        /// Gets the name of the controller.
        /// </summary>
        public string ControllerName
        {
            get
            {
                return controllerInfo.Name;
            }
        }
    }

    /// <summary>
    /// the base class of MVC Controller.
    /// </summary>
    public abstract class ControllerBase : BeeControllerBase
    {
        #region Fields

        private string currentActionName;

        private HttpContext httpContext;

        #endregion
        

        protected HttpContext HttpContext
        {
            get
            {
                return httpContext;
            }
        }

        /// <summary>
        /// The current ssesion.
        /// </summary>
        protected HttpSessionState Session
        {
            get
            {
                return httpContext.Session;
            }
        }

        internal virtual BeeAutoModelInfo AutoModelInfo()
        {
            return null;
        }

        internal void Init(HttpContext httpContext, ControllerInfo controllerInfo, BeeDataAdapter viewData, string actionName)
        {
            this.httpContext = httpContext;

            Init(controllerInfo, viewData);
            this.currentActionName = actionName;
        }

        /// <summary>
        /// Init the parameter of the pagination. include pagenum, pagesize, recordcount, orderfield, orderdirection.
        /// </summary>
        /// <param name="dataAdapter">the data.</param>
        protected virtual void InitPagePara(BeeDataAdapter dataAdapter)
        {
            ViewData.TryGetValue<int>("pagenum", 1, true);
            ViewData.TryGetValue<int>("pagesize", 20, true);
            ViewData.TryGetValue<int>("recordcount", 0, true);
            ViewData.TryGetValue<string>("orderField", "id", true);
            ViewData.TryGetValue<string>("orderDirection", "desc", true);
        }

        protected void SetReadonly()
        {
            ViewData[Constants.BeeReadonly] = true;
        }

        /// <summary>
        /// Basic query for pagination.
        /// </summary>
        /// <param name="tableName">the table name.</param>
        /// <param name="selectClause">the select clauses.</param>
        /// <param name="sqlCriteria">the condition.</param>
        /// <returns>the result.</returns>
        protected virtual DataTable InnerQuery(string tableName, string selectClause, SqlCriteria sqlCriteria)
        {
            return InnerQuery(tableName, selectClause, ViewData, sqlCriteria);
        }

        /// <summary>
        /// Basic query for pagination.
        /// </summary>
        /// <param name="tableName">the table name.</param>
        /// <param name="selectClause">the select clauses.</param>
        /// <param name="dataAdapter">the data.</param>
        /// <param name="sqlCriteria">the condition.</param>
        /// <param name="recordCount">the record count of the result.</param>
        /// <returns>the result.</returns>
        protected virtual DataTable InnerQuery(string tableName, string selectClause, BeeDataAdapter dataAdapter, 
            SqlCriteria sqlCriteria)
        {
            int pageNum = dataAdapter.TryGetValue<int>("pagenum", 1);
            int pageSize = dataAdapter.TryGetValue<int>("pagesize", 20);
            int recordCount = dataAdapter.TryGetValue<int>("recordcount", 0);

            string orderField = dataAdapter.TryGetValue<string>("orderField", "Id");
            string orderDirection = dataAdapter.TryGetValue<string>("orderDirection", "desc");

            DataTable result = DbSession.Current.Query(tableName, selectClause, sqlCriteria,
                    "{0} {1}".FormatWith(orderField, orderDirection), pageNum, pageSize, ref recordCount);

            dataAdapter["recordcount"] = recordCount;

            return result;

        }

        /// <summary>
        /// Gets the instance of the DbSession.
        /// </summary>
        /// <returns>the instance of DbSession.</returns>
        protected DbSession GetDbSession()
        {
            return GetDbSession(false);
        }

        /// <summary>
        /// Gets the instance of the DbSession.
        /// </summary>
        /// <param name="useTransaction">the flag indicate to use the transaction or not.</param>
        /// <returns></returns>
        protected virtual DbSession GetDbSession(bool useTransaction)
        {
            return DbSession.Current;
        }

        /// <summary>
        /// Gets the conditions via the attribute of the modeltype and the data from page.
        /// </summary>
        /// <param name="modelType">the model type.</param>
        /// <param name="dataAdapter">the data from page.</param>
        /// <returns>the condition.</returns>
        protected virtual SqlCriteria GetQueryCondition(Type modelType, BeeDataAdapter dataAdapter)
        {
            IEntityProxy entityProxy = EntityProxyManager.Instance.GetEntityProxyFromType(modelType);
            SqlCriteria result = new SqlCriteria();
            dataAdapter = new BeeDataAdapter(dataAdapter);
            dataAdapter.RemoveEmptyOrNull();

            ModelAttribute modelAttribute = entityProxy.GetCustomerAttribute<ModelAttribute>();
            foreach (PropertySchema propertySchema in entityProxy.GetPropertyList())
            {
                string propertyName = propertySchema.Name;
                ModelPropertyAttribute modelPropertyAttribute
                        = propertySchema.GetCustomerAttribute<ModelPropertyAttribute>();
                if (modelPropertyAttribute != null)
                {
                    if (dataAdapter.ContainsKey(propertyName))
                    {
                        if (modelPropertyAttribute.Queryable)
                        {
                            object conditionValue = dataAdapter[propertyName];

                            if (propertySchema.PropertyType == typeof(string))
                            {
                                conditionValue = HttpUtility.HtmlDecode(conditionValue.ToString());
                            }

                            if (modelPropertyAttribute.QueryType == ModelQueryType.Equal &&
                                propertySchema.PropertyType == typeof(bool))
                            {
                                bool value = false;
                                bool.TryParse(conditionValue.ToString(), out value);
                                if (value)
                                {
                                    conditionValue = 1;
                                }
                                else
                                {
                                    conditionValue = 0;
                                }
                            }

                            if (propertySchema.PropertyType == typeof(DateTime))
                            {
                                DateTime propertyValue = dataAdapter.TryGetValue<DateTime>(propertyName, DateTime.MinValue);
                                if (propertyValue != DateTime.MinValue)
                                {
                                    conditionValue = propertyValue;
                                }
                                else
                                {
                                    // 若是时间，而又没有赋值， 则略过
                                    continue;
                                }
                            }

                            AddCondition(result, modelPropertyAttribute.QueryType, propertyName, conditionValue);
                        }
                    }
                    else
                    {
                        if (modelPropertyAttribute.Queryable
                            && modelPropertyAttribute.QueryType == ModelQueryType.Between)
                        {

                            if (dataAdapter.ContainsKey(propertyName + "begin"))
                            {
                                if (propertySchema.PropertyType == typeof(DateTime))
                                {
                                    DateTime beginTime = dataAdapter.TryGetValue<DateTime>(propertyName + "begin", DateTime.MinValue);
                                    result.GreaterThanOrEqual(propertyName, beginTime);
                                }
                                else
                                {
                                    result.GreaterThanOrEqual(propertyName, dataAdapter[propertyName + "begin"]);
                                }
                            }

                            if (dataAdapter.ContainsKey(propertyName + "end"))
                            {
                                DateTime endTime = dataAdapter.TryGetValue<DateTime>(propertyName + "end", DateTime.MinValue);
                                if (propertySchema.PropertyType == typeof(DateTime))
                                {
                                    if(endTime == endTime.Date)
                                    {
                                        endTime = endTime.AddDays(1);
                                    }

                                    //result.LessThan(propertyName, endTime.ToString("yyyy-MM-dd"));
                                    result.LessThan(propertyName, endTime);
                                }
                                else
                                {
                                    result.LessThanOrEqual(propertyName, dataAdapter[propertyName + "end"]);
                                }
                            }
                        }
                    }
                }
            }

            return result;
        }

        protected virtual string GetQuerySelectClause(Type modelType)
        {
            IEntityProxy entityProxy = EntityProxyManager.Instance.GetEntityProxyFromType(modelType);

            string tableName = OrmUtil.GetTableName(modelType);
            TableSchema tableSchema = DbSession.Current.GetTableSchema(tableName);

            StringBuilder selectClause = new StringBuilder();
            foreach (PropertySchema propertySchema in entityProxy.GetPropertyList())
            {
                string columnName = propertySchema.Name;
                ModelPropertyAttribute modelPropertyAttribute
                    = propertySchema.GetCustomerAttribute<ModelPropertyAttribute>();
                if (modelPropertyAttribute != null)
                {
                    if (!modelPropertyAttribute.Visible)
                    {
                        continue;
                    }
                }

                OrmColumnAttribute ormColumnAttribute = propertySchema.GetCustomerAttribute<OrmColumnAttribute>();
                if (ormColumnAttribute != null && !string.IsNullOrEmpty(ormColumnAttribute.DbColumnName))
                {
                    columnName = ormColumnAttribute.DbColumnName;
                }

                if (tableSchema != null && !tableSchema.ContainsColumn(columnName))
                {
                    continue;
                }

                selectClause.AppendFormat("{0},", columnName);
            }
            selectClause.Remove(selectClause.Length - 1, 1);

            return selectClause.ToString();
        }

        protected void QuickAddCondition(SqlCriteria sqlCriteria, ModelQueryType queryType, string name, BeeDataAdapter dataAdapter)
        {
            if (queryType == ModelQueryType.Between)
            {
                if (dataAdapter.ContainsKey(name + "begin"))
                {
                    sqlCriteria.GreaterThanOrEqual(name, dataAdapter[name + "begin"]);
                }

                if (dataAdapter.ContainsKey(name + "end"))
                {
                    sqlCriteria.GreaterThanOrEqual(name, dataAdapter[name + "end"]);
                }
            }
            else
            {
                if (dataAdapter.ContainsKey(name))
                {
                    AddCondition(sqlCriteria, queryType, name, dataAdapter[name]);
                }
            }
        }

        protected void AddCondition(SqlCriteria sqlCriteria, ModelQueryType queryType, string name, object value)
        {
            switch (queryType)
            {
                case ModelQueryType.Contains:
                    sqlCriteria.Contains(name, value.ToString());
                    break;
                case ModelQueryType.EndWith:
                    sqlCriteria.EndWith(name, value.ToString());
                    break;
                case ModelQueryType.Equal:
                    sqlCriteria.Equal(name, value);
                    break;
                case ModelQueryType.GreaterThan:
                    sqlCriteria.GreaterThan(name, value);
                    break;
                case ModelQueryType.GreaterThanOrEqual:
                    sqlCriteria.GreaterThanOrEqual(name, value);
                    break;
                case ModelQueryType.LessThan:
                    sqlCriteria.LessThan(name, value);
                    break;
                case ModelQueryType.LessThanOrEqual:
                    sqlCriteria.LessThanOrEqual(name, value);
                    break;
                case ModelQueryType.StartWith:
                    sqlCriteria.StartWith(name, value.ToString());
                    break;
                case ModelQueryType.In:
                    sqlCriteria.In(name, value.ToString());
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// Invoke the action before the action excuted.
        /// </summary>
        /// <param name="actionName">the action name.</param>
        /// <param name="dataAdapter">the data.</param>
        /// <returns></returns>
        protected internal virtual string OnBeforeAction(string actionName, BeeDataAdapter dataAdapter)
        {
            return null;
        }

        protected internal virtual string OnAfterAction(string actionName, BeeDataAdapter dataAdapter)
        {
            return null;
        }

        #region Action Helper Methods

        /// <summary>
        /// Provided to Json the action result.
        /// </summary>
        /// <param name="model">the model.</param>
        /// <returns>the json result.</returns>
        protected ActionResult Json(object model)
        {
            return new JsonResult(model);
        }

        /// <summary>
        /// Provided to redirect the page.
        /// </summary>
        /// <param name="url">the url to redirect.</param>
        /// <returns>the instance of the ActionResult</returns>
        protected ActionResult Redirect(string url)
        {
            return new RedirectResult(url);
        }

        /// <summary>
        /// Provided to redirect the page via actionName.
        /// </summary>
        /// <param name="actionName">the actionName to redirect.</param>
        /// <returns>the instance of the ActionResult</returns>
        protected ActionResult RedirectAction(string actionName)
        {
            return new RedirectResult(ControllerName, actionName);
        }

        /// <summary>
        /// Provided to redirect the page via controllerName, actionName.
        /// </summary>
        /// <param name="controllerName">the controller name.</param>
        /// <param name="actionName">the action name.</param>
        /// <returns>the instance of the ActionResult.</returns>
        protected ActionResult Redirect(string controllerName, string actionName)
        {
            return new RedirectResult(controllerName, actionName);
        }

        /// <summary>
        /// Provided to show the current action's view.
        /// </summary>
        /// <param name="model">the model.</param>
        /// <returns>the instance of the ActionResult.</returns>
        protected PageResult View(object model)
        {
            return new PageResult(ControllerName, currentActionName, model);
        }

        /// <summary>
        /// Provided to show the current action's view.
        /// </summary>
        /// <returns>the instance of the ActionResult.</returns>
        protected PageResult View()
        {
            return new PageResult(ControllerName, currentActionName);
        }

        /// <summary>
        /// Provided to show the view.
        /// </summary>
        /// <param name="viewName">the view's name.</param>
        /// <param name="model">the model.</param>
        /// <returns>the instance of the ActionResult.</returns>
        protected PageResult View(string viewName, object model)
        {
            return new PageResult(ControllerName, viewName, model);
        }

        /// <summary>
        /// Provided to show the action's view.
        /// </summary>
        /// <param name="viewName">the view's name.</param>
        /// <returns>the instance of the ActionResult.</returns>
        protected PageResult View(string viewName)
        {
            return new PageResult(ControllerName, viewName);
        }

        /// <summary>
        /// Provided to show the action's view.
        /// </summary>
        /// <param name="controllerName">the controller's name.</param>
        /// <param name="viewName">the view's name.</param>
        /// <param name="model">the model.</param>
        /// <returns>the instance of the ActionResult.</returns>
        protected PageResult View(string controllerName, string viewName, object model)
        {
            return new PageResult(controllerName, viewName, model);
        }

        protected StreamResult OutputPage(string fileName, string controller, string action)
        {
            MemoryStream memoryStream = new MemoryStream();
            StreamWriter streamWriter = new StreamWriter(memoryStream);

            HttpWorkerRequest wr = new System.Web.Hosting.SimpleWorkerRequest("index.htm", string.Empty, streamWriter);
            HttpContext httpContext = new HttpContext(wr);
            httpContext.Response.ContentEncoding = Encoding.Default;

            MvcDispatcher.ExecuteAction(httpContext, controller, action, ViewData);

            httpContext.Response.Flush();
            streamWriter.Flush();

            StreamResult streamResult = new StreamResult(fileName, memoryStream);

            return streamResult;
        }

        protected string RenderHtml(string controller, string action, BeeDataAdapter dataAdapter)
        {
            string result = string.Empty;

            using (MemoryStream memoryStream = new MemoryStream())
            {
                using (StreamWriter streamWriter = new StreamWriter(memoryStream, Encoding.UTF8))
                {
                    HttpWorkerRequest wr = new System.Web.Hosting.SimpleWorkerRequest("index.htm", string.Empty, streamWriter);
                    HttpContext httpContext = new HttpContext(wr);
                    httpContext.Response.ContentEncoding = Encoding.Default;

                    MvcDispatcher.ExecuteAction(httpContext, controller, action, dataAdapter);

                    httpContext.Response.Flush();
                    streamWriter.Flush();

                    memoryStream.Position = 0;
                    //using (StreamReader reader = new StreamReader(memoryStream, Encoding.UTF8))
                    //{
                    //    result = reader.ReadToEnd();
                    //}
                    result = Encoding.UTF8.GetString(memoryStream.ToArray());
                }
            }

            return result;
        }

        protected void Invoke(string controllerName, string actionName, BeeDataAdapter dataAdapter)
        {
            MvcDispatcher.ExecuteAction(controllerName, actionName, dataAdapter);
        }

        #endregion
    }
}
