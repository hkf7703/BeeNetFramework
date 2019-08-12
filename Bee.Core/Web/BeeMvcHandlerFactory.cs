using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;

namespace Bee.Web
{
    public class BeeMvcHandlerFactory : IHttpHandlerFactory
    {

        public IHttpHandler GetHandler(HttpContext context, string requestType, string url, string pathTranslated)
        {
            throw new NotImplementedException();
        }

        public void ReleaseHandler(IHttpHandler handler)
        {
            throw new NotImplementedException();
        }
    }
}
