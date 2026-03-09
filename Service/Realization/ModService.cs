using EF;
using EF.Interface;
using Entity.Approve;
using Entity.File;
using Entity.Mod;
using Entity.User;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Redis.Interface;
using Service.Interface;
using System.Diagnostics;
using System.Net.Http;
using System.Threading;
using System.Xml.Linq;
using ViewEntity.Mod;

namespace Service.Realization
{
    public class ModService : IModService
    {
        private readonly ICreateDBContextService _IDbContextServices;
        private readonly IRedisManageService _IRedisManageService;
        private readonly IConfiguration _configuration; // 新增

        public ModService(ICreateDBContextService iDbContextServices, IRedisManageService redisManageService, IConfiguration configuration)
        {
            _IDbContextServices = iDbContextServices;
            _IRedisManageService = redisManageService;
            _configuration = configuration;
        }

        public List<ModListViewEntity> ModListPage(dynamic json, string UserId)
        {
            // 解析输入（容错 dynamic）
            int skip = SafeToInt(json?.Skip);
            int take = SafeToInt(json?.Take);
            if (skip < 0) skip = 0;
            if (take <= 0) take = 10;
            string gameId = (string?)json?.GameId ?? string.Empty;
            string search = (string?)json?.Search ?? string.Empty;
            var types = ExtractTypes(json?.Types);

            bool canUseRedis = string.IsNullOrWhiteSpace(search)
                               && types.Count == 0
                               && (skip + take) <= 1000
                               && !string.IsNullOrWhiteSpace(gameId);

            // 尝试读取缓存
            List<ModListViewEntity>? cachedBase = canUseRedis
                ? _IRedisManageService.Get<List<ModListViewEntity>>($"ModListPage:{gameId}", 1)
                : null;

            // 用户订阅缓存（可选）
            List<UserModSubscribeEntity>? cachedUserSubs = !string.IsNullOrWhiteSpace(UserId)
                ? _IRedisManageService.Get<List<UserModSubscribeEntity>>($"SetUserModSubscribe:{UserId}", 1)
                : null;

            if (cachedBase == null && canUseRedis)
            {
                // 无阻塞预热缓存
                _ = EnsureModListCacheAsync(gameId);
            }

            if (cachedBase != null && canUseRedis)
            {
                return SliceFromRedis(cachedBase, skip, take, UserId, cachedUserSubs);
            }

            // 走数据库查询
            return QueryFromEF(gameId, skip, take, types, search, UserId);
        }

        #region 内部方法

        private List<ModListViewEntity> QueryFromEF(string gameId, int skip, int take, List<string> types, string search, string userId)
        {
            var ctx = _IDbContextServices.CreateContext(ReadOrWriteEnum.Read);

            IQueryable<ModEntity> query = ctx.ModEntity
                .Where(m => !m.SoftDeleted && m.GameId == gameId)
                .Where(m =>
                    m.ModVersionEntities.Any(v => v.ApproveModVersionEntity.Status == "20") ||
                    m.ModVersionEntities.Any(v => v.Status == "20"));

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(m => m.Name.Contains(search));
            }

            // AND 语义：每个类型都必须存在
            if (types.Count > 0)
            {
                foreach (var t in types)
                {
                    query = query.Where(m => m.ModTypeEntities.Any(mt => mt.TypesId == t));
                }
            }

            // 投影：避免过度 Include 导致笛卡尔乘积
            var projected = query
                .OrderByDescending(m => m.DownloadCount)
                .ThenBy(m => m.CreatedAt)
                .Skip(skip)
                .Take(take)
                .Select(m => new
                {
                    m.ModId,
                    m.Name,
                    m.PicUrl,
                    m.DownloadCount,
                    Types = m.ModTypeEntities.Select(mt => new { mt.TypesId, TypeName = mt.Types.TypeName }),
                    Avg = m.ModPointEntities.Any(p => p.Point.HasValue)
                        ? Math.Round(m.ModPointEntities.Where(p => p.Point.HasValue).Average(p => p.Point!.Value), 2)
                        : (double?)null,
                    IsSub = !string.IsNullOrWhiteSpace(userId) &&
                            m.UserModSubscribeEntities.Any(s => s.UserId == userId),
                    Creator = new
                    {
                        UserId = m.CreatorEntity != null ? m.CreatorEntity.UserId : null,
                        NickName = m.CreatorEntity != null ? m.CreatorEntity.NickName : null,
                        HeadPic = m.CreatorEntity != null ? m.CreatorEntity.HeadPic : null
                    }
                })
                .ToList();

