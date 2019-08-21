<%@ Page Language="C#" AutoEventWireup="false" Inherits="Bee.Web.BeePageView" %>

<%@ Import Namespace="Bee.Web" %>
<%@ Import Namespace="Bee" %>
<%@ Import Namespace="System.Collections.Generic" %>
<% System.Data.DataTable dataTable = Model as System.Data.DataTable;  %>
<div class="pageHeader">
    <form id="pageForm<%=PageId %>" action="<%=HtmlHelper.ForActionLink("List") %>" method="post"
    class="required-validate alertMsg">
    <%=HtmlHelper.ForHidden("pageNum")%>
    <%=HtmlHelper.ForHidden("pageSize")%>
    <%=HtmlHelper.ForHidden("orderField")%>
    <%=HtmlHelper.ForHidden("orderDirection")%>
    <%=HtmlHelper.ForHidden("recordCount")%>
    <div class="searchBar">
        <ul class="searchContent">
            <%=HtmlHelper.AutoSearchInfo%>
        </ul>
    </div>
    </form>
</div>
<div class="pageContent">
    <div class="panelBar">
        <ul class="toolBar">
            <li><a class="add" href="<%=HtmlHelper.ForActionLink("show") %>?id=-1" target="dialog"
                mask="true" title="创建" rel="CD<%=PageId %>"><span>添加</span></a></li>
        </ul>
        <ul class="searchBar">
            <li><a class="button" href="javascript:" onclick="javascript:autoList();"><span>检索</span>
            </a></li>
        </ul>
    </div>
    <table id="table<%=PageId %>" class="table" width="1000" layouth="112">
        <thead>
            <th width='25'>
                <input type='checkbox' group='ids' class='checkboxCtrl'>
            </th>
            <%= HtmlHelper.AutoHeaderInfo%>
            <th width='70'>
                操作
            </th>
        </thead>
        <tbody>
            <%
                if (dataTable != null)
                {
                    foreach (System.Data.DataRow row in dataTable.Rows)
                    {%>
            <tr>
                <td>
                    <input name='ids' value='<%=row[0] %>' type='checkbox'>
                </td>
                <%
                    foreach (System.Data.DataColumn column in dataTable.Columns)
                    {
                %>
                <td>
                    <%=HtmlHelper.AutoFormatRowItem(row, column.ColumnName)%>
                </td>
                <%} %>
                <td>
                    <a title='删除' target='ajaxTodo' href='<%=HtmlHelper.ForActionLink("delete") %>?id=<%=row[0].ToString() %>'
                        class='btnDel'>删除</a> <a title='编辑' target="dialog" mask="true" rel="CD<%=PageId %>"
                            href='<%=HtmlHelper.ForActionLink("show") %>?id=<%=row[0].ToString() %>' class='btnEdit'>
                            编辑</a>
                </td>
            </tr>
            <%}
            } %>
        </tbody>
    </table>
    <div class='panelBar'>
        <div class='pages'>
            <span>显示</span>
            <select class='combox' name='numPerPage' onchange="javascript:autoChangePageSize(this);">
                <%=HtmlHelper.ForPageSizeSelect() %>
            </select>
            <span>条，共<%=ViewData["recordcount"] %>条</span>
        </div>
        <div class='pagination' totalcount='<%=ViewData["recordCount"] %>' numperpage='<%=ViewData["pagesize"] %>'
            pagenumshown='10' currentpage='<%=ViewData["pagenum"] %>' click="javascript:autoJumpTo(#pageNum#);">
        </div>
    </div>
</div>
