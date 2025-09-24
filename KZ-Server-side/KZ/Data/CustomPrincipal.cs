using System.Collections.Generic;
using System.Security.Principal;

namespace KZ.Data
{
    public class CustomPrincipal : IPrincipal
    {
        public IIdentity Identity { get; set; }
        public string Role { get; set; }
        private List<string> Permissions { get; set; }
        public string Email { get; set; }
        public int? TerritoryCode { get; set; }
        private IUserRepository UserRepository { get; set; }
        public CustomPrincipal(string username, string role, List<string> permissions, string email, IUserRepository userRepository)
        {
            Identity = new GenericIdentity(username);
            Role = role;
            Permissions = permissions;
            Email = email;
            UserRepository = userRepository;
        }
        public CustomPrincipal(string username, string role, int? territoryCode)
        {
            Identity = new GenericIdentity(username);
            Role = role;
            TerritoryCode = territoryCode;
        }
        public bool IsInRole(string permission)
        {
            string[] permissions = permission.Split(',');
            foreach (string p in permissions)
            {
                if (Permissions.Contains(p))
                {
                    return true;
                }
            }
            return false;
        }
    }
}