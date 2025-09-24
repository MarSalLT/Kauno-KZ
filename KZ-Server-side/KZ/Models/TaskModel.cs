using System.ComponentModel.DataAnnotations;

namespace KZ.Models
{
    public class TaskModel
    {
        [Required]
        public string Id { get; set; }
        public bool? CommentsChanged { get; set; }
        public string Timestamp { get; set; }
    }
}