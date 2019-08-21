using Bee.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bee.Auth
{
    public class LoginInfo
    {
        internal LoginInfo()
        {
            
            
        }

        public string AccountId
        {
            get;
            internal set;
        }

        public int AccountIdInt
        {
            get
            {
                int result = -1;

                int.TryParse(AccountId, out result);

                return result;
            }
        }

        public Guid AccountIdGuid
        {
            get
            {
                Guid result = Guid.Empty;

                Guid.TryParse(AccountId, out result);

                return result;
            }
        }

    }
}
