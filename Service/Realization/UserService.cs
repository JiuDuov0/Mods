using EF;
using EF.Interface;
using Entity.Approve;
using Entity.Mod;
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
            if (ReadContext.UserEntity.FirstOrDefault(x => x.Mail == entity.Mail) != null)
            {
                return null;
            }
            WriteContext.Add(entity);
            WriteContext.SaveChanges();
            //System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch();
            //watch.Start();

            //watch.Stop();
            //var timeSpan = watch.Elapsed.TotalMilliseconds;
            for (int i = 0; i < 5; i++)
            {
                var userselect = ReadContext.UserEntity.FirstOrDefault(x => x.UserId == entity.UserId);
                if (userselect != null && userselect.UserId != null)
                {
                    userselect.UserRoleID = new List<string>() { "4f58518b-5f9c-7cfe-ab48-9abc5d9ccc03" };
                    return userselect;
                }
                Thread.Sleep(1000);
            }
            return null;
        }

        public bool SubscribeToMod(string userId, string modId)
        {
            var context = _IDbContextServices.CreateContext(ReadOrWriteEnum.Write);
            var subscription = new UserModSubscribeEntity
            {
                SubscribeId = Guid.NewGuid().ToString(),
                UserId = userId,
                ModId = modId,
                SubscribedAt = DateTime.Now
            };
            context.UserModSubscribeEntity.Add(subscription);
            return context.SaveChanges() > 0;
        }

        public List<ModEntity> UserAllSubscribeModPage(dynamic json, string UserId)
        {
            var Skip = Convert.ToInt32((string)json.Skip);
            var Take = Convert.ToInt32((string)json.Take);
            IQueryable<ModEntity> context = _IDbContextServices.CreateContext(ReadOrWriteEnum.Read).ModEntity.Include(x => x.ModVersionEntities);
            context = context.Where(x =>
            x.UserModSubscribeEntities.Any(y => y.UserId == UserId) &&
            (x.ModVersionEntities.Any(y => y.ApproveModVersionEntity.Any(z => z.Status == ((int)ApproveModVersionStatusEnum.Approved).ToString())) ||
            x.ModVersionEntities.Any(y => y.Status == ((int)ApproveModVersionStatusEnum.Approved).ToString())));
            return context.OrderBy(x => x.DownloadCount).Skip(Skip).Take(Take).ToList();
        }

        public bool UserUnsubscribeMod(string UserId, string ModId)
        {
            var Context = _IDbContextServices.CreateContext(ReadOrWriteEnum.Write);
            var entity = Context.UserModSubscribeEntity.FirstOrDefault(x => x.UserId == UserId && x.ModId == ModId);
            Context.UserModSubscribeEntity.Remove(entity);
            return Context.SaveChanges() > 0;
        }
    }
}