            var list = new List<ModListViewEntity>(projected.Count);
            foreach (var m in projected)
            {
                list.Add(new ModListViewEntity
                {
                    ModId = m.ModId,
                    Name = m.Name,
                    PicUrl = m.PicUrl,
                    ModTypeEntities = m.Types.Select(t => new ModTypesListViewEntity
                    {
                        TypesId = t.TypesId,
                        TypeName = t.TypeName
                    }).ToList(),
                    IsMySubscribe = m.IsSub,
                    AVGPoint = m.Avg,
                    DownloadCount = m.DownloadCount,
                    CreatorUserId = m.Creator?.UserId,
                    CreatorNickName = m.Creator?.NickName,
                    CreatorHeadPic = m.Creator?.HeadPic
                });
            }
            return list;
        }

        private List<ModListViewEntity> SliceFromRedis(List<ModListViewEntity> cached, int skip, int take, string userId, List<UserModSubscribeEntity>? subs)
        {
            var slice = cached.Skip(skip).Take(take).ToList();
            if (string.IsNullOrWhiteSpace(userId) || slice.Count == 0)
                return slice;

            HashSet<string>? subSet = subs != null
                ? new HashSet<string>(subs.Where(s => !string.IsNullOrWhiteSpace(s.ModId)).Select(s => s.ModId!))
                : null;

            if (subSet != null)
            {
                foreach (var m in slice)
                {
                    if (m.ModId != null && subSet.Contains(m.ModId))
                        m.IsMySubscribe = true;
                }
            }
            else
            {
                // 若订阅缓存不存在，避免错误标记，保持默认 false/null
            }

            return slice;
        }

        private async Task EnsureModListCacheAsync(string gameId)
        {
            if (string.IsNullOrWhiteSpace(gameId)) return;
            // 双检：避免高并发重复构建
            if (_IRedisManageService.Get<List<ModListViewEntity>>($"ModListPage:{gameId}", 1) != null)
                return;

            var ctx = _IDbContextServices.CreateContext(ReadOrWriteEnum.Read);

            var baseQuery = ctx.ModEntity
                .Where(m => !m.SoftDeleted && m.GameId == gameId)
                .Where(m =>
                    m.ModVersionEntities.Any(v => v.ApproveModVersionEntity.Status == "20") ||
                    m.ModVersionEntities.Any(v => v.Status == "20"))
                .OrderByDescending(m => m.DownloadCount)
                .ThenBy(m => m.CreatedAt)
                .Take(1000)
                .Select(m => new
                {
                    m.ModId,
                    m.Name,
                    m.PicUrl,
                    m.DownloadCount,
                    Types = m.ModTypeEntities.Select(mt => new { mt.TypesId, TypeName = mt.Types.TypeName }),
                    Avg = m.ModPointEntities.Any(p => p.Point.HasValue)
                        ? Math.Round(m.ModPointEntities.Where(p => p.Point.HasValue).Average(p => p.Point!.Value), 2)
                        : (double?)null,
                    Creator = new
                    {
                        UserId = m.CreatorEntity != null ? m.CreatorEntity.UserId : null,
                        NickName = m.CreatorEntity != null ? m.CreatorEntity.NickName : null,
                        HeadPic = m.CreatorEntity != null ? m.CreatorEntity.HeadPic : null
                    }
                });

            var raw = await baseQuery.ToListAsync();
            var list = new List<ModListViewEntity>(raw.Count);
            foreach (var m in raw)
            {
                list.Add(new ModListViewEntity
                {
                    ModId = m.ModId,
                    Name = m.Name,
                    PicUrl = m.PicUrl,
                    ModTypeEntities = m.Types.Select(t => new ModTypesListViewEntity
                    {
                        TypesId = t.TypesId,
                        TypeName = t.TypeName
                    }).ToList(),
                    // 缓存不存订阅状态（与用户相关），仅保留平均分、下载数及作者信息
                    AVGPoint = m.Avg,
                    DownloadCount = m.DownloadCount,
                    CreatorUserId = m.Creator?.UserId,
                    CreatorNickName = m.Creator?.NickName,
                    CreatorHeadPic = m.Creator?.HeadPic
                });
            }

            await _IRedisManageService.SetAsync($"ModListPage:{gameId}", list, TimeSpan.FromMinutes(30), 1);
        }

