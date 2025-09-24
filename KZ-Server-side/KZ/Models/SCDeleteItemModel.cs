using System.ComponentModel.DataAnnotations;

namespace KZ.Models
{
    public class SCDeleteItemModel
    {
        [Required]
        public string Category { get; set; }
        [Required]
        public string Id { get; set; }
    }
}