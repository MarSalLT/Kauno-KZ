using System.ComponentModel.DataAnnotations;

namespace KZ.Models
{
    public class UserModel
    {
        public string Id { get; set; }
        [Required]
        public string Name { get; set; }
        [Required]
        public string Role { get; set; }
        public string Password { get; set; }
        public string Email { get; set; }
        public string ClientName { get; set; }
        public string ClientEnterprise { get; set; }
    }
}