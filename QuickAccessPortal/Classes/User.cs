using Microsoft.AspNetCore.Identity;
using RemoteAccessPortal.Config;

namespace RemoteAccessPortal.Classes
{
    public class User
    {
        public string Name { get; set; }
        public string Username { get; set; }
        public string UserHash { get; set; }
        public string PasswordHash { get; set; }
        public string ApiKey { get; set; }
        public bool IsAdmin { get; set; }



        public static async Task<User> CreateUser(string username, string name, string password, bool isAdmin=false)
        {
            string userHash = Config.Config.HashString(username);
            string authKey = Config.Config.HashString(username + "|" + password);

            User user = new User
            {
                Username = username,
                Name = name,
                UserHash = userHash,
                PasswordHash = Config.Config.HashPassword(password),
                ApiKey = authKey,
                IsAdmin = isAdmin
            };

            return user;
        }
    }
}
