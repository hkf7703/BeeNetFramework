using Bee.Auth;
using Bee.Util;
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
        public string Login(string name, string password)
        {
            return LoginInfoManager.Instance.JwtToken("1000");
        }

        public ActionResult LoginInfo()
        {
            return Json(LoginInfoManager.Instance.LoginInfo);
        }

        public object Test()
        {
            var data = BeeDataAdapter.New.Add("test", 1).Add("test2", DateTime.Now).Add("test3", 0.5533d).Add("test4", Guid.NewGuid()).Add("test5", null);
            var data2 = BeeDataAdapter.New.Merge(data, true).Add("test6", data);
            var json = SerializeUtil.ToJson(data2);
            return BeeDataAdapter.From(json);
        }
    }
}