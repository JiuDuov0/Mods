﻿using Entity.Tag;
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
        public string? CreatorUserId { get; set; }

        /// <summary>
        /// Mod的创建时间
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Mod的更新时间
        /// </summary>
        public DateTime UpdatedAt { get; set; }

        /// <summary>
        /// Mod的介绍视频链接
        /// </summary>
        public string? VideoUrl { get; set; }

        /// <summary>
        /// Mod父级mod
        /// </summary>
        public string? Parentld { get; set; }

        /// <summary>
        /// 下载次数
        /// </summary>
        public Int64? DownLoadCount { get; set; }

        /// <summary>
        /// Mod的标签
        /// </summary>
        [NotMapped]
        public List<TagEntity> Tags { get; set; } = new List<TagEntity>();
    }
}
