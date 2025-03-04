using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Entity.Mod
{
    [Table("ModPicture")]
    public class ModPictureEntity
    {
        /// <summary>
        /// 图片的唯一标识符
        /// </summary>
        [Key]
        public string? PictureId { get; set; }

        /// <summary>
        /// 所属Mod的唯一标识符
        /// </summary>
        public string? ModId { get; set; }

        /// <summary>
        /// 图片的URL
        /// </summary>
        public string? Url { get; set; }

        /// <summary>
        /// 图片的描述
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// 导航属性，指向所属的Mod
        /// </summary>
        [ForeignKey("ModId")]
        public ModEntity? Mod { get; set; }
    }
}
