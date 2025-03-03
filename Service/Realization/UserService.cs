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
            var context = _IDbContextServices.CreateContext(ReadOrWriteEnum.Read);
            var user = context.UserEntity.FirstOrDefault(x => x.Mail == Account && x.Password == Password);
            //角色
            if (user != null && user.UserId != null)
            {
                var roleids = context.UserRoleEntity.Where(x => x.UserId == user.UserId).ToList();
                if (roleids == null || roleids.Count == 0)
                {
                    user.UserRoleID = new List<string>() { "4f58518b-5f9c-7cfe-ab48-9abc5d9ccc03" };
                }
                else
                {
                    //后面需要多角色再改吧
                    user.UserRoleID = new List<string> { roleids[0].RoleId };
                }
            }
            return user;
        }
    }
}
