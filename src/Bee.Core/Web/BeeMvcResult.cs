using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Bee.Web
{
    public class BeeMvcResult
    {
        /// <summary>
        /// code :
        /// 200 正常
        /// 200~250 
        /// 400 请求无效 错误码:InvalidArgument
        /// 403 拒绝请求， 无效token， token失效
        /// 404 service不存在
        /// 405 内部错误
        /// 430 其他客户端登入
        /// </summary>
        public int code { get; set; }
        public string msg { get; set; }
        public object data { get; set; }
    }
}
