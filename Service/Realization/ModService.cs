using EF;
using EF.Interface;
using Entity.Approve;
using Entity.Mod;
using Entity.User;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using Redis.Interface;
using Service.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime;
using System.Text;
using System.Threading.Tasks;

namespace Service.Realization
{
    public class ModService : IModService
    {
        private readonly ICreateDBContextService _IDbContextServices;
        private readonly IRedisManageService _IRedisManageService;

        public ModService(ICreateDBContextService iDbContextServices, IRedisManageService redisManageService)
        {
            _IDbContextServices = iDbContextServices;
            _IRedisManageService = redisManageService;
        }

        public List<ModEntity> ModListPage(dynamic json, string UserId)
        {
            int Skip = Convert.ToInt32(json.Skip);
            int Take = Convert.ToInt32(json.Take);
            var Types = ((JArray)json.Types).ToObject<List<string>>();
            Types.RemoveAll(x => x == null || x == "");
            var RedisModList = _IRedisManageService.Get<List<ModEntity>>("ModListPage", 1);
            var RedisUserModSubscribe = _IRedisManageService.Get<List<UserModSubscribeEntity>>("SetUserModSubscribe" + UserId, 1);
            if (Skip + Take > 1000)
            {
                return EFGetList(json, UserId);
            }
            if (RedisModList == null)
            {
                Task.Run(() => SetModPageListToRedisAsync());

                return EFGetList(json, UserId);
            }
            if (!string.IsNullOrWhiteSpace((string)json.Search))
            {
                return EFGetList(json, UserId);
            }
            if (Types.Count > 0)
            {
                return EFGetList(json, UserId);
            }
            return ModPageListRedis(RedisModList, json, UserId, RedisUserModSubscribe, Types).Result;
        }

        private List<ModEntity> EFGetList(dynamic json, string UserId)
        {
            var task = _IDbContextServices.CreateContext(ReadOrWriteEnum.Read).UserModSubscribeEntity.Where(x => x.UserId == UserId).ToListAsync();
            int Skip = Convert.ToInt32(json.Skip);
            int Take = Convert.ToInt32(json.Take);
            var Types = ((JArray)json.Types).ToObject<List<string>>();//Newtonsoft.Json纯纯的勾失
            Types.RemoveAll(x => x == null || x == "");
            IQueryable<ModEntity> Context = _IDbContextServices.CreateContext(ReadOrWriteEnum.Read).ModEntity.Include(x => x.ModTypeEntities).ThenInclude(x => x.Types);
            #region 条件
            Context = Context.Where(x => x.SoftDeleted == false);
            if (!string.IsNullOrWhiteSpace((string)json.Search))
            {
                string Search = json.Search;
                Context = Context.Where(x => x.Name.Contains(Search));
            }
            if (Types.Count > 0)
            {
                foreach (var item in Types)
                {
                    Context = Context.Where(x => x.ModTypeEntities.Any(y => y.TypesId == item));
                }
            }
            Context = Context.Where(x =>
            (x.ModVersionEntities.Any(y => y.ApproveModVersionEntity.Status == ((int)ApproveModVersionStatusEnum.Approved).ToString()) ||
            x.ModVersionEntities.Any(y => y.Status == ((int)ApproveModVersionStatusEnum.Approved).ToString())));
            #endregion
            var list = Context.OrderByDescending(x => x.DownloadCount).ThenBy(x => x.CreatedAt).Skip(Skip).Take(Take).ToList();
            var Subscribes = task.Result;
            foreach (var item in list)
            {
                item.ModTypeEntities.ForEach(x => x.Types.ModTypeEntities = null);
                item.IsMySubscribe = Subscribes.Any(x => x.ModId == item.ModId);
            }
            return list;
        }

