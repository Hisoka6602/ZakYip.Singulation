using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace ZakYip.Singulation.Drivers.Common {

    /// <summary>
    /// 表示用于解析字节跨度为特定类型的委托。
    /// </summary>
    /// <typeparam name="T">解析的目标类型。</typeparam>
    /// <param name="span">要解析的只读字节跨度。</param>
    /// <returns>解析后的类型实例。</returns>
    public delegate T SpanParser<out T>(ReadOnlySpan<byte> span);
}