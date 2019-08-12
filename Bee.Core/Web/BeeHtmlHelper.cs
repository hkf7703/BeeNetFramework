using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using Bee.Util;
using System.Web;
using Bee.Core;
using Bee.Data;
using System.Text.RegularExpressions;

namespace Bee.Web
{
    /// <summary>
    /// 页面Html辅助类
    /// </summary>
    public class BeeHtmlHelper
    {
        private BeePageView owner;

        private static readonly Regex ExAttributeRegex = new Regex("(?<name>.*?)=(?<value>.*)");

        public BeeHtmlHelper(BeePageView view)
        {
            this.owner = view;
        }

        public void RenderAction(string controller, string action)
        {
            MvcDispatcher.ExecuteAction(controller, action, null);
        }

        public void RenderAction(string controller, string action, BeeDataAdapter dataAdapter)
        {
            MvcDispatcher.ExecuteAction(controller, action, dataAdapter);
        }

        public void RenderAction(string controller, string action, params string[] data)
        {
            BeeDataAdapter dataAdapter = new BeeDataAdapter();
            foreach (string item in data)
            {
                Match match = ExAttributeRegex.Match(item);
                if (match.Success)
                {
                    dataAdapter[match.Groups["name"].Value] = match.Groups["value"].Value;
                }
            }

            MvcDispatcher.ExecuteAction(controller, action, dataAdapter);
        }

        public BeePageView Owner
        {
            get
            {
                return this.owner;
            }
        }

        public string SwfKey
        {
            get
            {
                string sessionTokenKey = ConfigUtil.GetAppSettingValue<string>("SessionTokenKey");
                if (string.IsNullOrEmpty(sessionTokenKey))
                {
                    sessionTokenKey = "bee_sess";
                }
                string sessionId = HttpContextUtil.CurrentHttpContext.Session.SessionID.ToString();

                return SecurityUtil.EncryptS(sessionId, sessionTokenKey);
            }
        }

        /// <summary>
        /// 根据匹配值查找映射中的映射值。
        /// </summary>
        /// <param name="mappingName">映射名</param>
        /// <param name="keyValue">匹配值</param>
        /// <returns>映射值</returns>
        public string ForDataMapping(string mappingName, object keyValue)
        {
            return ForDataMapping(mappingName, keyValue, null, null);
        }

        /// <summary>
        /// 根据匹配值查找映射中的映射值。
        /// </summary>
        /// <param name="mappingName">映射名</param>
        /// <param name="keyValue">匹配值</param>
        /// <param name="propertyName">映射值列名或属性名</param>
        /// <returns>映射值</returns>
        public string ForDataMapping(string mappingName, object keyValue, string propertyName)
        {
            return ForDataMapping(mappingName, keyValue, null, propertyName);
        }

        public string ForSortOrder(string fieldName)
        {
            string result = string.Empty;
            string orderField = owner.ViewData.TryGetValue<string>("orderField", string.Empty);

            if (string.Compare(orderField, fieldName, true) == 0)
            {
                string orderDirection = owner.ViewData.TryGetValue<string>("orderDirection", string.Empty);

                result = "orderField='{0}' class='{1}'".FormatWith(fieldName, orderDirection);
            }
            else
            {
                result = "orderField='{0}'".FormatWith(fieldName);
            }

            return result;
        }

        public string ForDataMapping(string mappingName, object keyValue, string keyName, string propertyName)
        {
            if (keyValue != null)
            {
                if (owner.ViewData.ContainsKey(keyValue.ToString()))
                {
                    keyValue = owner.ViewData[keyValue.ToString()];
                }
            }

            if (keyValue != null && keyValue != DBNull.Value)
            {
                string keyValueItem = keyValue.ToString();
                // 若是Enum， 转换成数字匹配
                if (keyValue is Enum)
                {
                    keyValueItem = ((Enum)keyValue).ToString("D");
                }

                return DataMapping.Instance.Mapping(mappingName, keyValueItem, keyName, propertyName);
            }
            else
            {
                return string.Empty;
            }
        }

        public HtmlBuilder ForHidden(string name, params string[] attrs)
        {
            HtmlBuilder htmlBuilder = HtmlBuilder.New.tag("input").attr("type", "hidden").attr("name", name);

            AddAttr(htmlBuilder, attrs);

            if (owner.ViewData[name] != null)
            {
                htmlBuilder.attr("value", owner.ViewData[name]);
            }

            return htmlBuilder.end;
        }

