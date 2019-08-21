
<%@ Page Language="C#" AutoEventWireup="false" Inherits="Bee.Web.BeePageView"  %>
<%@ Import Namespace="Bee.Web" %>
<%@ Import Namespace="Bee" %>
<%@ Import Namespace="System.Collections.Generic" %>

<div class="pageHeader">
    <form id="pageForm<%=PageId %>" action="<%=ControllerName %>/List.cspx" method="post">
    <input type='hidden' name='pageNum' value='<%=ViewData["pageNum"] %>' />
    <input type='hidden' name='pageSize' value='<%=ViewData["pageSize"] %>' />
    <input type='hidden' name='orderField' value='<%=ViewData["orderField"] %>' />
    <input type='hidden' name='orderDirection' value='<%=ViewData["orderDirection"] %>' />
    <input type='hidden' name='recordCount' value='<%=ViewData["recordCount"] %>' />
    <div class="searchBar">
        <ul class="searchContent">
            <% List<BeeDataAdapter> searchInfoList = ((BeeAutoModelInfo)ViewData["Bee_AutoModelInfo"]).SearchInfo;
               foreach (BeeDataAdapter dataAdapter in searchInfoList)
               {
                   string name = dataAdapter["name"] as string;
                %>
              <li>
                <label><%=dataAdapter["description"] %>：</label>
                <%if (((ModelQueryType)dataAdapter["querytype"]) == ModelQueryType.Between)
                  { %>
                <input type='text' style='width:60px' name='<%=name %>begin' value='<%=ViewData[name+"begin"] %>' /> - <input style='width:60px' type='text' name='<%=name %>end' value='<%=ViewData[name+"end"] %>' />
                <%}
                  else
                  { %>
                  <input type='text' name='<%=name %>' value='<%=ViewData[name] %>' />
                <%} %>
              </li>
                
                <%} %>
        </ul>
        <div class="subBar">
            <ul>
                <li>
                    <div>
                        <div>
                            <a class="button" href="javascript:" onclick="javascript:autoList('<%=PageId %>');"
                                rel=""><span>检索</span> </a>
                        </div>
                    </div>
                </li>
            </ul>
        </div>
    </div>
    </form>
</div>
<div class="pageContent">
    <div class="panelBar">
        <ul class="toolBar">
            <li><a class="add" href="demo_page4.html" target="navTab"><span>添加</span></a></li>
            <li><a title="确实要删除这些记录吗?" target="selectedTodo" rel="ids" href="demo/common/ajaxDone.html"
                class="delete"><span>批量删除默认方式</span></a></li>
            <li><a title="确实要删除这些记录吗?" target="selectedTodo" rel="ids" posttype="string" href="demo/common/ajaxDone.html"
                class="delete"><span>批量删除逗号分隔</span></a></li>
            <li><a class="edit" href="" target="navTab" warn="请选择一个用户"><span>修改</span></a></li>
            <li class="line">line</li>
            <li><a class="icon" href="demo/common/dwz-team.xls" target="dwzExport" targettype="navTab"
                title="实要导出这些记录吗?"><span>导出EXCEL</span></a></li>
        </ul>
    </div>
    <table id="table<%=PageId %>" class="table" width="1000" layouth="188">
        <thead>
            <tr>
                <th width='25'>
                <input type='checkbox' group='ids' class='checkboxCtrl'></th>

            <% List<BeeDataAdapter> headInfoList = ((BeeAutoModelInfo)ViewData["Bee_AutoModelInfo"]).HeaderInfo;

               foreach (BeeDataAdapter dataAdapter in headInfoList)
               {
                   string widthInfo = string.Empty;
                   string orderFieldInfo = string.Empty;
                   string orderInfo = string.Empty;
                   string alignInfo = string.Empty;
                   
                   string name = dataAdapter["name"] as string;
                   if(!string.IsNullOrEmpty(dataAdapter["width"] as string))
                   {
                       widthInfo = string.Format("width='{0}'", dataAdapter["width"]);
                   }
                   if(!string.IsNullOrEmpty(dataAdapter["orderfield"] as string))
                   {
                       widthInfo = string.Format("orderField='{0}'", dataAdapter["orderfield"]);
                   }
                   
                   if(string.Compare(ViewData["orderField"] as string, name, true) == 0)
                   {
                       orderInfo = string.Format("class='{0}'", ViewData["orderDirection"]);
                   }
                   
                   if(!string.IsNullOrEmpty(dataAdapter["align"] as string))
                   {
                       widthInfo = string.Format("align='{0}'", dataAdapter["align"]);
                   }


                   Response.Write(string.Format("<th {0} {1} {2} {3} >{4}</th>", widthInfo, orderFieldInfo,
                       orderInfo, alignInfo, dataAdapter["description"]));
               }
                %>
                
                <th width='70'>操作</th>
            </tr>
        </thead>
        <tbody>
            
        </tbody>
    </table>
    <div class='panelBar'>
        <div class='pages'>
            <span>显示</span>
            <select class='combox' name='numPerPage' onchange=""javascript:autoChangePageSize(this, '<%=PageId %>')"">
                <option value='20'>20</option>
                <option value='40'>40</option>
                <option value='80'>80</option>
            </select>
            <span>条，共<%=ViewData["recordcount"] %>条</span>
        </div>
        <div class='pagination' totalcount='<%=ViewData["recordCount"] %>' numperpage='<%=ViewData["pagesize"] %>' pagenumshown='10'
            currentpage='<%=ViewData["pagenum"] %>' click=""javascript:autoJumpTo('{0}', #pageNum#);"">
        </div>
    </div>
</div>