        private static List<string> ExtractTypes(dynamic? dyn)
        {
            try
            {
                if (dyn == null) return new List<string>();
                if (dyn is JArray ja)
                {
                    return ja.ToObject<List<string>>()!
                             .Where(x => !string.IsNullOrWhiteSpace(x))
                             .Distinct()
                             .ToList();
                }
                return new List<string>();
            }
            catch
            {
                return new List<string>();
            }
        }

        private static int SafeToInt(object? v)
        {
            if (v == null) return 0;
            try
            {
                if (v is int i) return i;
                if (v is long l) return (int)l;
                if (v is string s && int.TryParse(s, out var r)) return r;
                return Convert.ToInt32(v);
            }
            catch { return 0; }
        }

        #endregion

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
                    ModTypeEntities = typesList,
                    DownloadCount = mod.DownloadCount,
                    CreatorUserId = mod.CreatorUserId,
                    CreatorNickName = mod.CreatorEntity?.NickName
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
            if (string.IsNullOrWhiteSpace(ModId))
            {
                return null;
            }

            var context = _IDbContextServices.CreateContext(ReadOrWriteEnum.Read);
            var cacheKey = $"ModDetail:{ModId}";

            var entity = _IRedisManageService.Get<ModEntity>(cacheKey, 1);
            if (entity == null)
            {
                entity = await QueryModDetailEntityAsync(context, ModId);
                if (entity == null)
                {
                    return null;
                }

                await _IRedisManageService.SetAsync(cacheKey, entity, TimeSpan.FromMinutes(10), 1);
            }

            var changedByModIo = await UpdateModDetailFrom_modio(entity.ModId, entity);
            if (changedByModIo)
            {
                entity = await QueryModDetailEntityAsync(context, ModId);
                if (entity == null)
                {
                    return null;
                }

                await _IRedisManageService.SetAsync(cacheKey, entity, TimeSpan.FromMinutes(10), 1);
            }

            var avgTask = context.ModPointEntity
                .Where(x => x.ModId == ModId)
                .Select(x => (double?)x.Point)
                .AverageAsync();

            var subscribeTask = context.UserModSubscribeEntity
                .AnyAsync(x => x.UserId == UserId && x.ModId == ModId);

            await Task.WhenAll(avgTask, subscribeTask);