        private void AddAttr(HtmlBuilder htmlBuilder, params string[] attrs)
        {
            foreach (string item in attrs)
            {
                Match match = ExAttributeRegex.Match(item);
                if (match.Success)
                {
                    string name = match.Groups["name"].Value;
                    string value = match.Groups["value"].Value;

                    htmlBuilder.attr(name, value);
                }
            }
        }

        /// <summary>
        /// 辅助创建文本框Html。
        /// </summary>
        /// <param name="name">名称</param>
        /// <param name="attrs">参数以=分割。 
        /// 如：ForTextBox(name, "class=required,num")
        /// </param>
        /// <returns></returns>
        public HtmlBuilder ForTextBox(string name, params string[] attrs)
        {
            HtmlBuilder htmlBuilder = HtmlBuilder.New.tag("input").attr("type", "text").attr("name", name);

            AddAttr(htmlBuilder, attrs);

            if (owner.ViewData[name] != null)
            {
                htmlBuilder.attr("value", owner.ViewData.Format(name));
            }
            return htmlBuilder.end;
        }

        public HtmlBuilder ForDatetimeTextBox(string name, string format, params string[] attrs)
        {
            HtmlBuilder htmlBuilder = HtmlBuilder.New.tag("input").attr("type", "text").attr("name", name);

            AddAttr(htmlBuilder, attrs);

            if (owner.ViewData[name] != null)
            {
                DateTime value = owner.ViewData.TryGetValue<DateTime>(name, DateTime.MinValue);
                if (string.IsNullOrEmpty(format))
                {
                    htmlBuilder.attr("value", owner.ViewData.Format(name));
                }
                else
                {
                    htmlBuilder.attr("value", value.ToString(format));
                }
            }
            return htmlBuilder.end;
        }

        public HtmlBuilder ForCheckBox(string name, string value, params string[] attrs)
        {
            HtmlBuilder htmlBuilder = HtmlBuilder.New.tag("input")
                .attr("type", "checkbox").attr("name", name)
                .attr("value", value);

            AddAttr(htmlBuilder, attrs);

            string tempValue = owner.ViewData.TryGetValue<string>(name, string.Empty);
            if (string.Compare(tempValue, value, true) == 0)
            {
                htmlBuilder.attr("checked", "checked");
            }
            return htmlBuilder.end;
        }

        public HtmlBuilder ForRadioBox(string name, string value, params string[] attrs)
        {
            HtmlBuilder htmlBuilder = HtmlBuilder.New.tag("input")
                .attr("type", "radio").attr("name", name)
                .attr("value", value);

            AddAttr(htmlBuilder, attrs);

            string tempValue = owner.ViewData.TryGetValue<string>(name, string.Empty);
            if (string.Compare(tempValue, value, true) == 0)
            {
                htmlBuilder.attr("checked", "checked");
            }
            return htmlBuilder.end;
        }

        public string ForSelect(string name, string mappingName)
        {
            return ForSelect(name, mappingName, true);
        }

        public string ForSelect(string name, string mappingName, SqlCriteria sqlCriteria)
        {
            return ForSelect(name, mappingName, sqlCriteria, true);
        }

        public string ForSelect(string name, string mappingName, string valuePropertyName, SqlCriteria sqlCriteria)
        {
            return ForSelect(name, mappingName, valuePropertyName, sqlCriteria, true);
        }

        public string ForSelect(string name, string mappingName, SqlCriteria sqlCriteria, bool appendAll)
        {
            return ForSelect(name, mappingName, "<option value='@id'>@name</option>", sqlCriteria, appendAll);
        }

        public string ForSelect(string name, string mappingName, string valuePropertyName)
        {
            return ForSelect(name, mappingName, valuePropertyName, null, true);
        }

        public string ForSelect(string name, string mappingName, bool appendAll)
        {
            return ForSelect(name, mappingName, "<option value='@id'>@name</option>", null, appendAll);
        }

        public string ForSelect(string name, string mappingName, string valuePropertyName, bool appendAll)
        {
            return ForSelect(name, mappingName, valuePropertyName, null, appendAll);
        }

        public string ForSelect(string name, string mappingName, string valuePropertyName, SqlCriteria sqlCriteria, bool appendAll)
        {
            DataTable data = DataMapping.Instance.GetMapping(mappingName) as DataTable;

            return ForSelect(name, data, valuePropertyName, sqlCriteria, appendAll);
        }

