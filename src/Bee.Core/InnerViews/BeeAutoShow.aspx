
<%@ Page Language="C#" AutoEventWireup="false" Inherits="Bee.Web.BeePageView"  %>
<%@ Import Namespace="Bee.Web" %>
<%@ Import Namespace="Bee" %>
<%@ Import Namespace="System.Collections.Generic" %>

<div class="pageContent">
	<form method="post" action="<%=HtmlHelper.ForActionLink("save") %>" class="required-validate" id="content<%=PageId %>">
		<div class="pageFormContent" layoutH="56">
			<%=HtmlHelper.AutoDetailInfo %>
		</div>
		<div class="formBar">
			<ul>
				<li> <a class="button" href="javascript:" onclick="javascript:autoSave('content<%=PageId %>');"><span>保存</span> </a></li>
				<li> <a class="button close" href="javascript:"><span>取消</span> </a></li>
			</ul>
		</div>
	</form>
</div>
