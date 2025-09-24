using KZ.Models;
using System.Collections.Generic;

namespace KZ.Data
{
    public interface IUserRepository
    {
        Dictionary<string, object> GetUserData(string username);
        bool IsUserValid(LoginModel model);
        void RegisterUser(RegisterModel model);
    }
}