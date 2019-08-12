using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Bee.Web
{
    public class BeeAutoModelInfo
    {
        public List<BeeDataAdapter> HeaderInfo;
        public List<BeeDataAdapter> SearchInfo;
        public List<BeeDataAdapter> DetailInfo;
        public Dictionary<string, string> DataMappingInfo;
    }
}
