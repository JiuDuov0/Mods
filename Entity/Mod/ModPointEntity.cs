using Entity.User;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Entity.Mod
{
    /// <summary>
    /// Mod评分实体
    /// </summary>
    [Table("ModPoint")]
    public class ModPointEntity
    {
        /// <summary>
        /// Mod评分的唯一标识符
        /// </summary>
        [Key]
        public string? ModPointId { get; set; }

        /// <summary>
        /// 所属Mod的唯一标识符
        /// </summary>
        public string? ModId { get; set; }

        /// <summary>
        /// 评分的用户的唯一标识符
        /// </summary>
        public string? UserId { get; set; }

        /// <summary>
        /// 评分
        /// </summary>
        public int? Point { get; set; }

        /// <summary>
        /// 评分理由
        /// </summary>
        public string? Reason { get; set; }

        /// <summary>
        /// 评分时间
        /// </summary>
        public DateTime? CreatedAt { get; set; }

        /// <summary>
        /// 导航属性，指向所属的Mod
        /// </summary>
        [ForeignKey("ModId")]
        public ModEntity ModEntity { get; set; }

        /// <summary>
        /// 导航属性，指向评分的用户
        /// </summary>
        [ForeignKey("UserId")]
        public UserEntity UserEntity { get; set; }
    }
}
