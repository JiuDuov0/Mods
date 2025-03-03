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
    }
}
