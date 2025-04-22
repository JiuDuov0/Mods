using EF.Interface;
using MailKit.Net.Smtp;
using MimeKit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Redis.Interface;
using Service.Interface;
using System;
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

            // 使用 MailKit 发送邮件
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("ModCat", selectedConfig.SenderEmail));
            message.To.Add(new MailboxAddress("", recipientEmail));
            message.Subject = "邮箱验证码";
            message.Body = new TextPart("plain")
            {
                Text = $"您的验证码是：{verificationCode}，请在10分钟内使用。"
            };

            using (var client = new SmtpClient())
            {
                try
                {
                    await client.ConnectAsync(selectedConfig.Server, selectedConfig.SmtpPort, MailKit.Security.SecureSocketOptions.SslOnConnect);
                    await client.AuthenticateAsync(selectedConfig.SenderEmail, selectedConfig.SenderPassword);
                    await client.SendAsync(message);
                    await client.DisconnectAsync(true);
                }
                catch (SmtpCommandException ex)
                {
                    return $"SMTP 命令错误: {ex.Message} (Code: {ex.StatusCode})";
                }
                catch (SmtpProtocolException ex)
                {
                    return $"SMTP 协议错误: {ex.Message}";
                }
                catch (IOException ex)
                {
                    return $"IO 错误: {ex.Message}";
                }
                catch (Exception ex)
                {
                    return $"未知错误: {ex.Message}";
                }
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

        public Task<bool> UpdateCatchState(string recipientEmail)
        {
            string redisKey = $"MailVerification:{recipientEmail}";
            // 检查缓存中是否存在验证码
            var mailInfo = _redisService.Get<MailInfo>(redisKey, 2);
            if (mailInfo == null)
            {
                return Task.FromResult(false); // 验证失败
            }
            // 更新状态为已修改（200）
            mailInfo.Status = "200";
            _redisService.Set(redisKey, mailInfo, TimeSpan.FromHours(24), 2); // 重置过期时间为24小时
            return Task.FromResult(true); // 更新成功
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
            if (mailInfo.Status != "0" || mailInfo.SentTime.Value.AddMinutes(10) < DateTime.Now)
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
