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
        [NotMapped]
        /// <summary>
        /// 角色id
        /// </summary>
        public List<string> UserRoleID { get; set; }
        /// <summary>
        /// 导航属性，指向Mod的图片
        /// </summary>
        public List<ModPictureEntity> ModPictureEntities { get; set; }
        /// <summary>
        /// 导航属性，指向Mod的标签
        /// </summary>
        public List<ModTagsEntity> ModTagsEntities { get; set; }
        /// <summary>
        /// 导航属性，指向Mod的类型
        /// </summary>
        public List<ModTypeEntity> ModTypeEntities { get; set; }
        /// <summary>
        /// 导航属性，指向Mod的版本
        /// </summary>
        public List<ModVersionEntity> ModVersionEntities { get; set; }
    }
}
