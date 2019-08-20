using Bee.Web;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Bee.WebDemo.Controllers
{
    public class DemoController : APIControllerBase
    {

        public int Add(int i, int j)
        {
            return i + j;
        }

        public JsonResult Test()
        {
            return null;
        }
    }
}