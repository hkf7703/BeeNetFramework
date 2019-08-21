using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Bee.Data
{
    public class DbTypeEntity
    {
        public DbTypeEntity(Type dotnetType, int maxLength)
        {
            this.DotNetType = dotnetType;
            this.MaxLength = maxLength;
        }

        public Type DotNetType { get; set; }
        public int MaxLength { get; set; }
    }
}
