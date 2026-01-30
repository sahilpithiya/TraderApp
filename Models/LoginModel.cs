using System;
using System.Collections.Generic;

namespace TraderApps.Models
{
    public class LoginModel
    {
        public string UserId { get; set; }
        public string Password { get; set; } // Encrypted
        public string LicenseId { get; set; }
        public bool IsRememberMe { get; set; }
        public DateTime LastLoginTime { get; set; }
    }
}