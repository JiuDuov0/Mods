using Entity.Type;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Entity.Mod
{
    [Table("ModType")]
    public class ModTypeEntity
    {
        /// <summary>
        /// Mod类型的唯一标识符
        /// </summary>
        [Key]
        public string? ModTypeId { get; set; }

        /// <summary>
        /// 所属Mod的唯一标识符
        /// </summary>
        public string? ModId { get; set; }

        /// <summary>
        /// 类型的唯一标识符
        /// </summary>
        public string? TypeId { get; set; }

        /// <summary>
        /// 导航属性，指向所属的Mod
        /// </summary>
        [ForeignKey("ModId")]
        public ModEntity? Mod { get; set; }

        /// <summary>
        /// 导航属性，指向类型
        /// </summary>
        [ForeignKey("TypeId")]
        public TypesEntity? Type { get; set; }
    }
}
