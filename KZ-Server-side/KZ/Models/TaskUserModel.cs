using System.ComponentModel.DataAnnotations;

namespace KZ.Models
{
    public class TaskUserModel
    {
        [Required]
        public string Password { get; set; }
        [Required]
        public int? TerritoryCode { get; set; }
        public TaskUserModel(string password, int? territoryCode)
        {
            Password = password;
            TerritoryCode = territoryCode;
        }
    }
}