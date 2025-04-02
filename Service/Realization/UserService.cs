using EF;
using EF.Interface;
using Entity.Approve;
using Entity.Mod;
using Entity.Role;
using Entity.User;
using Microsoft.EntityFrameworkCore;
using Redis.Interface;
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
        private readonly IRedisManageService _IRedisManageService;

        public UserService(ICreateDBContextService iDbContextServices, IRedisManageService redisManageService)
        {
            _IDbContextServices = iDbContextServices;
            _IRedisManageService = redisManageService;
        }

        public bool AddUserRole(UserRoleEntity entity)
        {
            entity.Id = Guid.NewGuid().ToString();
            var Context = _IDbContextServices.CreateContext(ReadOrWriteEnum.Write);
            entity.UserId = Context.UserEntity.FirstOrDefault(x => x.Mail == entity.UserId).UserId;
            Context.UserRoleEntity.Add(entity);
            return Context.SaveChanges() > 0;
        }

        public bool DeleteUserRole(string Id)
        {
            var Context = _IDbContextServices.CreateContext(ReadOrWriteEnum.Write);
            var entity = Context.UserRoleEntity.FirstOrDefault(x => x.Id == Id);
            if (entity != null)
            {
                Context.UserRoleEntity.Remove(entity);
            }
            return Context.SaveChanges() > 0;
        }

        public UserRoleEntity UpdateUserRole(UserRoleEntity entity)
        {
            var Context = _IDbContextServices.CreateContext(ReadOrWriteEnum.Write);
            var existingEntity = Context.UserRoleEntity.FirstOrDefault(x => x.Id == entity.Id);
            if (existingEntity != null)
            {
                existingEntity.UserId = entity.UserId;
                existingEntity.RoleId = entity.RoleId;
                Context.UserRoleEntity.Update(existingEntity);
                Context.SaveChanges();
            }
            return existingEntity;
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

        public List<UserRoleEntity> GetUserRolePages(dynamic json)
        {
            int Take = Convert.ToInt32(json.Take);
            int Skip = Convert.ToInt32(json.Skip);
            string Mail = Convert.ToString(json.Mail);
            string RoleId = Convert.ToString(json.RoleId);
            IQueryable<UserRoleEntity> Context = _IDbContextServices.CreateContext(ReadOrWriteEnum.Read).UserRoleEntity.Include(x => x.UserEntity);
            #region 条件
            if (!string.IsNullOrWhiteSpace(Mail))
            {
                Context = Context.Where(x => x.UserEntity.Mail.Contains(Mail));
            }
            if (!string.IsNullOrWhiteSpace(RoleId))
            {
                Context = Context.Where(x => x.RoleId == RoleId);
            }
            #endregion
            var Role = new RoleEntity().GetRoleList();
            var list = Context.Skip(Skip).Take(Take).ToList();
            #region 过滤
            foreach (var item in list)
            {
                item.RoleEntity = Role.FirstOrDefault(x => x.Id == item.RoleId);
                item.UserEntity.Password = null;
            }
            #endregion
            return list;
        }

        public async Task<UserRoleEntity?> GetUserRoleByIdAsync(string Id)
        {
            var Role = new RoleEntity().GetRoleList();
            var entity = await _IDbContextServices.CreateContext(ReadOrWriteEnum.Read).UserRoleEntity.Include(x => x.UserEntity).FirstOrDefaultAsync(x => x.Id == Id);
            entity.RoleEntity = Role.FirstOrDefault(x => x.Id == entity.RoleId);
            return entity;
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
            IQueryable<ModEntity> context = _IDbContextServices.CreateContext(ReadOrWriteEnum.Read).ModEntity.Include(x => x.ModVersionEntities).Include(x => x.ModTypeEntities).ThenInclude(x => x.Types);
            context = context.Where(x =>
            x.UserModSubscribeEntities.Any(y => y.UserId == UserId) &&
            (x.ModVersionEntities.Any(y => y.ApproveModVersionEntity.Status == ((int)ApproveModVersionStatusEnum.Approved).ToString()) ||
            x.ModVersionEntities.Any(y => y.Status == ((int)ApproveModVersionStatusEnum.Approved).ToString())));
            return context.OrderByDescending(x => x.DownloadCount).ThenBy(x => x.CreatedAt).Skip(Skip).Take(Take).ToList();
        }

        public bool UserUnsubscribeMod(string UserId, string ModId)
        {
            var Context = _IDbContextServices.CreateContext(ReadOrWriteEnum.Write);
            var entity = Context.UserModSubscribeEntity.FirstOrDefault(x => x.UserId == UserId && x.ModId == ModId);
            Context.UserModSubscribeEntity.Remove(entity);
            return Context.SaveChanges() > 0;
        }

        public async Task<UserEntity?> GetUserByUserIdAsync(string? UserId)
        {
            return await _IDbContextServices.CreateContext(ReadOrWriteEnum.Read).UserEntity.FirstOrDefaultAsync(x => x.UserId == UserId);
        }

        public async Task<bool> UpdateUserAsync(UserEntity entity)
        {
            var Context = _IDbContextServices.CreateContext(ReadOrWriteEnum.Write);
            var updateentity = await Context.UserEntity.FirstOrDefaultAsync(x => x.UserId == entity.UserId);
            if (!string.IsNullOrWhiteSpace(entity.NickName) && entity.NickName.Length <= 8 && entity.NickName != updateentity.NickName)
            {
                updateentity.NickName = entity.NickName;
            }
            if (!string.IsNullOrWhiteSpace(entity.HeadPic) && entity.HeadPic != updateentity.HeadPic)
            {
                updateentity.HeadPic = entity.HeadPic;
            }
            if (!string.IsNullOrWhiteSpace(entity.FeedBackMail) && entity.FeedBackMail != updateentity.FeedBackMail)
            {
                updateentity.FeedBackMail = entity.FeedBackMail;
            }
            if (!string.IsNullOrEmpty(entity.Token) && entity.Token != updateentity.Token)
            {
                updateentity.Token = entity.Token;
            }
            if (!string.IsNullOrEmpty(entity.Password) && entity.Password != updateentity.Password)
            {
                updateentity.Password = entity.Password;
            }
            Context.UserEntity.Update(updateentity);
            return await Context.SaveChangesAsync() > 0;
        }
    }
}
