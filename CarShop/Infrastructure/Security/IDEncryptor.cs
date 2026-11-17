using System.Security.Cryptography;
using System.Text;

namespace CarShop.Infrastructure.Security
{
    // 身分證 AES-256 加解密（見 ARCHITECTURE 7.4）
    // 儲存格式：Base64( IV 16 bytes + 密文 )
    public class IDEncryptor
    {
        private const int IvSize = 16;
        private readonly IConfiguration _configuration;

        public IDEncryptor(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public string Encrypt(string identityNumber)
        {
            using var aes = CreateAes();

            // 每次加密都用新的隨機 IV，同一個身分證每次加密結果都不同
            byte[] iv = RandomNumberGenerator.GetBytes(IvSize);
            byte[] cipher = aes.EncryptCbc(Encoding.UTF8.GetBytes(identityNumber), iv);

            // IV 不是秘密，放在密文前面一起存，解密時取出來用
            return Convert.ToBase64String([.. iv, .. cipher]);
        }

        public string Decrypt(string cipherText)
        {
            using var aes = CreateAes();

            byte[] data = Convert.FromBase64String(cipherText);
            byte[] iv = data[..IvSize];
            byte[] cipher = data[IvSize..];

            return Encoding.UTF8.GetString(aes.DecryptCbc(cipher, iv));
        }

        // 讀取金鑰並建立 Aes，加密和解密共用
        private Aes CreateAes()
        {
            var aesKey = _configuration.GetValue<string>("Security:IdentityAesKey")
                ?? throw new InvalidOperationException("缺少設定 Security:IdentityAesKey");

            byte[] keyBytes = Convert.FromBase64String(aesKey);
            if (keyBytes.Length != 32)
            {
                throw new InvalidOperationException("Security:IdentityAesKey 必須是 32 bytes（AES-256）");
            }

            var aes = Aes.Create();
            aes.Key = keyBytes;
            return aes;
        }
    }
}
