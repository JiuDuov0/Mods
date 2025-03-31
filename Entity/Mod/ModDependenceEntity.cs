using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Entity.Mod
{
    [Table("ModDependence")]
    public class ModDependenceEntity
    {
        /// <summary>
        /// 依赖的唯一标识符
        /// </summary>
        [Key]
        public string? ModDependenceId { get; set; }

        /// <summary>
        /// 所属Mod的唯一标识符
        /// </summary>
        public string? ModId { get; set; }

        /// <summary>
        /// 依赖的Mod版本的唯一标识符
        /// </summary>
        public string? DependenceModVersionId { get; set; }

        /// <summary>
        /// 导航属性，指向所属的Mod
        /// </summary>
        [ForeignKey("ModId")]
        public ModEntity? Mod { get; set; }

        /// <summary>
        /// 导航属性，指向依赖的Mod版本
        /// </summary>
        [ForeignKey("DependenceModVersionId")]
        public ModVersionEntity? DependenceModVersion { get; set; }
    }
}
