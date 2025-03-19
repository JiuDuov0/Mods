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
    [Table("Mod")]
    public class ModEntity
    {
        /// <summary>
        /// Mod的唯一标识符
        /// </summary>
        [Key]
        public string? ModId { get; set; }

        /// <summary>
        /// Mod的名称
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// Mod的描述
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Mod的创建者用户ID
        /// </summary>
        [InverseProperty("UserId")]
        public string? CreatorUserId { get; set; }

        /// <summary>
        /// Mod的创建时间
        /// </summary>
        public DateTime? CreatedAt { get; set; }

        /// <summary>
        /// Mod的更新时间
        /// </summary>
        public DateTime? UpdatedAt { get; set; }

        /// <summary>
        /// Mod的介绍视频链接
        /// </summary>
        public string? VideoUrl { get; set; }

        /// <summary>
        /// 下载次数
        /// </summary>
        public Int64? DownloadCount { get; set; }

        public bool SoftDeleted { get; set; }

        /// <summary>
        /// 导航属性，指向Mod的图片
        /// </summary>
        public List<ModPictureEntity> ModPictureEntities { get; set; }

        /// <summary>
        /// 导航属性，指向Mod的类型
        /// </summary>
        public List<ModTypeEntity> ModTypeEntities { get; set; }

        /// <summary>
        /// 导航属性，指向Mod的版本
        /// </summary>
        public List<ModVersionEntity> ModVersionEntities { get; set; }

        /// <summary>
        /// 导航属性，指向Mod的订阅人
        /// </summary>
        public List<UserModSubscribeEntity> UserModSubscribeEntities { get; set; }

        /// <summary>
        /// 导航属性 指向创建者
        /// </summary>
        [ForeignKey("CreatorUserId")]
        public UserEntity? CreatorEntity { get; set; }

        /// <summary>
        /// 导航属性，指向Mod的评分
        /// </summary>
        public List<ModPointEntity> ModPointEntities { get; set; }

        /// <summary>
        /// 是否订阅了本mod
        /// </summary>
        [NotMapped]
        public bool? IsMySubscribe { get; set; }

        /// <summary>
        /// 平均分
        /// </summary>
        [NotMapped]
        public double? AVGPoint { get; set; }
    }
}
