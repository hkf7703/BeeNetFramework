using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Bee.Core;
using System.Runtime.Serialization;

namespace Bee.Web
{
    internal class MvcException : CoreException
    {
        public MvcException()
        {
        }

        public MvcException(string message)
            : base(message)
        {
        }

        public MvcException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        protected MvcException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