        private async Task<List<ModEntity>> ModPageListRedis(List<ModEntity> mods, dynamic json, string UserId, List<UserModSubscribeEntity> userModSubscribeEntities, List<string> Types)
        {
            if (userModSubscribeEntities == null || userModSubscribeEntities.Count == 0)
            {
                userModSubscribeEntities = await SetUserModSubscribeToRedisAsync(UserId);
            }
            int Skip = Convert.ToInt32(json.Skip);
            int Take = Convert.ToInt32(json.Take);
            var query = mods.Where(x => { return true; });
            #region 条件
            if (!string.IsNullOrWhiteSpace((string)json.Search))
            {
                string Search = json.Search;
                query = query.Where(x => x.Name.Contains(Search));
            }
            if (Types.Count > 0)
            {
                foreach (var item in Types)
                {
                    query = query.Where(x => x.ModTypeEntities.Any(y => y.TypesId == item));
                }
            }
            query = query.Where(x =>
            (x.ModVersionEntities.Any(y => y.ApproveModVersionEntity.Status == ((int)ApproveModVersionStatusEnum.Approved).ToString()) ||
            x.ModVersionEntities.Any(y => y.Status == ((int)ApproveModVersionStatusEnum.Approved).ToString())));
            #endregion
            var list = query.OrderByDescending(x => x.DownloadCount).ThenBy(x => x.CreatedAt).Skip(Skip).Take(Take).ToList();
            foreach (var item in list)
            {
                item.ModTypeEntities.ForEach(x => x.Types.ModTypeEntities = null);
                item.ModVersionEntities = null;
                item.IsMySubscribe = userModSubscribeEntities.Any(x => x.ModId == item.ModId);
            }
            return list;
        }
        private async Task SetModPageListToRedisAsync()
        {
            var list = await _IDbContextServices.CreateContext(ReadOrWriteEnum.Read).ModEntity.Include(x => x.ModVersionEntities).ThenInclude(x => x.ApproveModVersionEntity).Include(x => x.ModTypeEntities).ThenInclude(x => x.Types).Where(x => x.SoftDeleted == false).OrderByDescending(x => x.DownloadCount).ThenBy(x => x.CreatedAt).Take(1000).ToListAsync();
            await _IRedisManageService.SetAsync("ModListPage", list, new TimeSpan(0, 30, 0), 1);
        }

        private async Task<List<UserModSubscribeEntity>> SetUserModSubscribeToRedisAsync(string UserId)
        {
            var list = await _IDbContextServices.CreateContext(ReadOrWriteEnum.Read).UserModSubscribeEntity.Where(x => x.UserId == UserId).ToListAsync();
            if (string.IsNullOrWhiteSpace(UserId))
            {
                return list;
            }
            await _IRedisManageService.SetAsync("SetUserModSubscribe" + UserId, list, new TimeSpan(0, 2, 0), 1);
            return list;
        }


        public void ApproveModVersion(string modVersionId, string approverUserId, string status, string comments)
        {
            var context = _IDbContextServices.CreateContext(ReadOrWriteEnum.Write);
            var approval = new ApproveModVersionEntity
            {
                ApproveModVersionId = Guid.NewGuid().ToString(),
                VersionId = modVersionId,
                UserId = approverUserId,
                ApprovedAt = DateTime.Now,
                Status = status,
                Comments = comments
            };
            context.ApproveModEntity.Add(approval);
            context.SaveChanges();
        }
        public async Task ApproveModVersionAsync(string modVersionId, string approverUserId, string status, string comments)
        {
            var context = _IDbContextServices.CreateContext(ReadOrWriteEnum.Write);
            var entity = await context.ApproveModEntity.FirstOrDefaultAsync(x => x.VersionId == modVersionId);
            if (entity != null)
            {
                entity.UserId = approverUserId;
                entity.ApprovedAt = DateTime.Now;
                entity.Status = status;
                entity.Comments = comments;
                context.ApproveModEntity.Update(entity);
            }
            await context.SaveChangesAsync();
        }

