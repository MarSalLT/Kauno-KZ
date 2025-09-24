using System.ComponentModel.DataAnnotations;

namespace KZ.Models
{
    public class SCSaveDataModel
    {
        [Required]
        public string Category { get; set; }
        [Required]
        public string Type { get; set; }
        [Required]
        public string Data { get; set; }
        [Required]
        public string DataUrl { get; set; }
        public string Subtype { get; set; }
        public string Id { get; set; }
    }
}