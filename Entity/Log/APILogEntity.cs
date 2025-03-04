using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Entity.Log
{
    [Table("APILog")]
    public class APILogEntity
    {
        [Key]
        public string? Id { get; set; }
        public string? API { get; set; }
        public string? UserId { get; set; }
        public string? IP { get; set; }
        public DateTime? CreatedAt { get; set; }
    }
}
