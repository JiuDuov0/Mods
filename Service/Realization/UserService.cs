using EF;
using EF.Interface;
using Entity.User;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Realization
{
    internal class UserService : Interface.IUserService
    {
        private readonly ICreateDBContextService _IDbContextServices;

        public UserService(ICreateDBContextService iDbContextServices)
        {
            _IDbContextServices = iDbContextServices;
        }

        public List<UserEntity> GetPages(int skip, int take)
        {
            return _IDbContextServices.CreateContext(ReadOrWriteEnum.Read).UserEntity.Skip(skip).Take(take).ToList();
        }

        public UserEntity Login(string Account, string Password)
        {
            var user = _IDbContextServices.CreateContext(ReadOrWriteEnum.Read).UserEntity.FirstOrDefault(x => x.Mail == Account && x.Password == Password);
            //添加角色
            return user;
        }
    }
}
