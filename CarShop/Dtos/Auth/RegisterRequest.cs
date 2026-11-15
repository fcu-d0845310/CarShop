using System.ComponentModel.DataAnnotations;

namespace CarShop.Dtos.Auth
{
    public class RegisterRequest
    {
        [Required]
        [EmailAddress]
        [MaxLength(255)]
        public string Email {  get; set; } = string.Empty;
        [Required]
        [StringLength(64, MinimumLength = 8)]
        [RegularExpression("^(?=.*[A-Za-z])(?=.*\\d).*$",ErrorMessage = "密碼需包含英文字母與數字")]
        public string Password { get; set; } = string.Empty;
        [Required]
        [MaxLength(50)]
        public string Name { get; set; } = string.Empty;
        [Required]
        [MaxLength(10)]
        public string Phone { get; set; } = string.Empty;
        [Required]
        public string IdentityNumber { get; set; } = string.Empty;
        [Required]
        [MaxLength(100)]
        public string Address { get; set; } = string.Empty;
        [Range(typeof(bool), "true", "true", ErrorMessage = "您必須同意服務條款才能註冊")]
        public bool AgreePrivacy {  get; set; }
    }
}
