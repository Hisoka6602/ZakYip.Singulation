using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace ZakYip.Singulation.Drivers.Common {

    public delegate T SpanParser<out T>(ReadOnlySpan<byte> span);
}