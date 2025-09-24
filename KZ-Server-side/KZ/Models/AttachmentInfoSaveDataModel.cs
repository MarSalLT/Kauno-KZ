using System.ComponentModel.DataAnnotations;

namespace KZ.Models
{
    public class AttachmentInfoSaveDataModel
    {
        [Required]
        public string AttachmentId { get; set; }
        [Required]
        public string Data { get; set; }
    }
}