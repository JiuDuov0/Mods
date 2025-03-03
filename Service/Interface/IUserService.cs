using Entity.User;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Interface
{
    public interface IUserService
    {
        public List<UserEntity> GetPages(dynamic json);
        public UserEntity Login(string Account, string Password);
    }
}
