using EF;
using EF.Interface;
using Entity.Approve;
using Entity.Mod;
using Entity.User;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Redis.Interface;
using Service.Interface;
using System.Diagnostics;
using System.Xml.Linq;
using ViewEntity.Mod;

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

        public List<ModListViewEntity> ModListPage(dynamic json, string UserId)
        {
            int Skip = Convert.ToInt32(json.Skip);
            int Take = Convert.ToInt32(json.Take);
            var Types = ((JArray)json.Types).ToObject<List<string>>();
            var GameId = (string)json.GameId;
            Types.RemoveAll(x => x == null || x == "");
            var RedisModList = _IRedisManageService.Get<List<ModListViewEntity>>($"ModListPage:{GameId}", 1);
            var RedisUserModSubscribe = _IRedisManageService.Get<List<UserModSubscribeEntity>>($"SetUserModSubscribe:{UserId}", 1);
            if (Skip + Take > 1000)
            {
                return EFGetList(json, UserId);
            }
            if (RedisModList == null)
            {
                Task.Run(() => SetModPageListToRedisAsync(GameId));

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

        private List<ModListViewEntity> EFGetList(dynamic json, string UserId)
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
            var GameId = (string)json.GameId;
            Context = Context.Where(x =>
            (x.ModVersionEntities.Any(y => y.ApproveModVersionEntity.Status == ((int)ApproveModVersionStatusEnum.Approved).ToString()) ||
            x.ModVersionEntities.Any(y => y.Status == ((int)ApproveModVersionStatusEnum.Approved).ToString())) && x.GameId == GameId);
            #endregion
            var list = Context.OrderByDescending(x => x.DownloadCount).ThenBy(x => x.CreatedAt).Skip(Skip).Take(Take).ToList();
            var Subscribes = task.Result;
            var PageList = new List<ModListViewEntity>();
            foreach (var item in list)
            {
                var TypesList = new List<ModTypesListViewEntity>();
                foreach (var item0 in item.ModTypeEntities)
                {
                    TypesList.Add(new ModTypesListViewEntity() { TypesId = item0.TypesId, TypeName = item0.Types.TypeName });
                }
                PageList.Add(new ModListViewEntity() { ModId = item.ModId, Name = item.Name, PicUrl = item.PicUrl, ModTypeEntities = TypesList, IsMySubscribe = Subscribes.Any(x => x.ModId == item.ModId) });
            }
            return PageList;
        }

        private async Task<List<ModListViewEntity>> ModPageListRedis(List<ModListViewEntity> mods, dynamic json, string UserId, List<UserModSubscribeEntity> userModSubscribeEntities, List<string> Types)
        {
            if (userModSubscribeEntities == null)
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
            #endregion
            var list = query.Skip(Skip).Take(Take).ToList();

            foreach (var item in list)
            {
                if (userModSubscribeEntities.Where(x => x.ModId == item.ModId).Count() > 0)
                {
                    item.IsMySubscribe = true;
                }
            }


            return list;
        }
        private async Task SetModPageListToRedisAsync(string GameId)
        {
            var list = await _IDbContextServices.CreateContext(ReadOrWriteEnum.Read).ModEntity.Include(x => x.ModVersionEntities).ThenInclude(x => x.ApproveModVersionEntity).Include(x => x.ModTypeEntities).ThenInclude(x => x.Types).Where(x => x.SoftDeleted == false).Where(x =>
            (x.ModVersionEntities.Any(y => y.ApproveModVersionEntity.Status == ((int)ApproveModVersionStatusEnum.Approved).ToString()) ||
            x.ModVersionEntities.Any(y => y.Status == ((int)ApproveModVersionStatusEnum.Approved).ToString())) && x.GameId == GameId)
                .OrderByDescending(x => x.DownloadCount).ThenBy(x => x.CreatedAt).Take(1000).ToListAsync();
            var PageList = new List<ModListViewEntity>();
            foreach (var item in list)
            {
                var TypesList = new List<ModTypesListViewEntity>();
                foreach (var item0 in item.ModTypeEntities)
                {
                    TypesList.Add(new ModTypesListViewEntity() { TypesId = item0.TypesId, TypeName = item0.Types.TypeName });
                }
                PageList.Add(new ModListViewEntity() { ModId = item.ModId, Name = item.Name, PicUrl = item.PicUrl, ModTypeEntities = TypesList });
            }
            await _IRedisManageService.SetAsync($"ModListPage:{GameId}", PageList, new TimeSpan(0, 30, 0), 1);
        }

        private async Task<List<UserModSubscribeEntity>> SetUserModSubscribeToRedisAsync(string UserId)
        {
            var list = await _IDbContextServices.CreateContext(ReadOrWriteEnum.Read).UserModSubscribeEntity.Where(x => x.UserId == UserId).ToListAsync();
            if (string.IsNullOrWhiteSpace(UserId))
            {
                return list;
            }
            await _IRedisManageService.SetAsync($"SetUserModSubscribe:{UserId}", list, new TimeSpan(0, 2, 0), 1);
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

        public bool AddModAndModVersion(ModEntity modEntity, ModVersionEntity modVersionEntity, List<ModTypeEntity>? list, List<ModDependenceEntity>? ModDependenceEntities)
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
                if (ModDependenceEntities != null && ModDependenceEntities.Count > 0)
                {
                    Context.ModDependenceEntity.AddRange(ModDependenceEntities);
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
            IQueryable<ApproveModVersionEntity> Context = _IDbContextServices.CreateContext(ReadOrWriteEnum.Read).ApproveModEntity.Include(x => x.ModVersion).ThenInclude(x => x.Mod).Where(x => x.Status == "0").Where(x => x.ModVersion.Mod.SoftDeleted == false);
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

        public List<ModListViewEntity> GetMyCreateMod(string UserId, dynamic json)
        {
            int Skip = Convert.ToInt32(json.Skip);
            int Take = Convert.ToInt32(json.Take);
            var GameId = (string)json.GameId;
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
            Context = Context.Where(x => x.CreatorUserId == UserId && x.GameId == GameId);
            #endregion
            var list = Context.OrderByDescending(x => x.DownloadCount).ThenBy(x => x.CreatedAt).Skip(Skip).Take(Take).ToList();

            var result = new List<ModListViewEntity>();
            foreach (var mod in list)
            {
                var typesList = new List<ModTypesListViewEntity>();
                if (mod.ModTypeEntities != null)
                {
                    foreach (var typeEntity in mod.ModTypeEntities)
                    {
                        if (typeEntity.Types != null)
                        {
                            typesList.Add(new ModTypesListViewEntity
                            {
                                TypesId = typeEntity.TypesId,
                                TypeName = typeEntity.Types.TypeName
                            });
                        }
                    }
                }
                result.Add(new ModListViewEntity
                {
                    ModId = mod.ModId,
                    Name = mod.Name,
                    PicUrl = mod.PicUrl,
                    ModTypeEntities = typesList
                });
            }

            return result;
        }

        private async Task<List<ModEntity>> GetMyCreateModRedisAsync(string UserId)
        {
            var result = await _IDbContextServices.CreateContext(ReadOrWriteEnum.Read).ModEntity.Include(x => x.ModTypeEntities).ThenInclude(x => x.Types).Where(x => x.CreatorUserId == UserId).OrderByDescending(x => x.DownloadCount).ThenBy(x => x.CreatedAt).ToListAsync();
            await _IRedisManageService.SetAsync("GetMyCreateMod" + UserId, result, new TimeSpan(2, 0, 0), 1);
            return result;
        }

        private async Task<List<ModEntity>> GetMyCreateModEFAsync()
        {
            throw new NotImplementedException();
        }

        public async Task<ModEntity> ModDetail(string UserId, string ModId)
        {
            var Context = _IDbContextServices.CreateContext(ReadOrWriteEnum.Read);
            var avg = await Context.ModPointEntity.Where(x => x.ModId == ModId).AverageAsync(x => x.Point);
            var entity = _IRedisManageService.Get<ModEntity>($"ModDetail:{ModId}", 1);
            if (entity == null)
            {
                entity = await Context.ModEntity.IgnoreQueryFilters()
                    .Include(x => x.ModVersionEntities)
                    .ThenInclude(x => x.ApproveModVersionEntity)
                    .Include(x => x.ModTypeEntities)
                    .ThenInclude(x => x.Types)
                    .Include(x => x.CreatorEntity)
                    .Include(x => x.ModDependenceEntities)
                    .ThenInclude(x => x.DependenceModVersion)
                    .ThenInclude(x => x.Mod)
                    .Where(x => x.SoftDeleted == false)
                    .Where(x => x.ModVersionEntities.Any(y => y.ApproveModVersionEntity.Status == "20"))
                    .FirstOrDefaultAsync(x => x.ModId == ModId);
                await _IRedisManageService.SetAsync($"ModDetail:{entity.ModId}", entity, new TimeSpan(12, 0, 0), 1);
            }

            var subscribe = await Context.UserModSubscribeEntity.FirstOrDefaultAsync(x => x.UserId == UserId && x.ModId == ModId);
            if (entity != null)
            {
                var user = new UserEntity() { UserId = entity.CreatorEntity.UserId, NickName = entity.CreatorEntity.NickName };
                entity.CreatorEntity = user;
                entity.IsMySubscribe = subscribe != null;

                entity.ModVersionEntities = entity.ModVersionEntities
                    .Where(x => x.ApproveModVersionEntity.Status == "20")
                    .Where(x => !string.IsNullOrWhiteSpace(x.FilesId))
                    .OrderByDescending(x => x.CreatedAt)
                    .ToList();

                foreach (var modVersion in entity.ModVersionEntities)
                {
                    if (modVersion.ApproveModVersionEntity != null)
                    {
                        modVersion.ApproveModVersionEntity.User = null;
                        modVersion.ApproveModVersionEntity.Comments = null;
                        modVersion.ApproveModVersionEntity.ApprovedAt = null;
                    }
                }
            }

            if (avg != null)
            {
                entity.AVGPoint = Convert.ToDouble(((double)avg).ToString("0.00"));
            }
            //GC.Collect();
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
                .Include(x => x.ModDependenceEntities)
                .ThenInclude(x => x.DependenceModVersion)
                .ThenInclude(x => x.Mod)
                .Where(x => x.SoftDeleted == false)
                //.Where(x => x.ModVersionEntities.Any(y => y.ApproveModVersionEntity.Status == "20"))
                .FirstOrDefaultAsync(x => x.ModId == ModId);

            var subscribe = await Context.UserModSubscribeEntity.FirstOrDefaultAsync(x => x.UserId == UserId && x.ModId == ModId);
            if (entity != null)
            {
                var user = new UserEntity() { UserId = entity.CreatorEntity.UserId, NickName = entity.CreatorEntity.NickName };
                entity.CreatorEntity = user;
                entity.IsMySubscribe = subscribe != null;
                entity.ModVersionEntities = entity.ModVersionEntities.Where(x => !string.IsNullOrWhiteSpace(x.FilesId)).OrderByDescending(x => x.CreatedAt).ToList();
                foreach (var modVersion in entity.ModVersionEntities)
                {
                    if (modVersion.ApproveModVersionEntity != null)
                    {
                        modVersion.ApproveModVersionEntity.User = null;
                        modVersion.ApproveModVersionEntity.Comments = null;
                        modVersion.ApproveModVersionEntity.ApprovedAt = null;
                    }
                }
            }

            if (avg != null)
            {
                entity.AVGPoint = Convert.ToDouble(((double)avg).ToString("0.00"));
            }
            return entity;
        }

        public ModEntity ModDetailUpd(string UserId, string ModId)
        {
            return _IDbContextServices.CreateContext(ReadOrWriteEnum.Read).ModEntity
                .Include(x => x.ModTypeEntities)
                .ThenInclude(x => x.Types)
                .Include(x => x.ModDependenceEntities)
                .ThenInclude(x => x.DependenceModVersion)
                .ThenInclude(x => x.Mod)
                .FirstOrDefault(x => x.ModId == ModId && x.CreatorUserId == UserId);
        }

        public bool? UpdateModInfo(ModEntity entity, string UserId)
        {
            var WriteContext = _IDbContextServices.CreateContext(ReadOrWriteEnum.Write);
            var Transaction = WriteContext.Database.BeginTransaction();
            var mod = WriteContext.ModEntity.FirstOrDefault(x => x.ModId == entity.ModId);
            var modtypes = WriteContext.ModTypeEntity.Where(x => x.ModId == entity.ModId).ToList();
            var ModDependencelist = WriteContext.ModDependenceEntity.Where(x => x.ModId == entity.ModId).ToList();
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
                WriteContext.RemoveRange(ModDependencelist);
                WriteContext.ModDependenceEntity.AddRange(entity.ModDependenceEntities);
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

        public async Task<List<ModEntity>?> ModListPageSearch(int Skip, int Take, string Search)
        {
            IQueryable<ModEntity> Context = _IDbContextServices.CreateContext(ReadOrWriteEnum.Read).ModEntity.Include(x => x.ModTypeEntities).ThenInclude(x => x.Types).Include(x => x.ModVersionEntities);
            Context = Context.Where(x => x.SoftDeleted == false);
            if (!string.IsNullOrWhiteSpace(Search))
            {
                Context = Context.Where(x => x.Name.Contains(Search));
            }
            Context = Context.Where(x =>
            x.ModVersionEntities.Any(y => y.ApproveModVersionEntity.Status == ((int)ApproveModVersionStatusEnum.Approved).ToString()) ||
            x.ModVersionEntities.Any(y => y.Status == ((int)ApproveModVersionStatusEnum.Approved).ToString()));
            return await Context.OrderByDescending(x => x.DownloadCount).ThenBy(x => x.CreatedAt).Skip(Skip).Take(Take).ToListAsync();
        }
    }
}
