using LiteDB;
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace ZakYip.Singulation.Infrastructure.Configs.Entities {

    /// <summary>
    /// 轴网格布局的数据库文档实体。
    /// </summary>
    public sealed class AxisGridLayoutDoc {
        /// <summary>
        /// 文档唯一标识符（单例模式）。
        /// </summary>
        [BsonId] public string Id { get; set; } = "singleton";

        /// <summary>
        /// 网格行数。
        /// </summary>
        public int Rows { get; set; }

        /// <summary>
        /// 网格列数。
        /// </summary>
        public int Cols { get; set; }
    }
}