            NormalizeModDetailEntity(entity, subscribeTask.Result, avgTask.Result);
            return entity;
        }

        private async Task<ModEntity> QueryModDetailEntityAsync(AllContext context, string modId)
        {
            return await context.ModEntity
                .IgnoreQueryFilters()
                .AsNoTracking()
                .AsSplitQuery()
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
                .FirstOrDefaultAsync(x => x.ModId == modId);
        }

        private static void NormalizeModDetailEntity(ModEntity entity, bool isSubscribed, double? avg)
        {
            if (entity == null)
            {
                return;
            }

            if (entity.CreatorEntity != null)
            {
                entity.CreatorEntity = new UserEntity
                {
                    UserId = entity.CreatorEntity.UserId,
                    NickName = entity.CreatorEntity.NickName
                };
            }

            entity.IsMySubscribe = isSubscribed;

            entity.ModVersionEntities = (entity.ModVersionEntities ?? new List<ModVersionEntity>())
                .Where(x => x.ApproveModVersionEntity != null && x.ApproveModVersionEntity.Status == "20")
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

            if (avg.HasValue)
            {
                entity.AVGPoint = Math.Round(avg.Value, 2);
            }
        }

        public async Task<bool> UpdateModDetailFrom_modio(string ModId, ModEntity Mod)
        {
            if (Mod == null || string.IsNullOrWhiteSpace(Mod.ModIdmodio) || string.IsNullOrWhiteSpace(Mod.GameIdmodio))
                return false;

            var modioCacheKey = $"modioDetail:{ModId}";
            var existed = _IRedisManageService.Get<string>(modioCacheKey, 3);
            if (!string.IsNullOrWhiteSpace(existed))
            {
                return false;
            }

            try
            {
                var gameIdEscaped = Uri.EscapeDataString(Mod.GameIdmodio);
                var modIdEscaped = Uri.EscapeDataString(Mod.ModIdmodio);
                var url = $"https://mod.io/v1/games/@{gameIdEscaped}/mods/@{modIdEscaped}/files?_sort=-date_added&_limit=100";

                using var http = new HttpClient();
                http.DefaultRequestHeaders.Accept.Clear();
                http.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

                var token = _configuration["ModIo:Token"];
                if (!string.IsNullOrWhiteSpace(token))
                {
                    http.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                }

                var resp = await http.GetAsync(url);
                if (!resp.IsSuccessStatusCode)
                {
                    return false;
                }

                var body = await resp.Content.ReadAsStringAsync();
                if (string.IsNullOrWhiteSpace(body))
                {
                    return false;
                }

                JToken parsed;
                try
                {
                    parsed = JToken.Parse(body);
                }
                catch
                {
                    return false;
                }

                JToken firstData = null;
                if (parsed is JObject obj && obj.TryGetValue("data", out var dataToken) && dataToken is JArray dataArr && dataArr.Count > 0)
                {
                    firstData = dataArr[0];
                }
                else if (parsed is JArray arr && arr.Count > 0)
                {
                    firstData = arr[0];
                }

                if (firstData == null)
                {
                    return false;
                }

                await _IRedisManageService.SetAsync(modioCacheKey, firstData.ToString(Newtonsoft.Json.Formatting.None), TimeSpan.FromDays(1), 3);

                string modioVersion = firstData.Type == JTokenType.Object ? (string)firstData["version"] : null;
                var currentVersion = Mod.ModVersionEntities?
                    .Where(v => !string.IsNullOrWhiteSpace(v.VersionNumber))
                    .OrderByDescending(v => v.CreatedAt)
                    .ThenByDescending(v => v.VersionNumber)
                    .FirstOrDefault()?.VersionNumber;

                if (string.IsNullOrWhiteSpace(modioVersion) || string.IsNullOrWhiteSpace(currentVersion) || string.Equals(modioVersion, currentVersion, StringComparison.Ordinal))
                {
                    return false;
                }

                var fullPath = await DownloadAndSaveFileFromJsonAsync(firstData.ToString(Newtonsoft.Json.Formatting.None));
                if (string.IsNullOrEmpty(fullPath))
                {
                    return false;
                }

                var fi = new System.IO.FileInfo(fullPath);
                var fileEntity = new FilesEntity
                {
                    FilesId = Guid.NewGuid().ToString(),
                    FilesType = Path.GetExtension(fullPath)?.TrimStart('.') ?? string.Empty,
                    FilesName = (string)firstData["filename"] ?? Path.GetFileName(fullPath),
                    Path = fullPath,
                    Size = fi.Length.ToString(),
                    CreatedAt = DateTime.Now
                };

                var modVersionEntity = new ModVersionEntity
                {
                    VersionId = Guid.NewGuid().ToString(),
                    ModId = Mod.ModId,
                    Description = (string)firstData["changelog"],
                    VersionNumber = modioVersion,
                    FilesId = fileEntity.FilesId,
                    Status = "20",
                    CreatedAt = DateTime.Now
                };

                var approveEntity = new ApproveModVersionEntity
                {
                    ApproveModVersionId = Guid.NewGuid().ToString(),
                    VersionId = modVersionEntity.VersionId,
                    UserId = "system",
                    ApprovedAt = DateTime.Now,
                    Status = "20",
                    Comments = "自动同步 mod.io 上的版本"
                };

                var db = _IDbContextServices.CreateContext(ReadOrWriteEnum.Write);
                db.FilesEntity.Add(fileEntity);
                db.ModVersionEntity.Add(modVersionEntity);
                db.ApproveModEntity.Add(approveEntity);
                await db.SaveChangesAsync();

                _IRedisManageService.Remove($"ModDetail:{ModId}", 1);
                return true;
            }
            catch
            {
                return false;
            }
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
            mod.GameIdmodio = entity.GameIdmodio;
            mod.ModIdmodio = entity.ModIdmodio;
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

        public List<ModVersionEntity> GetVersionsByModIds(List<string> modIds, DateTime? since)
        {
            if (modIds == null || modIds.Count == 0)
                return new List<ModVersionEntity>();

            var ctx = _IDbContextServices.CreateContext(ReadOrWriteEnum.Read);

            IQueryable<ModVersionEntity> q = ctx.ModVersionEntity
                .Include(v => v.Files)
                .Include(v => v.Mod)
                .Where(v => v.ModId != null && modIds.Contains(v.ModId));

            if (since.HasValue)
            {
                q = q.Where(v => v.CreatedAt.HasValue && v.CreatedAt > since.Value);
            }

            var list = q.OrderByDescending(v => v.CreatedAt).ThenByDescending(v => v.VersionNumber).ToList();

            // 清理返回结果中不要暴露的字段
            foreach (var v in list)
            {
                if (v.Mod != null)
                {
                    // 去除 Mod 中的版本列表，避免递归/多余数据
                    v.Mod.ModVersionEntities = null;
                }
                if (v.Files != null)
                {
                    // 隐藏文件的本地存储路径
                    v.Files.Path = null;
                }
            }

            return list;
        }

        public async Task<string?> DownloadAndSaveFileFromJsonAsync(string json, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(json)) return null;
            JToken token;
            try
            {
                token = JToken.Parse(json);
            }
            catch
            {
                return null;
            }

            // 尝试从多种可能的字段位置提取下载 URL（兼容旧格式和示例中的嵌套结构）
            string? binaryUrl = (string?)token["binary_url"]
                                ?? (string?)token["download"]?["binary_url"]
                                ?? (string?)token.SelectToken("download.binary_url")
                                ?? (string?)token.SelectToken("download.url")
                                ?? (string?)token["download_url"];

            if (string.IsNullOrWhiteSpace(binaryUrl))
                return null;

            // 尝试优先使用返回的 filename 字段
            string? filenameFromJson = (string?)token["filename"];
            long? fileSizeFromJson = token["filesize"]?.Value<long?>() ?? token["filesize_uncompressed"]?.Value<long?>();

            var guid = Guid.NewGuid().ToString();
            var filePathRoot = _configuration["FilePath"];
            if (string.IsNullOrWhiteSpace(filePathRoot)) return null;

            try
            {
                Directory.CreateDirectory(filePathRoot);

                using var http = new HttpClient();
                http.DefaultRequestHeaders.Accept.Clear();
                http.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/octet-stream"));

                using var resp = await http.GetAsync(binaryUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
                if (!resp.IsSuccessStatusCode) return null;

                // 先尝试从 filename 字段获取扩展名，否则从 URL 路径或 content-type 推断
                string ext = string.Empty;
                if (!string.IsNullOrWhiteSpace(filenameFromJson))
                {
                    ext = Path.GetExtension(filenameFromJson) ?? string.Empty;
                }

                if (string.IsNullOrEmpty(ext))
                {
                    try
                    {
                        ext = Path.GetExtension(new Uri(binaryUrl).LocalPath) ?? string.Empty;
                    }
                    catch
                    {
                        ext = string.Empty;
                    }
                }

                if (string.IsNullOrEmpty(ext))
                {
                    var mediaType = resp.Content.Headers.ContentType?.MediaType;
                    ext = GetExtensionFromContentType(mediaType) ?? string.Empty;
                }

                var fileName = (!string.IsNullOrWhiteSpace(filenameFromJson) ? Path.GetFileName(filenameFromJson) : guid + ext);
                if (string.IsNullOrWhiteSpace(Path.GetExtension(fileName)) && !string.IsNullOrEmpty(ext))
                    fileName = fileName + ext;

                var fullPath = Path.Combine(filePathRoot, guid + Path.GetExtension(fileName));

                using var stream = await resp.Content.ReadAsStreamAsync(cancellationToken);
                using var fs = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.None);
                await stream.CopyToAsync(fs, cancellationToken);

                return fullPath;
            }
            catch
            {
                return null;
            }
        }

        private static string? GetExtensionFromContentType(string? contentType)
        {
            if (string.IsNullOrWhiteSpace(contentType)) return null;
            return contentType.ToLowerInvariant() switch
            {
                "image/jpeg" => ".jpg",
                "image/png" => ".png",
                "image/gif" => ".gif",
                "application/zip" => ".zip",
                "application/octet-stream" => ".bin",
                "application/pdf" => ".pdf",
                "text/plain" => ".txt",
                "application/x-rar-compressed" => ".rar",
                _ => null
            };
        }
    }
}