using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Entity.Tag
{
    [Table("Tag")]
    public class TagEntity
    {
        /// <summary>
        /// 标签的唯一标识符
        /// </summary>
        [Key]
        public string? TagId { get; set; }

        /// <summary>
        /// 标签的名称
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// 标签的描述
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// 标签的创建时间
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// 标签的更新时间
        /// </summary>
        public DateTime UpdatedAt { get; set; }
    }
}