        public bool AddModAndModVersion(ModEntity modEntity, ModVersionEntity modVersionEntity, List<ModTypeEntity> list)
        {
            var Context = _IDbContextServices.CreateContext(ReadOrWriteEnum.Write);
            var transaction = Context.Database.BeginTransaction();
            var insertlist = new List<ModTypeEntity>();
            if (list.Count > 0)
            {
                var all = _IDbContextServices.CreateContext(ReadOrWriteEnum.Read).TypesEntity.ToList();
                for (int i = 0; i < list.Count; i++)
                {
                    var entity = all.FirstOrDefault(x => x.TypesId == list[i].TypesId);
                    if (entity != null)
                    {
                        insertlist.Add(new ModTypeEntity { ModTypeId = Guid.NewGuid().ToString(), ModId = modEntity.ModId, TypesId = entity.TypesId });
                    }
                }
            }
            try
            {
                Context.ModEntity.Add(modEntity);
                Context.ModVersionEntity.Add(modVersionEntity);
                if (insertlist.Count > 0)
                {
                    Context.ModTypeEntity.AddRange(insertlist);
                }
                Context.SaveChanges();
                transaction.Commit();
                return true;
            }
            catch (Exception)
            {
                transaction.Rollback();
                return false;
            }
        }

        public bool AddModVersion(ModVersionEntity modVersionEntity)
        {
            var Context = _IDbContextServices.CreateContext(ReadOrWriteEnum.Write);
            Context.Add(modVersionEntity);
            if (Context.SaveChanges() > 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public ModVersionEntity GetByModVersionId(string modVersionId)
        {
            return _IDbContextServices.CreateContext(ReadOrWriteEnum.Read).ModVersionEntity.FirstOrDefault(x => x.VersionId == modVersionId);
        }

        public bool AddModTypes(JArray array)
        {
            var Context = _IDbContextServices.CreateContext(ReadOrWriteEnum.Write);
            var Transaction = Context.Database.BeginTransaction();
            try
            {
                foreach (JObject item in array)
                {
                    Context.Add(new ModTypeEntity()
                    {
                        ModTypeId = Guid.NewGuid().ToString(),
                        ModId = item["ModId"].ToString(),
                        TypesId = item["TypesId"].ToString()
                    });
                }
                Context.SaveChanges();
                Transaction.Commit();
                return true;
            }
            catch (Exception)
            {
                Transaction.Rollback();
                return false;
            }
        }

        public List<ApproveModVersionEntity> GetApproveModVersionPageList(int Skip, int Take, string Search)
        {
            IQueryable<ApproveModVersionEntity> Context = _IDbContextServices.CreateContext(ReadOrWriteEnum.Read).ApproveModEntity.Include(x => x.ModVersion).ThenInclude(x => x.Mod).Where(x => x.Status == "0");
            if (!string.IsNullOrWhiteSpace(Search))
            {
                Context = Context.Where(x => x.ModVersion.Mod.Name.Contains(Search));
            }
            return Context.OrderByDescending(x => x.CreatedAt).Take(Take).Skip(Skip).ToList();
        }

        public bool IsLoginUserMods(List<string> list, string UserId)
        {
            IQueryable<ModEntity> context = _IDbContextServices.CreateContext(ReadOrWriteEnum.Read).ModEntity;
            foreach (var item in list)
            {
                context = context.Where(x => x.ModId == item);
            }
            var Mods = context.ToList();
            List<ModEntity> querylist = Mods.FindAll(x => x.CreatorUserId != UserId).ToList();
            if (querylist == null || querylist.Count == 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public bool IsLoginUserMods(string VersionId, string UserId)
        {
            var entity = _IDbContextServices.CreateContext(ReadOrWriteEnum.Read).ModEntity.Include(x => x.ModVersionEntities).Where(x => x.ModVersionEntities.Any(y => y.VersionId == VersionId)).Where(x => x.CreatorUserId == UserId).FirstOrDefault();
            return entity == null;
        }

        public List<ModEntity> GetMyCreateMod(string UserId, dynamic json)
        {
            int Skip = Convert.ToInt32(json.Skip);
            int Take = Convert.ToInt32(json.Take);
            var Types = ((JArray)json.Types).ToObject<List<string>>();//Newtonsoft.Json纯纯的勾失
            Types.RemoveAll(x => x == null || x == "");
            IQueryable<ModEntity> Context = _IDbContextServices.CreateContext(ReadOrWriteEnum.Read).ModEntity.Include(x => x.ModTypeEntities).ThenInclude(x => x.Types);
            #region 条件
            if (!string.IsNullOrWhiteSpace((string)json.Search))
            {
                string Search = json.Search;
                Context = Context.Where(x => x.Name.Contains(Search));
            }
            if (Types.Count > 0)
            {
                foreach (var item in Types)
                {
                    Context = Context.Where(x => x.ModTypeEntities.Any(y => y.TypesId == item));
                }
            }
            Context = Context.Where(x => x.CreatorUserId == UserId);
            #endregion
            return Context.OrderByDescending(x => x.DownloadCount).ThenBy(x => x.CreatedAt).Skip(Skip).Take(Take).ToList();
        }

        public async Task<ModEntity> ModDetail(string UserId, string ModId)
        {
            var Context = _IDbContextServices.CreateContext(ReadOrWriteEnum.Read);
            var avg = await Context.ModPointEntity.Where(x => x.ModId == ModId).AverageAsync(x => x.Point);
            var entity = await Context.ModEntity.IgnoreQueryFilters()
                .Include(x => x.ModVersionEntities)
                .ThenInclude(x => x.ApproveModVersionEntity)
                .Include(x => x.ModTypeEntities)
                .ThenInclude(x => x.Types)
                .Include(x => x.CreatorEntity)
                .Where(x => x.SoftDeleted == false)
                .Where(x => x.ModVersionEntities.Any(y => y.ApproveModVersionEntity.Status == "20"))
                .FirstOrDefaultAsync(x => x.ModId == ModId);

            var subscribe = await Context.UserModSubscribeEntity.FirstOrDefaultAsync(x => x.UserId == UserId && x.ModId == ModId);
            if (entity != null)
            {
                var user = new UserEntity() { UserId = entity.CreatorEntity.UserId, NickName = entity.CreatorEntity.NickName };
                entity.CreatorEntity = user;
                entity.IsMySubscribe = subscribe != null;

                // 过滤 ModVersionEntities 只包含 Status == "20" 的实体
                entity.ModVersionEntities = entity.ModVersionEntities
                    .Where(x => x.ApproveModVersionEntity.Status == "20")
                    .OrderByDescending(x => x.CreatedAt)
                    .ToList();
            }

            if (avg != null)
            {
                entity.AVGPoint = Convert.ToDouble(((double)avg).ToString("0.00"));
            }
            return entity;
        }

        public async Task<ModEntity> ModDetailAllModVersion(string UserId, string ModId)
        {
            var Context = _IDbContextServices.CreateContext(ReadOrWriteEnum.Read);
            var avg = await Context.ModPointEntity.Where(x => x.ModId == ModId).AverageAsync(x => x.Point);
            var entity = await Context.ModEntity.IgnoreQueryFilters()
                .Include(x => x.ModVersionEntities)
                .ThenInclude(x => x.ApproveModVersionEntity)
                .Include(x => x.ModTypeEntities)
                .ThenInclude(x => x.Types)
                .Include(x => x.CreatorEntity)
                .Where(x => x.SoftDeleted == false)
                .Where(x => x.ModVersionEntities.Any(y => y.ApproveModVersionEntity.Status == "20"))
                .FirstOrDefaultAsync(x => x.ModId == ModId);

            var subscribe = await Context.UserModSubscribeEntity.FirstOrDefaultAsync(x => x.UserId == UserId && x.ModId == ModId);
            if (entity != null)
            {
                var user = new UserEntity() { UserId = entity.CreatorEntity.UserId, NickName = entity.CreatorEntity.NickName };
                entity.CreatorEntity = user;
                entity.IsMySubscribe = subscribe != null;
            }

            if (avg != null)
            {
                entity.AVGPoint = Convert.ToDouble(((double)avg).ToString("0.00"));
            }
            return entity;
        }

        public ModEntity ModDetailUpd(string UserId, string ModId)
        {
            return _IDbContextServices.CreateContext(ReadOrWriteEnum.Read).ModEntity.Include(x => x.ModTypeEntities).ThenInclude(x => x.Types).FirstOrDefault(x => x.ModId == ModId && x.CreatorUserId == UserId);
        }

        public bool? UpdateModInfo(ModEntity entity, string UserId)
        {
            var WriteContext = _IDbContextServices.CreateContext(ReadOrWriteEnum.Write);
            var Transaction = WriteContext.Database.BeginTransaction();
            var mod = WriteContext.ModEntity.FirstOrDefault(x => x.ModId == entity.ModId);
            var modtypes = WriteContext.ModTypeEntity.Where(x => x.ModId == entity.ModId).ToList();
            var types = WriteContext.TypesEntity.ToList();
            mod.Description = entity.Description;
            mod.UpdatedAt = DateTime.Now;
            mod.VideoUrl = entity.VideoUrl;
            mod.PicUrl = entity.PicUrl;
            var list = new List<ModTypeEntity>();
            foreach (var item in entity.ModTypeEntities)
            {
                var type = types.FirstOrDefault(x => x.TypesId == item.TypesId);
                if (type != null)
                {
                    list.Add(new ModTypeEntity { ModTypeId = Guid.NewGuid().ToString(), ModId = entity.ModId, TypesId = item.TypesId });
                }
            }
            try
            {
                WriteContext.Update(mod);
                WriteContext.RemoveRange(modtypes);
                WriteContext.ModTypeEntity.AddRange(list);
                WriteContext.SaveChanges();
                Transaction.Commit();
            }
            catch (Exception)
            {
                Transaction.Rollback();
                return false;
                //throw;
            }
            return true;
        }

        public bool? DeleteMod(string ModId, string UserId)
        {
            var WriteContext = _IDbContextServices.CreateContext(ReadOrWriteEnum.Write);
            var Transaction = WriteContext.Database.BeginTransaction();
            try
            {
                var mod = WriteContext.ModEntity.FirstOrDefault(x => x.ModId == ModId && x.CreatorUserId == UserId);
                if (mod == null)
                {
                    return null;
                }
                mod.SoftDeleted = true;
                //todo 删除文件
                WriteContext.Update(mod);
                WriteContext.SaveChanges();
                Transaction.Commit();
            }
            catch (Exception)
            {
                Transaction.Rollback();
                return false;
            }
            return true;
        }

        public bool AddModPoint(ModPointEntity entity)
        {
            var Context = _IDbContextServices.CreateContext(ReadOrWriteEnum.Write);
            Context.ModPointEntity.Add(entity);
            return Context.SaveChanges() > 0;
        }

        public ModPointEntity UpdateModPointEntity(ModPointEntity entity)
        {
            var Context = _IDbContextServices.CreateContext(ReadOrWriteEnum.Write);
            var old = Context.ModPointEntity.FirstOrDefault(x => x.ModPointId == entity.ModPointId);
            if (old == null)
            {
                return null;
            }
            if (old.UserId != entity.UserId)
            {
                return null;
            }
            else
            {
                old.Point = entity.Point;
            }
            Context.ModPointEntity.Update(old);
            Context.SaveChanges();
            return entity;
        }

        public bool DeleteModPoint(string ModId, string UserId)
        {
            var Context = _IDbContextServices.CreateContext(ReadOrWriteEnum.Write);
            var entity = Context.ModPointEntity.FirstOrDefault(x => x.ModId == ModId && x.UserId == UserId);
            if (entity == null)
            {
                return false;
            }
            Context.ModPointEntity.Remove(entity);
            return Context.SaveChanges() > 0;
        }

        public bool DeleteModPoint(string ModPointId)
        {
            var Context = _IDbContextServices.CreateContext(ReadOrWriteEnum.Write);
            var entity = Context.ModPointEntity.FirstOrDefault(x => x.ModPointId == ModPointId);
            if (entity == null)
            {
                return false;
            }
            Context.ModPointEntity.Remove(entity);
            return Context.SaveChanges() > 0;
        }

        public ModPointEntity? GetModPointEntity(string ModId, string UserId)
        {
            return _IDbContextServices.CreateContext(ReadOrWriteEnum.Read).ModPointEntity.FirstOrDefault(x => x.ModId == ModId && x.UserId == UserId);
        }
    }
}
