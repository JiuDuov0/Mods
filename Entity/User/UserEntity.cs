using Entity.Approve;
using Entity.File;
using Entity.Mod;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Entity.User
{
    [Table("User")]
    public class UserEntity
    {
        /// <summary>
        /// 用户id
        /// </summary>
        public string? UserId { get; set; }
        /// <summary>
        /// 昵称
        /// </summary>
        public string? NickName { get; set; }
        /// <summary>
        /// 邮箱
        /// </summary>
        public string? Mail { get; set; }
        /// <summary>
        /// 头像
        /// </summary>
        public string? HeadPic { get; set; }
        /// <summary>
        /// 密码
        /// </summary>
        public string? Password { get; set; }
        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime? CreatedAt { get; set; }

        /// <summary>
        /// 导航属性，指向Files
        /// </summary>
        public List<FilesEntity> FilesEntities { get; set; }

        /// <summary>
        /// 导航属性，指向Mod
        /// </summary>
        public List<ModEntity> ModEntities { get; set; }

        /// <summary>
        /// 导航属性，指向Mod评分
        /// </summary>
        public List<ModPointEntity> ModPointEntities { get; set; }

        /// <summary>
        /// 导航属性，指向ApproveModVersion
        /// </summary>
        public List<ApproveModVersionEntity> ApproveModVersionEntities { get; set; }

        /// <summary>
        /// 角色id
        /// </summary>
        [NotMapped]
        public List<string> UserRoleID { get; set; }
    }
}
