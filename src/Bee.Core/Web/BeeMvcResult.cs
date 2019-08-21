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
        /// 508 非法token
        /// 512 其他客户端登入
        /// 514 过期
        /// </summary>
        public int code { get; set; }
        public string message { get; set; }
        public object data { get; set; }
    }
}
