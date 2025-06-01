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

        internal static string AdminUsername { get; set; }
        internal static byte[] AdminPassword { get; set; }

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
            AdminUsername = config.AdminUsername;
            AdminPassword = HashPassword(config.AdminPassword);
            DatabaseManager.dbPath = config.DatabasePath;
        }

        public static byte[] HashPassword( string password)
        {

            byte[] hashedPass = SHA512.HashData(Encoding.UTF8.GetBytes(password));
 
            return hashedPass;
        }


    }
}
