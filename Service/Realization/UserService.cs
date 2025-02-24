using Entity.User;
using EF.Interface;
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
            return _IDbContextServices.CreateContext(EF.ReadOrWriteEnum.Read).UserEntity.Skip(skip).Take(take).ToList();
        }
    }
}
