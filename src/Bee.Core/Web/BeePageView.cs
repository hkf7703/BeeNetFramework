using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.UI;

namespace Bee.Web
{
    public class BeeMasterPage : MasterPage
    {
        private BeeHtmlHelper htmlHelper;

        private string pageId;
        protected BeeDataAdapter dataAdapter;

        new internal void Init(string pageId, BeeDataAdapter dataAdapter, BeeHtmlHelper htmlHelper)
        {
            this.pageId = pageId;
            this.dataAdapter = dataAdapter;
            this.htmlHelper = htmlHelper;
        }

        public BeeHtmlHelper HtmlHelper
        {
            get
            {
                return htmlHelper;
            }
        }

        public BeeDataAdapter ViewData
        {
            get
            {
                return dataAdapter;
            }
            internal set
            {
                dataAdapter = value;
            }
        }

        public object Model
        {
            get
            {
                return dataAdapter[Constants.BeeModelName];
            }
        }

        public string ControllerName
        {
            get
            {
                return dataAdapter[Constants.BeeControllerName] as string;
            }
        }

        public string ActionName
        {
            get
            {
                return dataAdapter[Constants.BeeActionName] as string;
            }
        }

        public string PageId
        {
            get
            {
                return pageId;
            }
        }
    }

    public class BeePageView : Page
    {
        private string pageId;
        protected BeeDataAdapter dataAdapter;
        private BeeHtmlHelper htmlHelper;

        public BeePageView()
        {
            this.EnableViewState = false;
            pageId = DateTime.Now.ToString("hhmmssffff");
            htmlHelper = new BeeHtmlHelper(this);

            this.PreInit += new EventHandler(BeePageView_PreInit);
        }

        void BeePageView_PreInit(object sender, EventArgs e)
        {
            BeeMasterPage masterPage = Master as BeeMasterPage;
            if (masterPage != null)
            {
                masterPage.Init(pageId, dataAdapter, htmlHelper);
            }
        }

        internal void InitData(BeeDataAdapter viewData)
        {
            this.dataAdapter = viewData;
           
        }
        public BeeDataAdapter ViewData
        {
            get
            {
                return dataAdapter;
            }
        }

        public object Model
        {
            get
            {
                return dataAdapter[Constants.BeeModelName];
            }
        }

        public string ControllerName
        {
            get
            {
                return dataAdapter[Constants.BeeControllerName] as string;
            }
        }

        public string ActionName
        {
            get
            {
                return dataAdapter[Constants.BeeActionName] as string;
            }
        }

        public string PageId
        {
            get
            {
                return pageId;
            }
        }

        public BeeHtmlHelper HtmlHelper
        {
            get
            {
                return htmlHelper;
            }
        }
    }

    public class BeePageView<TModel> : BeePageView
    {
        public new TModel Model
        {
            get
            {
                return (TModel)dataAdapter[Constants.BeeModelName];
            }
        }
    }
}
