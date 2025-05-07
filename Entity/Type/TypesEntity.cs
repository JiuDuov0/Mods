using Entity.Game;
using Entity.Mod;
using Entity.Role;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Entity.Type
{
    [Table("Types")]
    public class TypesEntity
    {
        [Key]
        public string? TypesId { get; set; }
        public string? TypeName { get; set; }
        public int? Sort { get; set; }
        public string? GameId { get; set; }
        public List<ModTypeEntity> ModTypeEntities { get; set; }
        [ForeignKey("GameId")]
        public GameEntity GameEntity { get; set; }
    }
}
