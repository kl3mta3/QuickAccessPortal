using RemoteAccessPortal;
using System.Text.Json;
using RemoteAccessPortal.Database;
using RemoteAccessPortal.Dashboard;
using RemoteAccessPortal.Classes;
using System.Security.Cryptography;
using System.Text;



namespace RemoteAccessPortal.Config
{
    public class Config
    {
        internal static string ConfigFilePath = "app.config";
        internal static string LogFilePath = "log.txt";

        internal static string AdminUserHash { get; set; }
        internal static string AdminAuthKey { get; set; }

        public static async Task ConfigureApplication()
        {
            if (!File.Exists(ConfigFilePath))
            {
                File.Create(ConfigFilePath).Close();
            }
            else
            {
                await LoadConfigFile();
            }

            if (!File.Exists(LogFilePath))
            { 
            File.Create(LogFilePath).Close();
            }
        }

        private static async Task LoadConfigFile()
        {
            string json = File.ReadAllText(ConfigFilePath);

            DeviceConfig config = JsonSerializer.Deserialize<DeviceConfig>(json);

            if (config == null)
            {
                throw new InvalidOperationException("Failed to deserialize node config.");
            }

            WebAppDashboard.WebAppIP = config.ServerURL;
            WebAppDashboard.WebAppPort = config.ServerPort;
            WebAppDashboard.ClientPort = config.ClientPort;
            AdminUserHash = HashString(config.AdminUsername);
            //AdminAuthKey = HashString(config.AdminUsername + "|" + config.AdminPassword);
            DatabaseManager.dbPath = config.DatabasePath;
        }

        internal static async Task<string> GetAdminAuthKey()
        {
            string json = File.ReadAllText(ConfigFilePath);

            DeviceConfig config = JsonSerializer.Deserialize<DeviceConfig>(json);

            if (config == null)
            {
                throw new InvalidOperationException("Failed to deserialize node config.");
            }

            string adminAuthKey = HashString(config.AdminUsername + "|" + config.AdminPassword);

            return adminAuthKey;
        }

        internal static async Task<string> GetAdminUserHash()
        {
            string json = File.ReadAllText(ConfigFilePath);

            DeviceConfig config = JsonSerializer.Deserialize<DeviceConfig>(json);

            if (config == null)
            {
                throw new InvalidOperationException("Failed to deserialize node config.");
            }
            string adminUserHash = HashString(config.AdminUsername);
        

            return adminUserHash;
        }

        public static string HashString(string password)
        {

            string hashedPass = Convert.ToBase64String(SHA512.HashData(Encoding.UTF8.GetBytes(password)));
 
            return hashedPass;
        }

        public static string CapitalizeFirstLetter(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return input;

            return char.ToUpper(input[0]) + input.Substring(1);
        }

        public static async Task InitializeAdmin()
        {


            string json = File.ReadAllText(ConfigFilePath);

            DeviceConfig config = JsonSerializer.Deserialize<DeviceConfig>(json);

            if (config == null)
            {
                throw new InvalidOperationException("Failed to deserialize node config.");
            }

            User adminUser = new User
            {

                Username = config.AdminUsername,
               
                Name = "Admin",
                UserHash = HashString(config.AdminUsername),
                AuthKey = HashString(config.AdminUsername + "|" + config.AdminPassword),
                IsAdmin = true

            };

            await DatabaseManager.AddUserToDatabase(adminUser);


        }
    }
}