        public string ForSelect(string name, DataTable data, string valuePropertyName, SqlCriteria sqlCriteria, bool appendAll)
        {
            StringBuilder builder = new StringBuilder();
            string value = string.Empty;
            if (owner.ViewData[name] != null)
            {
                value = "svalue='{0}'".FormatWith(owner.ViewData[name]);
            }
            builder.AppendFormat("<select name='{0}' class='combox {2}' {1}>", name, value, owner.ViewData.ContainsKey(Constants.BeeReadonly) ? "readonly" : "");

            if (appendAll)
            {
                builder.Append("<option value=''>请选择</option>");
            }

            if (data != null && data.Rows.Count > 0)
            {
                data = DataUtil.Query(data, sqlCriteria);

                foreach (DataRow item in data.Rows)
                {
                    builder.Append(valuePropertyName.RazorFormat(item));
                }
            }

            builder.Append("</select>");

            return builder.ToString();
        }


        //public string ForSelectOption(BeeDataAdapter dataAdapter, object selectedValue)
        //{
        //    if (dataAdapter == null) return string.Empty;

        //    string selectedKey = null;
        //    if (selectedValue != null)
        //    {
        //        selectedKey = selectedValue.ToString();
        //    }

        //    StringBuilder builder = new StringBuilder();
        //    bool selectedFlag = false;
        //    int index = 0;
        //    foreach (string key in dataAdapter.Keys)
        //    {
        //        selectedFlag = false;
        //        if (string.IsNullOrEmpty(selectedKey))
        //        {
        //            if (index == 0)
        //            {
        //                selectedFlag = true;
        //            }
        //        }
        //        else
        //        {
        //            selectedFlag = string.Compare(key, selectedKey, true) == 0;
        //        }

        //        if (selectedFlag)
        //        {
        //            builder.AppendFormat("<option value='{0}' selected>{1}</option>\r\n", HttpUtility.HtmlEncode(key), dataAdapter[key]);
        //        }
        //        else
        //        {
        //            builder.AppendFormat("<option value='{0}'>{1}</option>\r\n", HttpUtility.HtmlEncode(key), dataAdapter[key]);
        //        }
        //        index++;
        //    }

        //    return builder.ToString();
        //}

