using System.ComponentModel.DataAnnotations;

namespace KZ.Models
{
    public class TaskApprovalOrRejectionModel
    {
        [Required]
        public string Id { get; set; }
        [Required]
        public string Status { get; set; }
        public string Reason { get; set; }
    }
}
