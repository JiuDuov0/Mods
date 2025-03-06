using Entity.Approve;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Entity.Mod
{
    [Table("ModVersion")]
    public class ModVersionEntity
    {
        /// <summary>
        /// 版本的唯一标识符
        /// </summary>
        [Key]
        public string? VersionId { get; set; }

        /// <summary>
        /// 所属Mod的唯一标识符
        /// </summary>
        public string? ModId { get; set; }

        /// <summary>
        /// 版本号
        /// </summary>
        public string? VersionNumber { get; set; }

        /// <summary>
        /// 版本的描述
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Mod的下载链接
        /// </summary>
        public string? DownloadUrl { get; set; }

        /// <summary>
        /// 版本的创建时间
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// 版本的更新时间
        /// </summary>
        public DateTime UpdatedAt { get; set; }

        /// <summary>
        /// 版本的状态
        /// </summary>
        public string? Status { get; set; }

        /// <summary>
        /// 导航属性，指向所属的Mod
        /// </summary>
        [ForeignKey("ModId")]
        public ModEntity? Mod { get; set; }

        public List<ApproveModVersionEntity> ApproveModVersionEntity { get; set; }
    }
}