        public string ForPageSizeSelect()
        {
            StringBuilder stringBuilder = new StringBuilder();
            int pageSize = owner.ViewData.TryGetValue<int>("pagesize", 20);

            List<int> pageSizeList = new List<int>();
            pageSizeList.Add(20);
            pageSizeList.Add(40);
            pageSizeList.Add(80);
            if (!pageSizeList.Contains(pageSize))
            {
                if (pageSize > 80)
                {
                    pageSizeList.Add(pageSize);
                }
                else
                {
                    for (int i = 0; i < pageSizeList.Count; i++)
                    {
                        if (pageSizeList[i] > pageSize)
                        {
                            pageSizeList.Insert(i, pageSize);
                            break;
                        }
                    }
                }
            }
            foreach (int item in pageSizeList)
            {
                stringBuilder.Append(string.Format(@"
                <option value='{0}' {1}>{0}</option>", item, item == pageSize ? "selected" : ""));
            }

            return stringBuilder.ToString();
        }

        public string ForPageList(int pageIndex, int pageSize, int recordCount, string hrefFormat)
        {
            StringBuilder builder = new StringBuilder();

            int showPageCount = 10;

            int pageCount = recordCount / pageSize + ((recordCount % pageSize) == 0 ? 0 : 1);

            pageIndex = Math.Min(pageIndex, pageCount);

            int startId = (pageIndex - (showPageCount / 2)) <= 0 ? 1 : pageIndex - showPageCount / 2;

            for (int i = startId, pageNum = 1; i <= pageCount && pageNum <= showPageCount; i++)
            {
                if (i == pageIndex)
                {
                    builder.Append(i);
                }
                else
                {
                    builder.AppendFormat("<a href='" + hrefFormat + "' >{0}</a>", i);
                }

                pageNum++;
            }


            string headPage = "disabled='disabled'";
            string tailPage = "disabled='disabled'";
            string prevPage = "disabled='disabled'";
            string nextPage = "disabled='disabled'";
            if (pageIndex != 1)
            {
                headPage = string.Format("href='" + hrefFormat + "'", 1);
                prevPage = string.Format("href='" + hrefFormat + "'", pageIndex - 1);
            }

            if (pageIndex < pageCount)
            {
                tailPage = string.Format("href='" + hrefFormat + "'", pageCount);
                nextPage = string.Format("href='" + hrefFormat + "'", pageIndex + 1);
            }

            return string.Format(@"<div class='manu'><a {0}>首页</a>  <a {1}>上一页</a>{2}
                <a {3}>下一页</a>  <a {4}>尾页</a></div>
            ", headPage, prevPage, builder.ToString(), nextPage, tailPage);
        }

        public string ForActionLink()
        {
            return ForActionLink(owner.ControllerName, owner.ActionName);
        }

        public string ForActionLink(string actionName)
        {
            return ForActionLink(owner.ControllerName, actionName);
        }

        public string ForActionLink(string controllerName, string actionName)
        {
            return @"/{0}/{1}.bee".FormatWith(controllerName, actionName);
        }

        public string AutoHeaderInfo
        {
            get
            {
                StringBuilder builder = new StringBuilder();

                BeeAutoModelInfo autoModelInfo = owner.ViewData[Constants.BeeAutoModelInfo] as BeeAutoModelInfo;
                if (autoModelInfo != null)
                {
                    List<BeeDataAdapter> headInfoList = autoModelInfo.HeaderInfo;

                    foreach (BeeDataAdapter dataAdapter in headInfoList)
                    {
                        string widthInfo = string.Empty;
                        string orderFieldInfo = string.Empty;
                        string orderInfo = string.Empty;
                        string alignInfo = string.Empty;

                        string name = dataAdapter["name"] as string;
                        if (!string.IsNullOrEmpty(dataAdapter["width"] as string))
                        {
                            widthInfo = string.Format("width='{0}'", dataAdapter["width"]);
                        }
                        if (!string.IsNullOrEmpty(dataAdapter["orderfield"] as string))
                        {
                            orderFieldInfo = string.Format("orderField='{0}'", dataAdapter["orderfield"]);
                        }

                        if (string.Compare(owner.ViewData["orderField"] as string, name, true) == 0)
                        {
                            orderInfo = string.Format("class='{0}'", owner.ViewData["orderDirection"]);
                        }

                        if (!string.IsNullOrEmpty(dataAdapter["align"] as string))
                        {
                            alignInfo = string.Format("align='{0}'", dataAdapter["align"]);
                        }


                        builder.AppendFormat("<th {0} {1} {2} {3} >{4}</th>", widthInfo, orderFieldInfo,
                            orderInfo, alignInfo, dataAdapter["description"]);
                    }
                }

                return builder.ToString();
            }
        }

        public string AutoSearchInfo
        {
            get
            {
                StringBuilder builder = new StringBuilder();
                BeeAutoModelInfo autoModelInfo = owner.ViewData[Constants.BeeAutoModelInfo] as BeeAutoModelInfo;
                if (autoModelInfo != null)
                {
                    List<BeeDataAdapter> searchInfoList = autoModelInfo.SearchInfo;
                    foreach (BeeDataAdapter dataAdapter in searchInfoList)
                    {
                        string name = dataAdapter["name"] as string;
                        Type propertyType = dataAdapter["type"] as Type;

                        builder.AppendFormat(@"
               <li>
                <label>{0}：</label>", dataAdapter["description"]);
                        if (((ModelQueryType)dataAdapter["querytype"]) == ModelQueryType.Between)
                        {
                            if (propertyType == typeof(DateTime))
                            {
                                builder.AppendFormat(@"<input type='text' style='width:70px' name='{0}begin' value='{1}' class='date'/> - <input style='width:70px' type='text' name='{0}end' value='{2}' class='date'/>",
                                    name, owner.ViewData[name + "begin"], owner.ViewData[name + "end"]);
                            }
                            else
                            {
                                builder.AppendFormat(@"<input type='text' style='width:70px' name='{0}begin' value='{1}' class='number'/> - <input style='width:70px' type='text' name='{0}end' value='{2}' class='number'/>",
                                    name, owner.ViewData[name + "begin"], owner.ViewData[name + "end"]);
                            }
                        }
                        //else if (propertyType == typeof(bool))
                        //{
                        //    string checkValue = string.Empty;
                        //    if (owner.ViewData.TryGetValue<bool>(name, false))
                        //    {
                        //        checkValue = "checked";
                        //    }

                        //    builder.AppendFormat(@"<input type='checkbox' name='{0}' value='true' {1}/>",
                        //            name, checkValue);
                        //}
                        else
                        {
                            string mappingName = dataAdapter["mappingname"] as string;
                            if (!string.IsNullOrEmpty(mappingName))
                            {
                                builder.Append(ForSelect(name, mappingName, true));
                            }
                            else
                            {
                                builder.AppendFormat(@"<input type='text' name='{0}' value='{1}'/>",
                                        name, owner.ViewData[name]);
                            }
                        }

                        builder.Append(@"
                </li>");
                    }
                }

                return builder.ToString();
            }
        }

        public string AutoDetailInfo
        {
            get
            {
                bool newEntityFlag = false;

                StringBuilder builder = new StringBuilder();
                BeeAutoModelInfo autoModelInfo = owner.ViewData[Constants.BeeAutoModelInfo] as BeeAutoModelInfo;

                BeeDataAdapter itemValue = owner.ViewData;

                newEntityFlag = itemValue.TryGetValue<int>("id", int.MinValue) < 0;

                if (autoModelInfo != null)
                {
                    List<BeeDataAdapter> searchInfoList = autoModelInfo.DetailInfo;
                    foreach (BeeDataAdapter dataAdapter in searchInfoList)
                    {
                        string description = dataAdapter["description"] as string;
                        string name = dataAdapter["name"] as string;
                        string mappingName = dataAdapter["mappingname"] as string;

                        bool showonlyFlag = dataAdapter.TryGetValue<bool>("showonly", false);

                        if (newEntityFlag && showonlyFlag)
                        {
                            continue;
                        }

                        if (!dataAdapter.TryGetValue<bool>("visible", true))
                        {
                            continue;
                        }

                        if (string.IsNullOrEmpty(mappingName))
                        {
                            string readonlyInfo = string.Empty;

                            bool readonlyFlag = (dataAdapter.TryGetValue<bool>("readonly", false) && !newEntityFlag);

                            string fieldInfo = string.Empty;
                            string dateInfo = string.Empty;
                            string inputValue = string.Empty;
                            if (dataAdapter.TryGetValue<bool>("date", false))
                            {
                                dateInfo = "class='date' ";

                                DateTime dateTimeValue = itemValue.TryGetValue<DateTime>(name, DateTime.MinValue);

                                if (dateTimeValue != DateTime.MinValue)
                                {
                                    inputValue = dateTimeValue.ToString(Constants.DateTimeFormat);
                                }
                            }
                            else
                            {
                                inputValue = itemValue.Format(name);
                            }

                            if (readonlyFlag)
                            {
                                //readonlyInfo = "readonly = 'readonly'";
                                fieldInfo = "<input type='hidden' name='{0}' value='{1}'/>{1}".FormatWith(name, inputValue);
                            }
                            else
                            {
                                fieldInfo = "<input name='{0}' type='text' size='30' value='{1}' {2} />"
                                    .FormatWith(name, inputValue, dateInfo);
                            }

                            builder.AppendFormat(@"
            <dl>
				<dt>{0}：</dt>
                <dd>{1}</dd>
			</dl>", description, fieldInfo);
                        }
                        else
                        {
                            string fieldInfo = string.Empty;
                            if (dataAdapter.TryGetValue<bool>("readonly", false) && !newEntityFlag)
                            {
                                fieldInfo = "<input type='hidden' name='{0}' value='{1}'/>{2}".FormatWith(name, itemValue.Format(name),
                                    ForDataMapping(mappingName, itemValue.Format(name)));
                            }
                            else
                            {
                                fieldInfo = ForSelect(name, mappingName, false);
                            }

                            builder.AppendFormat(@"
            <dl>
				<dt>{0}：</dt>
                <dd>{1}</dd>
			</dl>", description, fieldInfo);
                        }
                    }
                }

                return builder.ToString();
            }
        }

        public string AutoFormatRowItem(DataRow item, string columnName)
        {
            string result = string.Empty;

            BeeAutoModelInfo autoModelInfo = owner.ViewData[Constants.BeeAutoModelInfo] as BeeAutoModelInfo;

            if (autoModelInfo != null)
            {
                Dictionary<string, string> dataMappingInfo = autoModelInfo.DataMappingInfo;
                if (dataMappingInfo != null && dataMappingInfo.ContainsKey(columnName))
                {
                    result = ForDataMapping(dataMappingInfo[columnName], item[columnName]);
                }
                else
                {
                    result = item.Format(columnName);
                }
            }
            else
            {
                result = item.Format(columnName);
            }


            return result;
        }

        public string ForSingleTree(DataTable dataTable, string nameColumn, string valueColumn, string sort)
        {
            return ForSingleTree(dataTable, nameColumn, valueColumn, sort, "<li><a tvalue={1}>{0}</a></li>");
        }

        public string ForSingleTree(DataTable dataTable, string nameColumn, string valueColumn, string sort, string contentFormat)
        {
            StringBuilder builder = new StringBuilder();

            if (dataTable != null
                && dataTable.Columns.Contains(nameColumn)
                && dataTable.Columns.Contains(valueColumn))
            {
                DataRow[] rows = dataTable.Select("", sort);

                foreach (DataRow item in rows)
                {
                    builder.AppendFormat(contentFormat, item[nameColumn], item[valueColumn]);
                }
            }

            return builder.ToString();
        }

        public string ForTree(DataTable dataTable, string parentColumn,
            string nameColumn, string valueColumn, string sort)
        {
            return ForTree(dataTable, parentColumn, nameColumn, valueColumn, sort, 0, "<ul>", "</ul>", "<li><a tvalue={1}>{0}</a>", "</li>");
        }

        public string ForTree(DataTable dataTable, string parentColumn,
            string nameColumn, string valueColumn, string sort, object defaultParentValue
            , string parentBeginTag, string parentEndTag, string contentBeginTag, string contentEndTag)
        {
            StringBuilder builder = new StringBuilder();

            if (dataTable != null && dataTable.Columns.Contains(parentColumn)
                && dataTable.Columns.Contains(nameColumn)
                && dataTable.Columns.Contains(valueColumn))
            {
                GenerateTree(dataTable, parentColumn, nameColumn, valueColumn, valueColumn, defaultParentValue, sort,
                    defaultParentValue, parentBeginTag,
                        parentEndTag, contentBeginTag, contentBeginTag, contentEndTag, builder);
            }

            return builder.ToString();
        }

        public string InnerForLeftMenu(DataTable dataTable, int parentValue)
        {
            StringBuilder builder = new StringBuilder();

            string parentColumn = "parentid";
            string nameColumn = "title";
            string valueColumn = "id";
            string realValueColumn = "res";
            string sort = "dispindex asc";


            if (dataTable != null && dataTable.Columns.Contains(parentColumn)
                && dataTable.Columns.Contains(nameColumn)
                && dataTable.Columns.Contains(valueColumn)
                && dataTable.Columns.Contains(realValueColumn))
            {
                GenerateTree(dataTable, parentColumn, nameColumn, realValueColumn, valueColumn, parentValue, sort,
                    parentValue, "<ul>",
                        "</ul>", "<li><a href='{1}' target='navTab' rel='{2}' {3}>{0}</a>", "<li><a>{0}</a>", "</li>", builder);
            }

            return builder.ToString();
        }

        private void GenerateTree(DataTable src, string parentColumn, string nameColumn, string realValueColumn,
            string valueColumn, object parentId, string sort, object defaultParentId,
            string parentBeginTag, string parentEndTag, string contentBeginTag, string contentFolderBeginTag,
            string contentEndTag, StringBuilder builder)
        {
            DataRow[] rows = src.Select(string.Format("{0}='{1}'", parentColumn, parentId), sort);

            if (parentId.ToString() != defaultParentId.ToString() && rows.Length != 0)
            {
                builder.Append(parentBeginTag);
                builder.AppendLine();
            }

            foreach (DataRow item in rows)
            {
                if (item[realValueColumn] != null && !string.IsNullOrEmpty(item[realValueColumn].ToString().Trim()))
                {
                    string href = item[realValueColumn].ToString().ToLower();
                    string externalFlag = href.StartsWith("http") ? "external = 'true'" : string.Empty;
                    builder.AppendFormat(contentBeginTag, item[nameColumn], item[realValueColumn], "rel" + item[valueColumn]
                        , externalFlag);
                }
                else
                {
                    builder.AppendFormat(contentFolderBeginTag, item[nameColumn], item[realValueColumn]);
                }
                builder.AppendLine();

                GenerateTree(src, parentColumn, nameColumn, realValueColumn, valueColumn, item[valueColumn], sort, defaultParentId, parentBeginTag,
                    parentEndTag, contentBeginTag, contentFolderBeginTag, contentEndTag, builder);

                builder.Append(contentEndTag);
                builder.AppendLine();
            }

            if (parentId.ToString() != defaultParentId.ToString() && rows.Length != 0)
            {
                builder.Append(parentEndTag);
                builder.AppendLine();
            }
        }

    }
}
