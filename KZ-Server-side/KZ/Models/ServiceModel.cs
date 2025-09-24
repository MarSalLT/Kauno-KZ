using System.ComponentModel.DataAnnotations;

namespace KZ.Models
{
    public class ServiceModel
    {
        [Required]
        public string Url { get; set; }
    }
}