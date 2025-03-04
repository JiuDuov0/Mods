﻿using EF;
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

        public List<UserEntity> GetPages(dynamic json)
        {
            int skip = Convert.ToInt32(json.Skip);
            int take = Convert.ToInt32(json.Take);
            var entity = _IDbContextServices.CreateContext(ReadOrWriteEnum.Read).UserEntity;
            //条件
            //entity.Where(x => x.UserId == "");
            return entity.Skip(skip).Take(take).ToList();
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
                    //不储存普通用户的角色
                    user.UserRoleID = new List<string>() { "4f58518b-5f9c-7cfe-ab48-9abc5d9ccc03" };
                }
                else
                {
                    var list = new List<string>();
                    foreach (var item in roleids)
                    {
                        list.Add(item.RoleId);
                    }
                    user.UserRoleID = list;
                }
            }
            return user;
        }

        public UserEntity? Register(UserEntity entity)
        {
            var WriteContext = _IDbContextServices.CreateContext(ReadOrWriteEnum.Write);
            var ReadContext = _IDbContextServices.CreateContext(ReadOrWriteEnum.Read);

            WriteContext.Add(entity);
            WriteContext.SaveChanges();
            for (int i = 0; i < 5; i++)
            {
                Thread.Sleep(1000);
                var userselect = ReadContext.UserEntity.FirstOrDefault(x => x.UserId == entity.UserId);
                if (userselect != null && userselect.UserId != null)
                {
                    return userselect;
                }
            }
            return null;
        }
    }
}
