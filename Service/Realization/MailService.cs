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

        public MailService(IConfiguration configuration, IRedisManageService redisService)
        {
            _configuration = configuration;
            _redisService = redisService;
        }

        public async Task SendVerificationCodeAsync(string recipientEmail, string verificationCode)
        {
            try
            {
                // 检查邮箱是否已发送过验证码
                string redisKey = $"MailVerification:{recipientEmail}";
                if (await _redisService.KeyExistsAsync(redisKey))
                {
                    throw new InvalidOperationException("该邮箱已发送过验证码，请稍后再试。");
                }

                // 从配置文件中读取 Smtp 配置
                var smtpConfigs = _configuration.GetSection("Smtp").Get<SmtpConfig[]>();
                if (smtpConfigs == null || smtpConfigs.Length == 0)
                {
                    throw new InvalidOperationException("未找到任何 SMTP 配置信息");
                }

                // 随机选择一个 SMTP 配置
                var random = new Random();
                var selectedConfig = smtpConfigs[random.Next(smtpConfigs.Length)];

                using (var client = new SmtpClient(selectedConfig.Server))
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
                    VerificationCode = verificationCode
                };

                await _redisService.SetAsync(redisKey, mailInfo, TimeSpan.FromHours(24), 2);
            }
            catch (Exception ex)
            {
                // 记录日志或处理异常
                throw new InvalidOperationException("发送验证码失败", ex);
            }
        }

        private class SmtpConfig
        {
            public string Server { get; set; }
            public string SenderEmail { get; set; }
            public string SenderPassword { get; set; }
        }

        private class MailInfo
        {
            public string? RecipientEmail { get; set; }
            public DateTime? SentTime { get; set; }
            public string? Status { get; set; } // 0-已发送，100-已验证，200-已修改
            public string? VerificationCode { get; set; }
        }
    }
}
