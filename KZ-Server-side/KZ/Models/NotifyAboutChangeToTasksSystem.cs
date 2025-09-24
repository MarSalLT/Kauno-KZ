using System.ComponentModel.DataAnnotations;

namespace KZ.Models
{
    public class NotifyAboutChangeToTasksSystem
    {
        [Required]
        public int EnterpriseId { get; set; }
        [Required]
        public string Id { get; set; }
        public bool Attachment { get; set; }
    }
}