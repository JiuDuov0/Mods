using Entity.Mod;
using Entity.User;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Entity.Approve
{
    [Table("ApproveModVersion")]
    public class ApproveModVersionEntity
    {
        /// <summary>
        /// 审批的唯一标识符
        /// </summary>
        [Key]
        public string? ApproveModVersionId { get; set; }

        /// <summary>
        /// Mod版本的唯一标识符
        /// </summary>
        public string? VersionId { get; set; }

        /// <summary>
        /// 审批者的用户ID
        /// </summary>
        public string? UserId { get; set; }

        /// <summary>
        /// 审批时间
        /// </summary>
        public DateTime? ApprovedAt { get; set; }

        /// <summary>
        /// 审批状态
        /// </summary>
        public string? Status { get; set; } // Approved=20, Rejected=10, Pending=0

        /// <summary>
        /// 审批意见
        /// </summary>
        public string? Comments { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime? CreatedAt { get; set; }

        /// <summary>
        /// 导航属性，指向所属的ModVersion
        /// </summary>
        [ForeignKey("VersionId")]
        public ModVersionEntity? ModVersion { get; set; }

        /// <summary>
        /// 导航属性，指向审批者
        /// </summary>
        [ForeignKey("UserId")]
        public UserEntity? User { get; set; }
    }
}
