using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Interface
{
    public interface IMailService
    {
        public Task<string> SendVerificationCodeAsync(string recipientEmail, string IP);
        public Task<bool> VerifyEmailCodeAsync(string recipientEmail, string verificationCode);
    }
}
