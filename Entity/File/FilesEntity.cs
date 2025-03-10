using Entity.User;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Entity.File
{
    [Table("Files")]
    public class FilesEntity
    {
        /// <summary>
        /// 文件的唯一标识符
        /// </summary>
        [Key]
        public string? FilesId { get; set; }
        /// <summary>
        /// 文件类型
        /// </summary>
        public string? FilesType { get; set; }
        /// <summary>
        /// 文件名称
        /// </summary>
        public string? FilesName { get; set; }
        /// <summary>
        /// 文件大小
        /// </summary>
        public string? Size { get; set; }
        /// <summary>
        /// 文件路径
        /// </summary>
        public string? Path { get; set; }
        /// <summary>
        /// 文件上传者
        /// </summary>
        public string? UserId { get; set; }
        /// <summary>
        /// 文件上传时间
        /// </summary>
        public DateTime? CreatedAt { get; set; }

        /// <summary>
        /// 导航属性，指向用户
        /// </summary>
        [ForeignKey("UserId")]
        public UserEntity? UserEntity { get; set; }
    }
}
