﻿using Entity.Mod;
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
        public UserEntity? Register(UserEntity entity);
        public bool SubscribeToMod(string userId, string modId);
        public List<ModEntity> UserAllSubscribeModPage(dynamic json, string UserId);
        public bool UserUnsubscribeMod(string UserId, string ModId);
        public List<UserRoleEntity> GetUserRolePages(dynamic json);
        public bool AddUserRole(UserRoleEntity entity);
        public UserRoleEntity UpdateUserRole(UserRoleEntity entity);
        public bool DeleteUserRole(string Id);
    }
}
