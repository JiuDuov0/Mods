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
        public List<TypesEntity> GetRoleList()
        {
            return new List<TypesEntity>()
            {
                new TypesEntity(){ Id = "52F94622-FA0A-74C2-0AC1-11B06B5B1D86", TypeName = "视觉", Sort = 0 },
                new TypesEntity(){ Id = "4708E8C8-4D4C-093F-9C53-50E8312D745A", TypeName = "音频", Sort = 1 },
                new TypesEntity(){ Id = "6C38D615-8CC7-336D-324A-69F26F9FE62D", TypeName = "框架", Sort = 2 },
                new TypesEntity(){ Id = "BB13FD20-F807-E987-7391-4E757F4C684E", TypeName = "改变游戏玩法", Sort = 3 },
                //new TypesEntity(){ Id = "6A54C42E-4762-D83B-F7E0-41D4A6435ED6", TypeName = "" },
                //new TypesEntity(){ Id = "A12FC03F-AD66-DF42-4A7A-72FE461C6806", TypeName = "" },
                //new TypesEntity(){ Id = "2406ABF2-59AC-D64E-8A29-A2710428608B", TypeName = "" },
                //new TypesEntity(){ Id = "BA328400-D028-BB25-F837-FD19ECF892C0", TypeName = "" },
                //new TypesEntity(){ Id = "624513D9-A604-D708-6067-3DE518C80A26", TypeName = "" },
                new TypesEntity(){ Id = "F15583FF-9F23-B4AB-104F-89539A6A6C07", TypeName = "工具", Sort = 4 }
            };
        }
        [Key]
        public string? Id { get; set; }
        public string? TypeName { get; set; }
        public int? Sort { get; set; }
    }
}
