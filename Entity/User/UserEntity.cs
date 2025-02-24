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
        [NotMapped]
        /// <summary>
        /// 角色id
        /// </summary>
        public string[] UserRoleID { get; set; }
    }
}
