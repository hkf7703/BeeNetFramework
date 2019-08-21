using Bee.Web;
using Bee.Web.Swagger;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Bee.WebDemo.Controllers
{
    /// <summary>
    /// 实例
    /// </summary>
    [BeeController()]
    public class DemoController : AbstractSwaggerController
    {
        /// <summary>
        /// 加法
        /// </summary>
        /// <param name="i">i</param>
        /// <param name="j">j</param>
        /// <returns>和</returns>
        public int Add(int i, int j)
        {
            return i + j;
        }

        /// <summary>
        /// 测试
        /// </summary>
        /// <returns>json返回</returns>
        public JsonResult Test()
        {
            return null;
        }
    }
}