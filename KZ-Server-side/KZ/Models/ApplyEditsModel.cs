using System.ComponentModel.DataAnnotations;

namespace KZ.Models
{
    public class ApplyEditsModel
    {
        [Required]
        public string ServiceId { get; set; }
        [Required]
        public string Edits { get; set; }
    }
}