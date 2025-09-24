using System.ComponentModel.DataAnnotations;

namespace KZ.Models
{
    public class DeleteUserModel
    {
        [Required]
        public string Id { get; set; }
    }
}