using Entity.Mod;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Entity.User
{
    [Table("UserModSubscribe")]
    public class UserModSubscribeEntity
    {
        [Key]
        public string? SubscribeId { get; set; }

        public string? UserId { get; set; }
        public string? ModId { get; set; }

        [ForeignKey("UserId")]
        public UserEntity? User { get; set; }

        [ForeignKey("ModId")]
        public ModEntity? Mod { get; set; }

        public DateTime SubscribedAt { get; set; }
    }
}
