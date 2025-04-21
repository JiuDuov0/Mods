using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Interface
{
    public interface IMailService
    {
        public Task SendVerificationCodeAsync(string recipientEmail, string verificationCode);
    }
}
