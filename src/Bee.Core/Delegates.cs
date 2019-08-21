using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Bee
{
    public delegate TR CallbackReturnHandler<TR>();

    public delegate TR CallbackReturnHandler<T, TR>(T o);

    public delegate TR CallbackReturnHandler<T1, T2, TR>(T1 o1, T2 o2);

    public delegate void CallbackVoidHandler();

}
