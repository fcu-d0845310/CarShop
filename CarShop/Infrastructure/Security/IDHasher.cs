using System.Security.Cryptography;
using System.Text;

namespace CarShop.Infrastructure.Security
{
    // 身分證的 HMAC-SHA256 雜湊，用來檢查身分證是否重複（見 ARCHITECTURE 7.4）
    public class IDHasher
    {
        private readonly IConfiguration _configuration;

        public IDHasher(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public string Hash(string identityNumber)
        {
            var hmacKey = _configuration.GetValue<string>("Security:IdentityHmacKey")
                ?? throw new InvalidOperationException("缺少設定 Security:IdentityHmacKey");

            byte[] keyBytes = Convert.FromBase64String(hmacKey);
            byte[] dataBytes = Encoding.UTF8.GetBytes(identityNumber);
            byte[] hashBytes = HMACSHA256.HashData(keyBytes, dataBytes);

            // 32 bytes轉64 個 16 進位字元
            return Convert.ToHexString(hashBytes);
        }
    }
}
