using Entity.Role;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Entity.User
{
    [Table("UserRole")]
    public class UserRoleEntity
    {
        public string? Id { get; set; }
        public string? UserId { get; set; }
        public string? RoleId { get; set; }

        [ForeignKey("UserId")]
        public UserEntity UserEntity { get; set; }

        [NotMapped]
        public RoleEntity RoleEntity { get; set; }
    }
}
