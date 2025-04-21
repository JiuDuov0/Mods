using EF.Interface;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.Extensions.Configuration;
using Redis.Interface;
using Service.Interface;
using System;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

namespace Service.Realization
{
    public class MailService : IMailService
    {
        private readonly IConfiguration _configuration;
        private readonly IRedisManageService _redisService;
        private readonly ICreateDBContextService _IDbContextServices;

        public MailService(IConfiguration configuration, IRedisManageService redisService, ICreateDBContextService iDbContextServices)
        {
            _configuration = configuration;
            _redisService = redisService;
            _IDbContextServices = iDbContextServices;
        }

        public async Task<string> SendVerificationCodeAsync(string recipientEmail, string IP)
        {
            var verificationCode = GenerateRandomCode();

            // 检查用户中是否有此邮箱
            var user = await _IDbContextServices.CreateContext(EF.ReadOrWriteEnum.Read).UserEntity.FirstOrDefaultAsync(x => x.Mail == recipientEmail);
            if (user == null)
            {
                return "未找到该用户。";
            }

            // 检查邮箱是否已发送过验证码
            string redisKey = $"MailVerification:{recipientEmail}";
            if (await _redisService.KeyExistsAsync(redisKey, 2))
            {
                return "该邮箱已发送过验证码，请24小时后再试。";
            }

            // 从配置文件中读取 Smtp 配置
            var smtpConfigs = _configuration.GetSection("Smtp").Get<SmtpConfig[]>();
            if (smtpConfigs == null || smtpConfigs.Length == 0)
            {
                return "未找到任何 SMTP 配置信息";
            }

            // 随机选择一个 SMTP 配置
            var random = new Random();
            var selectedConfig = smtpConfigs[random.Next(smtpConfigs.Length)];

            using (var client = new SmtpClient(selectedConfig.Server, selectedConfig.SmtpPort))
            {
                client.Credentials = new NetworkCredential(selectedConfig.SenderEmail, selectedConfig.SenderPassword);
                client.EnableSsl = true;

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(selectedConfig.SenderEmail),
                    Subject = "邮箱验证码",
                    Body = $"您的验证码是：{verificationCode}，请在10分钟内使用。",
                    IsBodyHtml = false
                };

                mailMessage.To.Add(recipientEmail);

                await client.SendMailAsync(mailMessage);
            }

            // 将验证码和状态信息存入 Redis，设置 24 小时过期时间
            var mailInfo = new MailInfo
            {
                RecipientEmail = recipientEmail,
                SentTime = DateTime.Now,
                Status = "0", // 已发送
                IP = IP,
                VerificationCode = verificationCode
            };

            await _redisService.SetAsync(redisKey, mailInfo, TimeSpan.FromHours(24), 2);
            return string.Empty; // 返回验证码
        }

        public async Task<bool> VerifyEmailCodeAsync(string recipientEmail, string verificationCode)
        {
            string redisKey = $"MailVerification:{recipientEmail}";

            // 检查缓存中是否存在验证码
            var mailInfo = await _redisService.GetAsync<MailInfo>(redisKey, 2);
            if (mailInfo == null || mailInfo.VerificationCode != verificationCode)
            {
                return false; // 验证失败
            }

            // 更新状态为已验证（100）
            mailInfo.Status = "100";
            await _redisService.SetAsync(redisKey, mailInfo, TimeSpan.FromHours(24), 2); // 重置过期时间为24小时

            return true; // 验证成功
        }

        private string GenerateRandomCode()
        {
            var random = new Random();
            return random.Next(100000, 999999).ToString(); // 生成 6 位随机数字
        }

        private class SmtpConfig
        {
            public string Server { get; set; }
            public string SenderEmail { get; set; }
            public int SmtpPort { get; set; }
            public string SenderPassword { get; set; }
        }

        private class MailInfo
        {
            public string? RecipientEmail { get; set; }
            public DateTime? SentTime { get; set; }
            public string? Status { get; set; } // 0-已发送，100-已验证，200-已修改
            public string? IP { get; set; }
            public string? VerificationCode { get; set; }
        }
    }
}
