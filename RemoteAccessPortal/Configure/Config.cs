using RemoteAccessPortal;
using System.Text.Json;
using RemoteAccessPortal.Database;
using RemoteAccessPortal.Dashboard;
using RemoteAccessPortal.Classes;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;



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
            try
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
                DatabaseManager.dbPath = config.DatabasePath;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to load config file.", ex);
            }
        }

        internal static async Task<string> GetAdminAuthKey()
        {
            try
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
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to get admin auth key.", ex);
            }
        }

        internal static async Task<string> GetAdminUserHash()
        {
            try
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
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to get admin user hash.", ex);
            }
        }

        public static string HashString(string password)
        {
            try
            {
                string hashedPass = Convert.ToBase64String(SHA512.HashData(Encoding.UTF8.GetBytes(password)));

                return hashedPass;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static string CapitalizeFirstLetter(string input)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(input))
                    return input;

                return char.ToUpper(input[0]) + input.Substring(1);
            }
            catch (Exception)
            {

                return input;
            }
        }

        public static async Task InitializeAdmin()
        {
            try
            {

                string json = File.ReadAllText(ConfigFilePath);

                DeviceConfig config = JsonSerializer.Deserialize<DeviceConfig>(json);

                if (config == null)
                {
                    throw new InvalidOperationException("Failed to deserialize node config.");
                }


                string adminPassHash = HashPassword(config.AdminPassword);
                User adminUser = new User
                {

                    Username = config.AdminUsername,
                    Name = "Admin",
                    UserHash = HashString(config.AdminUsername),
                    PasswordHash = adminPassHash,
                    AuthKey = HashString(config.AdminUsername + "|" + config.AdminPassword),
                    IsAdmin = true

                };
                Console.WriteLine($"Admin PassHash from saving. : {adminUser.PasswordHash}");
                await DatabaseManager.InsertUser(adminUser);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to initialize admin user.", ex);

            }
        }

        public static string HashPassword(string password)
        {
            try
            {
                string prehashedPass = Convert.ToBase64String(SHA512.HashData(Encoding.UTF8.GetBytes(password)));

                using var rng = RandomNumberGenerator.Create();
                byte[] salt = new byte[16];
                rng.GetBytes(salt);

                using var pbkdf2 = new Rfc2898DeriveBytes(prehashedPass, salt, 100000, HashAlgorithmName.SHA256);
                byte[] hash = pbkdf2.GetBytes(32);

                byte[] hashBytes = new byte[48];
                Array.Copy(salt, 0, hashBytes, 0, 16);
                Array.Copy(hash, 0, hashBytes, 16, 32);

                return Convert.ToBase64String(hashBytes);
            }
            catch (Exception)
            {
                return null; 
            }
        }

        public static bool VerifyPassword(string password, string storedHash)
        {
            try
            {
                byte[] hashBytes = Convert.FromBase64String(storedHash);

                if (hashBytes.Length != 48)
                {
                    return false;
                }

                byte[] salt = new byte[16];
                Array.Copy(hashBytes, 0, salt, 0, 16);

                using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, 100000, HashAlgorithmName.SHA256);
                byte[] hash = pbkdf2.GetBytes(32);

                bool isMatch = true;
                for (int i = 0; i < 32; i++)
                {
                    if (hashBytes[i + 16] != hash[i]) isMatch = false;
                }

                return isMatch;
            }
            catch (FormatException)
            {
                return false;
            }
            catch (ArgumentNullException)
            {
                return false;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
