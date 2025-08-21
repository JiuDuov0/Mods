using Entity.Log;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Service.Interface;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Realization
{
    public class APILogService : IAPILogService
    {
        private readonly IConfiguration _IConfiguration;

        public APILogService(IConfiguration iConfiguration)
        {
            _IConfiguration = iConfiguration;
        }

        public async ValueTask WriteLogAsync(string API, string UserId, string IP)
        {
            if (string.IsNullOrWhiteSpace(API) || string.IsNullOrWhiteSpace(UserId) || string.IsNullOrWhiteSpace(IP))
                return;

            const string strsql = "insert into APILog(Id,API,UserId,IP,CreatedAt) values(@Id, @API, @UserId, @IP, @CreatedAt)";
            var sqlParameters = new[]
            {
        new SqlParameter("@Id", Guid.NewGuid()),
        new SqlParameter("@API", API),
        new SqlParameter("@UserId", UserId),
        new SqlParameter("@IP", IP),
        new SqlParameter("@CreatedAt", DateTime.Now)
    };

            await using var sqlConnection = new SqlConnection(_IConfiguration["WriteConnectionString"]);
            await sqlConnection.OpenAsync().ConfigureAwait(false);
            await using var sqlCommand = new SqlCommand(strsql, sqlConnection)
            {
                CommandType = CommandType.Text
            };
            sqlCommand.Parameters.AddRange(sqlParameters);
            await sqlCommand.ExecuteNonQueryAsync().ConfigureAwait(false);
            sqlCommand.Parameters.Clear();
        }

        /// <summary>
        /// 显示最近一段时间的登录日志
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <returns></returns>
        public async Task<List<APILogEntity>> GetLoginLogsAsync(DateTime start, DateTime end)
        {
            var result = new List<APILogEntity>();
            const string strsql = @"
        SELECT t.Id, t.API, t.UserId, t.IP, t.CreatedAt
        FROM (
            SELECT
                l.Id, l.API, l.UserId, l.IP, l.CreatedAt,
                ROW_NUMBER() OVER (PARTITION BY l.UserId ORDER BY l.CreatedAt DESC) AS rn
            FROM APILog l
            INNER JOIN [User] u ON l.UserId = u.UserId
            WHERE l.CreatedAt >= @Start AND l.CreatedAt < @End AND l.API <> @ExcludeAPI
        ) t
        WHERE t.rn = 1";

            var sqlParameters = new[]
            {
        new SqlParameter("@Start", start),
        new SqlParameter("@End", end),
        new SqlParameter("@ExcludeAPI", "LoginController/UserLogin")
    };

            await using var sqlConnection = new SqlConnection(_IConfiguration["ReadConnectionString"]);
            await sqlConnection.OpenAsync().ConfigureAwait(false);
            await using var cmd = new SqlCommand(strsql, sqlConnection)
            {
                CommandType = CommandType.Text
            };
            cmd.Parameters.AddRange(sqlParameters);
            await using var reader = await cmd.ExecuteReaderAsync().ConfigureAwait(false);
            while (await reader.ReadAsync().ConfigureAwait(false))
            {
                var entity = new APILogEntity
                {
                    Id = reader["Id"]?.ToString(),
                    API = reader["API"]?.ToString(),
                    UserId = reader["UserId"]?.ToString(),
                    IP = reader["IP"]?.ToString(),
                    CreatedAt = reader["CreatedAt"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["CreatedAt"])
                };
                result.Add(entity);
            }
            return result;
        }

        /// 显示最近 N 天（最多 30 天）内每天流失的用户数量
        /// </summary>
        /// <param name="days">统计天数（1~30）</param>
        /// <returns>每天流失用户数量字典，key为日期字符串，value为数量</returns>
        public async Task<Dictionary<string, int>> GetLostUsersAsync(int days)
        {
            if (days < 1) days = 1;
            if (days > 30) days = 30;
            var today = DateTime.Now.Date;
            var start = today.AddDays(-days + 1);

            // 获取所有用户ID和注册时间（CreatedAt）
            var userInfoDict = new Dictionary<string, DateTime>();
            const string userSql = "SELECT UserId, CreatedAt FROM [User]";
            await using (var userConn = new SqlConnection(_IConfiguration["ReadConnectionString"]))
            {
                await userConn.OpenAsync().ConfigureAwait(false);
                await using (var userCmd = new SqlCommand(userSql, userConn))
                await using (var userReader = await userCmd.ExecuteReaderAsync().ConfigureAwait(false))
                {
                    while (await userReader.ReadAsync().ConfigureAwait(false))
                    {
                        var userId = userReader["UserId"]?.ToString();
                        var regTime = userReader["CreatedAt"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(userReader["CreatedAt"]);
                        if (!string.IsNullOrEmpty(userId) && regTime.HasValue)
                            userInfoDict[userId] = regTime.Value.Date;
                    }
                }
            }

            // 查询所有用户的最后一次登录时间
            var lastLoginDict = new Dictionary<string, DateTime>();
            const string loginSql = "SELECT UserId, MAX(CreatedAt) AS LastLogin FROM APILog WHERE API = @API GROUP BY UserId";
            var loginParams = new[]
            {
        new SqlParameter("@API", "LoginController/UserLogin")
    };
            await using (var sqlConnection = new SqlConnection(_IConfiguration["ReadConnectionString"]))
            {
                await sqlConnection.OpenAsync().ConfigureAwait(false);
                await using (var cmd = new SqlCommand(loginSql, sqlConnection))
                {
                    cmd.Parameters.AddRange(loginParams);
                    await using (var reader = await cmd.ExecuteReaderAsync().ConfigureAwait(false))
                    {
                        while (await reader.ReadAsync().ConfigureAwait(false))
                        {
                            var userId = reader["UserId"]?.ToString();
                            var lastLogin = reader["LastLogin"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["LastLogin"]);
                            if (!string.IsNullOrEmpty(userId) && lastLogin.HasValue)
                                lastLoginDict[userId] = lastLogin.Value.Date;
                        }
                    }
                }
            }

            // 统计每天累计流失用户数
            var result = new Dictionary<string, int>(days);
            var lostUserIds = new HashSet<string>();
            for (int i = 0; i < days; i++)
            {
                var date = start.AddDays(i);
                foreach (var kvp in userInfoDict)
                {
                    var userId = kvp.Key;
                    if (lostUserIds.Contains(userId))
                        continue;

                    DateTime baseTime;
                    if (lastLoginDict.TryGetValue(userId, out baseTime))
                    {
                        // 有登录记录，按最后登录时间判定
                        if (date >= baseTime.AddDays(30))
                            lostUserIds.Add(userId);
                    }
                    else
                    {
                        // 无登录记录，按注册时间（CreatedAt）判定
                        if (date >= kvp.Value.AddDays(30))
                            lostUserIds.Add(userId);
                    }
                }
                result[date.ToString("yyyy-MM-dd")] = lostUserIds.Count;
            }

            return result;
        }

        /// <summary>
        /// 查询每天去重后的活跃用户数
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <returns></returns>
        public async Task<Dictionary<string, int>> GetDailyActiveUserCountAsync(DateTime start, DateTime end)
        {
            const string strsql = @"
        SELECT 
            CAST(CreatedAt AS DATE) AS Day,
            COUNT(DISTINCT UserId) AS UserCount
        FROM APILog
        WHERE CreatedAt >= @Start AND CreatedAt < @End
          AND API NOT IN (@ExcludeAPI1, @ExcludeAPI2, @ExcludeAPI3)
          AND UserId IS NOT NULL AND UserId <> ''
        GROUP BY CAST(CreatedAt AS DATE)
        ORDER BY Day ASC";

            var sqlParameters = new[]
            {
        new SqlParameter("@Start", start),
        new SqlParameter("@End", end),
        new SqlParameter("@ExcludeAPI1", "LoginController/UserLogin"),
        new SqlParameter("@ExcludeAPI2", "LoginController/UserRegister"),
        new SqlParameter("@ExcludeAPI3", "LoginController/RefreshToken")
    };

            var result = new Dictionary<string, int>();
            await using var sqlConnection = new SqlConnection(_IConfiguration["ReadConnectionString"]);
            await sqlConnection.OpenAsync().ConfigureAwait(false);
            await using var cmd = new SqlCommand(strsql, sqlConnection)
            {
                CommandType = CommandType.Text
            };
            cmd.Parameters.AddRange(sqlParameters);
            await using var reader = await cmd.ExecuteReaderAsync().ConfigureAwait(false);
            while (await reader.ReadAsync().ConfigureAwait(false))
            {
                string day = reader["Day"] != DBNull.Value
                    ? Convert.ToDateTime(reader["Day"]).ToString("yyyy-MM-dd")
                    : string.Empty;
                int count = reader["UserCount"] == DBNull.Value ? 0 : Convert.ToInt32(reader["UserCount"]);
                if (!string.IsNullOrEmpty(day))
                    result[day] = count;
            }
            return result;
        }
    }
}
