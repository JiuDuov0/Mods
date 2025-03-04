using Entity.Tag;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Entity.Mod
{
    [Table("ModTags")]
    public class ModTagsEntity
    {
        /// <summary>
        /// Mod标签的唯一标识符
        /// </summary>
        [Key]
        public string? ModTagId { get; set; }

        /// <summary>
        /// 所属Mod的唯一标识符
        /// </summary>
        public string? ModId { get; set; }

        /// <summary>
        /// 标签的唯一标识符
        /// </summary>
        public string? TagId { get; set; }

        /// <summary>
        /// 导航属性，指向所属的Mod
        /// </summary>
        [ForeignKey("ModId")]
        public ModEntity? Mod { get; set; }

        /// <summary>
        /// 导航属性，指向标签
        /// </summary>
        [ForeignKey("TagId")]
        public TagEntity? Tag { get; set; }
    }
}
