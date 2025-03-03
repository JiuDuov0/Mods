using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Entity.Role
{
    /// <summary>
    /// 角色 有点懒就先这样设计吧
    /// </summary>
    public class RoleEntity
    {
        public List<RoleEntity> GetRoleList()
        {
            return new List<RoleEntity>()
            {
                new RoleEntity(){ Id = "45166589-67eb-4012-abcc-817a0fa12c0e", RoleName = "Developer" },
                new RoleEntity(){ Id = "b156c735-fe7b-421a-4764-78867798ef42", RoleName = "Auditors" },
                new RoleEntity(){ Id = "74c3d1d8-d156-4314-bfea-a3162c014117", RoleName = "ModDev" },
                new RoleEntity(){ Id = "4f58518b-5f9c-7cfe-ab48-9abc5d9ccc03", RoleName = "User" }
            };
        }

        public string? Id { get; set; }
        public string? RoleName { get; set; }
    }
}
