using Microsoft.Data.Sqlite;
using System.Net.Sockets;
using RemoteAccessPortal.Classes;
using RemoteAccessPortal.Config;
using RemoteAccessPortal.Dashboard;
using Microsoft.AspNetCore.Http.HttpResults;
using System.Xml.Linq;
using System.Reflection.PortableExecutable;

namespace RemoteAccessPortal.Database
{


    public class DatabaseManager
    {

        internal static string dbPath = "client.db";
        internal static List<Client> Clients { get; set; } = new List<Client>();
        private static readonly object _clientLock = new();

        // Initialize the database and create tables if they do not exist
        public static async Task Initialize()
        {
            try
            {
                using var connection = new SqliteConnection($"Data Source={dbPath}");
                connection.Open();

                var command = connection.CreateCommand();
                command.CommandText = @"
            CREATE TABLE IF NOT EXISTS ClientList (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                ClientName TEXT,
                Timestamp TEXT,
                AddedBy TEXT,
                KbaUrl TEXT,
                RemoteLocation TEXT 

            );

            CREATE TABLE IF NOT EXISTS ClientAlerts (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                AlertID TEXT,
                ClientName TEXT,
                AddedBy TEXT,
                AddedOn TEXT,
                ResolvedOn TEXT,
                Status TEXT,
                Resolution TEXT,
                Message TEXT
            );
                

                CREATE TABLE IF NOT EXISTS Users(
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Name TEXT,
                Username TEXT,
                UserHash TEXT,
                PasswordHash TEXT,
                ApiKey TEXT,
                IsAdmin BOOL
            ); ";


                command.ExecuteNonQuery();


                Dictionary<string, string> clientData = await DatabaseManager.GetClientDictAsync();
                List<Alert> currentAlerts = await DatabaseManager.GetCurrentAlerts();



                if (clientData == null || clientData.Count == 0)
                {
                    await SeedRandomClients(20);
                }

                if (currentAlerts == null || currentAlerts.Count == 0)
                {
                    await SeedFakeAlerts();
                }

                string adminUserHash = await Config.Config.GetAdminUserHash();
                User user = await GetUserByUserHash(adminUserHash);
                if (user==null)
                {

                    await Config.Config.InitializeAdmin();
                }

            }
            catch (Exception ex)
            {
                throw;
            }
        }


        // Client DB manager methods
        public static async Task InsertClient(string clientName, string addedBy, string kbaUrl, string remoteLocation)
        {
            try
            {
                Client client = GetClient(clientName);
                if (client != null)
                {
                    throw new InvalidOperationException($"Client with name '{clientName}' already exists.");
                }

                using var connection = new SqliteConnection($"Data Source={dbPath}");

                await connection.OpenAsync();
                var command = connection.CreateCommand();
                command.CommandText = @"
                    INSERT INTO ClientList (ClientName, Timestamp, AddedBy, KbaUrl, RemoteLocation)
                    VALUES ($clientName, $timestamp, $addedBy, $kbaUrl, $remoteLocation)";
                command.Parameters.AddWithValue("$clientName", clientName);
                command.Parameters.AddWithValue("$timestamp", DateTime.UtcNow.ToString("o"));
                command.Parameters.AddWithValue("$addedBy", addedBy);
                command.Parameters.AddWithValue("$kbaUrl", kbaUrl);
                command.Parameters.AddWithValue("$remoteLocation", remoteLocation);

                await command.ExecuteNonQueryAsync();

                var freshClients = await DatabaseManager.GetAllClients();

                lock (_clientLock)
                {
                    Clients = freshClients;
                }

            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public static async Task UpdateClient(Client client)
        {
            try
            {

                Console.WriteLine($"Updating client: {client.ClientName} at {dbPath}");
                using var connection = new SqliteConnection($"Data Source={dbPath}");
                await connection.OpenAsync();
                var command = connection.CreateCommand();
                command.CommandText = @"
                UPDATE ClientList
                SET 
                    Timestamp = @timestamp,
                    KbaUrl = @kbaUrl,
                    RemoteLocation = @remoteLocation
                WHERE ClientName = @clientName";

                command.Parameters.AddWithValue("@clientName", client.ClientName);
                command.Parameters.AddWithValue("@timestamp", client.Timestamp);
                command.Parameters.AddWithValue("@addedBy", client.AddedBy);
                command.Parameters.AddWithValue("@kbaUrl", client.KbaUrl);
                command.Parameters.AddWithValue("@remoteLocation", client.RemoteLocation);

                await command.ExecuteNonQueryAsync();

                var freshClients = await DatabaseManager.GetAllClients();

                lock (_clientLock)
                {
                    Clients = freshClients;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating client: {ex.Message}");
                throw;
            }
        }

        public static async Task<Dictionary<string, string>> GetClientDictAsync()
        {
            var clientData = new Dictionary<string, string>();

            try
            {
                using var connection = new SqliteConnection($"Data Source={dbPath}");
                await connection.OpenAsync();

                var command = connection.CreateCommand();
                command.CommandText = "SELECT ClientName, KbaUrl FROM ClientList ORDER BY ClientName";

                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    string name = reader.GetString(0);
                    string kbaUrl = reader.IsDBNull(1) ? "" : reader.GetString(1);

                    if (!clientData.ContainsKey(name))
                        clientData[name] = kbaUrl;
                }
                reader.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error retrieving client list: {ex.Message}");
            }

            return clientData;
        }

        public static async Task<Client> GetClient(string clientName)
        {

            try
            {
                using var connection = new SqliteConnection($"Data Source={dbPath}");
                await connection.OpenAsync();
                var command = connection.CreateCommand();
                command.CommandText = @"
            SELECT * 
            FROM ClientList
            WHERE ClientName = $clientName
            ORDER BY Timestamp DESC 
            LIMIT 1;
                ";
                command.Parameters.AddWithValue("$clientName", clientName);
                using var reader = await command.ExecuteReaderAsync();
                if (reader.Read())
                {
                    var client = new Client
                    {
                        ClientName = reader.GetString(reader.GetOrdinal("ClientName")),
                        Timestamp = reader.GetString(reader.GetOrdinal("Timestamp")),
                        AddedBy = reader.GetString(reader.GetOrdinal("AddedBy")),
                        KbaUrl = reader.GetString(reader.GetOrdinal("KbaUrl")),
                        RemoteLocation = reader.GetString(reader.GetOrdinal("RemoteLocation")),
                    };
                    reader.Close();
                    return client;
                }
            
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error retrieving client: {ex.Message}");

            }

            return null;
        }

        public static async Task<List<Client>> GetAllClients()
        {

            List<Client> clients = new();

            try
            {
                using var connection = new SqliteConnection($"Data Source={dbPath}");
                await connection.OpenAsync();

                var command = connection.CreateCommand();
                command.CommandText = "SELECT * FROM ClientList";

                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    Client client = new Client
                    {
                        ClientName = reader.GetString(reader.GetOrdinal("ClientName")),
                        Timestamp = reader.GetString(reader.GetOrdinal("Timestamp")),
                        AddedBy = reader.GetString(reader.GetOrdinal("AddedBy")),
                        KbaUrl = reader.GetString(reader.GetOrdinal("KbaUrl")),
                        RemoteLocation = reader.GetString(reader.GetOrdinal("RemoteLocation")),
                    };

                    clients.Add(client);
                }
                    reader.Close();
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving all clients: {ex.Message}", ex);
            }
           
            return clients;

        }

        public static async Task<bool> DeleteClientByName(string name)
        {
            try
            {
                using var connection = new SqliteConnection($"Data Source={dbPath}");
                await connection.OpenAsync();

                var command = connection.CreateCommand();
                command.CommandText = @"
            DELETE FROM ClientList
            WHERE ClientName = @clientName";
                command.Parameters.AddWithValue("@clientName", name);

                var rowsAffected = await command.ExecuteNonQueryAsync();
                return rowsAffected > 0;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error deleting client: {ex.Message}", ex);
            }
        }


        //Alert DB manager methods
        public static async Task InsertAlert(string clientName, string addedBy, string message)
        {
            try
            {
                using var connection = new SqliteConnection($"Data Source={dbPath}");
                string alertId = Guid.NewGuid().ToString();
                await connection.OpenAsync();
                var command = connection.CreateCommand();
                command.CommandText = @"
                    INSERT INTO ClientAlerts (ClientName, AlertID, AddedOn, AddedBy,  ResolvedOn, Message, Resolution, Status)
                    VALUES ($clientName, $alertID, $addedOn, $addedBy, $resolvedOn, $message, $resolution, $status)";
                command.Parameters.AddWithValue("$alertID", alertId);
                command.Parameters.AddWithValue("$clientName", clientName);
                command.Parameters.AddWithValue("$addedBy", addedBy);
                command.Parameters.AddWithValue("$addedOn", DateTime.UtcNow.ToString("o"));
                command.Parameters.AddWithValue("$resolvedOn", "TBD");
                command.Parameters.AddWithValue("$status", "New");
                command.Parameters.AddWithValue("$resolution", "Unresolved");
                command.Parameters.AddWithValue("$message", message);
                await command.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                throw new Exception($"Error inserting alert: {ex.Message}", ex);
            }
        }

        public static async Task UpdateAlert(Alert alert)
        {
            try
            {
                using var connection = new SqliteConnection($"Data Source={dbPath}");
                await connection.OpenAsync();

                var command = connection.CreateCommand();
                command.CommandText = @"
            UPDATE ClientAlerts
            SET 
                Status = @status,
                ResolvedOn = @resolvedOn,
                Resolution = @resolution,
                Message = @message
            WHERE AlertID = @alertID";

                command.Parameters.AddWithValue("@status", alert.Status);
                command.Parameters.AddWithValue("@resolvedOn", alert.ResolvedOn ?? "");
                command.Parameters.AddWithValue("@resolution", alert.Resolution ?? "");
                command.Parameters.AddWithValue("@message", alert.Message ?? "");
                command.Parameters.AddWithValue("@alertID", alert.AlertID);

                await command.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public static async Task<List<Alert>> GetCurrentAlerts()
        {
            List<Alert> alerts = new();

            try
            {
                using var connection = new SqliteConnection($"Data Source={dbPath}");
                await connection.OpenAsync();

                var command = connection.CreateCommand();
                command.CommandText = @"
            SELECT Id, AlertID, ClientName, AddedBy, AddedOn, ResolvedOn, Status, Resolution, Message
            FROM ClientAlerts
            WHERE Status != 'Resolved';
        ";

                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var alert = new Alert
                    {
                        AlertID = reader.GetString(reader.GetOrdinal("AlertID")),
                        ClientName = reader.GetString(reader.GetOrdinal("ClientName")),
                        AddedBy = reader.GetString(reader.GetOrdinal("AddedBy")),
                        AddedOn = reader.GetString(reader.GetOrdinal("AddedOn")),
                        ResolvedOn = reader.IsDBNull(reader.GetOrdinal("ResolvedOn")) ? null : reader.GetString(reader.GetOrdinal("ResolvedOn")),
                        Status = reader.GetString(reader.GetOrdinal("Status")),
                        Resolution = reader.IsDBNull(reader.GetOrdinal("Resolution")) ? null : reader.GetString(reader.GetOrdinal("Resolution")),
                        Message = reader.GetString(reader.GetOrdinal("Message"))
                    };

                    alerts.Add(alert);
                }
                reader.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching alerts: {ex.Message}");
            }

            return alerts;
        }

        public static async Task<List<Alert>> GetAllAlerts()
        {
            List<Alert> alerts = new();

            try
            {
                using var connection = new SqliteConnection($"Data Source={dbPath}");
                await connection.OpenAsync();

                var command = connection.CreateCommand();
                command.CommandText = @"
            SELECT Id, AlertID, ClientName, AddedBy, AddedOn, ResolvedOn, Status, Resolution, Message
            FROM ClientAlerts";

                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var alert = new Alert
                    {
                        AlertID = reader.GetString(reader.GetOrdinal("AlertID")),
                        ClientName = reader.GetString(reader.GetOrdinal("ClientName")),
                        AddedBy = reader.GetString(reader.GetOrdinal("AddedBy")),
                        AddedOn = reader.GetString(reader.GetOrdinal("AddedOn")),
                        ResolvedOn = reader.IsDBNull(reader.GetOrdinal("ResolvedOn")) ? null : reader.GetString(reader.GetOrdinal("ResolvedOn")),
                        Status = reader.GetString(reader.GetOrdinal("Status")),
                        Resolution = reader.IsDBNull(reader.GetOrdinal("Resolution")) ? null : reader.GetString(reader.GetOrdinal("Resolution")),
                        Message = reader.GetString(reader.GetOrdinal("Message"))
                    };

                    alerts.Add(alert);
                }
                reader.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching all alerts: {ex.Message}");
            }

            return alerts;
        }

        public static async Task<Alert> GetAlertById(string alertId)
        {

            try
            {
                using var connection = new SqliteConnection($"Data Source={dbPath}");
                await connection.OpenAsync();

                var command = connection.CreateCommand();
                command.CommandText = @"
            SELECT Id, AlertID, ClientName, AddedBy, AddedOn, ResolvedOn, Status, Resolution, Message
            FROM ClientAlerts
            WHERE AlertID = @alertId";
                command.Parameters.AddWithValue("@alertId", alertId);

                using var reader = await command.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    Alert alert = new Alert()
                    {

                        AlertID = reader.GetString(reader.GetOrdinal("AlertID")),
                        ClientName = reader.GetString(reader.GetOrdinal("ClientName")),
                        AddedBy = reader.GetString(reader.GetOrdinal("AddedBy")),
                        AddedOn = reader.GetString(reader.GetOrdinal("AddedOn")),
                        ResolvedOn = reader.IsDBNull(reader.GetOrdinal("ResolvedOn")) ? null : reader.GetString(reader.GetOrdinal("ResolvedOn")),
                        Status = reader.GetString(reader.GetOrdinal("Status")),
                        Resolution = reader.IsDBNull(reader.GetOrdinal("Resolution")) ? null : reader.GetString(reader.GetOrdinal("Resolution")),
                        Message = reader.GetString(reader.GetOrdinal("Message"))

                    };
                        reader.Close();
                    ;   return alert;
                }
                
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetAlertById: {ex.Message}");
                throw;
            }





        }

        public static async Task<bool> DeleteAlertById(string alertId)
        {
            try
            {
                using var connection = new SqliteConnection($"Data Source={dbPath}");
                await connection.OpenAsync();

                var command = connection.CreateCommand();
                command.CommandText = @"
            DELETE FROM ClientAlerts
            WHERE AlertID = @alertId";
                command.Parameters.AddWithValue("@alertId", alertId);

                var rowsAffected = await command.ExecuteNonQueryAsync();
                return rowsAffected > 0;
            }
            catch (Exception )
            {

                throw;
            }
        }


        // User DB manager methods
        public static async Task<bool> UserExists(string username)
        {
            string userHash = Config.Config.HashString(username.ToLower());

            try
            {
                using var connection = new SqliteConnection($"Data Source={dbPath}");
                await connection.OpenAsync();
                var command = connection.CreateCommand();

                command.CommandText = @"
                SELECT COUNT(*) 
                FROM Users 
                WHERE UserHash = @userHash";

                command.Parameters.AddWithValue("@userHash", userHash); 

                var count = (long)await command.ExecuteScalarAsync();
                return count > 0;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error checking if user exists: {ex.Message}", ex);
            }
        }

        public static async Task<bool> IsUserAdmin(string username)
        {
            string userHash = Config.Config.HashString(username.ToLower());

            try
            {
                using var connection = new SqliteConnection($"Data Source={dbPath}");
                await connection.OpenAsync();

                var command = connection.CreateCommand();
                command.CommandText = @"
                SELECT IsAdmin 
                FROM Users 
                WHERE UserHash = @userHash";

                command.Parameters.AddWithValue("@userHash", userHash);

                var result = await command.ExecuteScalarAsync();
                return result != null && Convert.ToBoolean(result);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error checking if user is admin: {ex.Message}", ex);
            }
        }

        public static async Task<User?> GetUserByUserHash(string userHash)
        {
           
            try
            {
                using var connection = new SqliteConnection($"Data Source={dbPath}");
                await connection.OpenAsync();
                var command = connection.CreateCommand();
                command.CommandText = @"
                SELECT * 
                FROM Users 
                WHERE UserHash = @userHash";
                command.Parameters.AddWithValue("@userHash", userHash);
                using var reader = await command.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    return new User
                    {
                        Name = reader.GetString(reader.GetOrdinal("Name")),
                        Username = reader.GetString(reader.GetOrdinal("Username")),
                        UserHash = userHash,
                        ApiKey = reader.GetString(reader.GetOrdinal("ApiKey")),
                        PasswordHash = reader.GetString(reader.GetOrdinal("PasswordHash")),
                        IsAdmin = reader.GetBoolean(reader.GetOrdinal("IsAdmin"))
                    };
                }
                reader.Close();
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving user: {ex.Message}", ex);
            }
            return null;
        }

        public static async Task InsertUser(User user )
        {

            try
            {
                if (await GetUserByUserHash(user.UserHash) != null)
                {
                    throw new InvalidOperationException("User already exists.");
                }

                using var connection = new SqliteConnection($"Data Source={dbPath}");
                await connection.OpenAsync();
                var command = connection.CreateCommand();
                command.CommandText = @"
                INSERT INTO Users (Name, PasswordHash, Username, UserHash, ApiKey, IsAdmin)
                VALUES (@name, @passwordHash, @username, @userHash, @apiKey, @isAdmin)";
                command.Parameters.AddWithValue("@name", user.Name);
                command.Parameters.AddWithValue("@username", user.Username);
                command.Parameters.AddWithValue("@passwordHash", user.PasswordHash);
                command.Parameters.AddWithValue("@userHash", user.UserHash);
                command.Parameters.AddWithValue("@apiKey", user.ApiKey);
                command.Parameters.AddWithValue("@isAdmin", user.IsAdmin);
                await command.ExecuteNonQueryAsync();

                List<User> allUsers = await GetAllUsers();
                int totalUsers = allUsers.Count;
            }
            catch (Exception ex)
            {

                throw new Exception($"Error adding user to database: {ex.Message}", ex);
            }
        }

        public static async Task<List<User>> GetAllUsers()
        {
            List<User> users = new List<User>();
            try
            {
                using var connection = new SqliteConnection($"Data Source={dbPath}");
                await connection.OpenAsync();
                var command = connection.CreateCommand();
                command.CommandText = "SELECT * FROM Users";
                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    User user = new User
                    {
                        Username = reader.GetString(reader.GetOrdinal("Username")),
                        Name = reader.GetString(reader.GetOrdinal("Name")),
                        UserHash = reader.GetString(reader.GetOrdinal("UserHash")),
                        ApiKey = reader.GetString(reader.GetOrdinal("ApiKey")),
                        IsAdmin = reader.GetBoolean(reader.GetOrdinal("IsAdmin"))
                    };
                    users.Add(user);
                }
                reader.Close();
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving all users: {ex.Message}", ex);
            }
            return users;
        }

        internal static async Task<bool> DeleteUserByUsername(string username)
        {
            try
            {   
                using var connection = new SqliteConnection($"Data Source={dbPath}");
                await connection.OpenAsync();

                var command = connection.CreateCommand();
                command.CommandText = @"
                DELETE FROM Users
                WHERE Username = @username AND IsAdmin = 0";

                command.Parameters.AddWithValue("@username", username);

                var rowsAffected = await command.ExecuteNonQueryAsync();
                return rowsAffected > 0;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error deleting user: {ex.Message}", ex);
            }
        }

        internal static async Task<bool> UserApiKeyExists(string apiKey)
        {
            try
            {
                using var connection = new SqliteConnection($"Data Source={dbPath}");
                await connection.OpenAsync();
                var command = connection.CreateCommand();
                command.CommandText = @"
                SELECT COUNT(*) 
                FROM Users 
                WHERE ApiKey = @apiKey";
                command.Parameters.AddWithValue("@apiKey", apiKey);
                var count = (long)await command.ExecuteScalarAsync();
                return count > 0;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error checking if user apiKey exists: {ex.Message}", ex);
            }
        }

        internal static async Task<String> GetUserApiKeyWithUserHash(string userHash)
        {
            try
            {
                using var connection = new SqliteConnection($"Data Source={dbPath}");
                await connection.OpenAsync();
                var command = connection.CreateCommand();
                command.CommandText = @"
                SELECT * 
                FROM Users 
                WHERE UserHash = @userHash";
                command.Parameters.AddWithValue("@userHash", userHash);
                using var reader = await command.ExecuteReaderAsync();

                if (await reader.ReadAsync())
                {
                   User user = new User
                    {
                        ApiKey = reader.GetString(reader.GetOrdinal("ApiKey")),
                    };
                     return Convert.ToString(user.ApiKey);
                }

                reader.Close();
                return string.Empty; 
            }
            catch (Exception ex)
            {
                throw new Exception($"Error checking if user auth key exists: {ex.Message}", ex);
            }
        }

        internal static async Task<bool> UpdateUser(User user, string originalUsername)
        {
            try
            {
                string newUserHash = Config.Config.HashString(user.Username.ToLower());
                using var connection = new SqliteConnection($"Data Source={dbPath}");
                await connection.OpenAsync();

                var command = connection.CreateCommand();
                command.CommandText = @"
                UPDATE Users
                SET 
                    Username = @newUsername,
                    UserHash = @newUserHash,
                    Name = @name,
                    IsAdmin = @isAdmin
                WHERE Username = @originalUsername";

                command.Parameters.AddWithValue("@newUsername", user.Username);
                command.Parameters.AddWithValue("@newUserHash", newUserHash);
                command.Parameters.AddWithValue("@name", user.Name);
                command.Parameters.AddWithValue("@isAdmin", user.IsAdmin);
                command.Parameters.AddWithValue("@originalUsername", originalUsername);


                var rowsAffected = await command.ExecuteNonQueryAsync();
                return rowsAffected > 0;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error updating user: {ex.Message}", ex);
            }
        }

        internal static async Task<bool> UpdateUserPassword(string username, string newPassword)
        {
            try
            {
                string apiKey = Config.Config.HashString(username.ToLower() + "|" + newPassword);
                string passwordHash = Config.Config.HashPassword(newPassword);

                using var connection = new SqliteConnection($"Data Source={dbPath}");
                await connection.OpenAsync();

                var command = connection.CreateCommand();
                command.CommandText = @"
                UPDATE Users
                SET ApiKey = @apiKey,
                    PasswordHash = @passwordHash
                WHERE Username = @username";

                command.Parameters.AddWithValue("@apiKey", apiKey);
                command.Parameters.AddWithValue("@passwordHash", passwordHash);
                command.Parameters.AddWithValue("@username", username);

                var rowsAffected = await command.ExecuteNonQueryAsync();
                return rowsAffected > 0;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error updating user password: {ex.Message}", ex);
            }
        }



        //for Testing to populate an empty DB
        public static async Task SeedFakeAlerts(int count = 10)
        {
            var random = new Random();

            var animals = new List<string>
            {
                "Panda", "Koala", "Jaguar", "Otter", "Penguin", "Sloth", "Cheetah", "Narwhal", "Wombat", "Capybara",
                "Lynx", "Meerkat", "Axolotl", "Ibex", "Quokka", "Tapir", "Manatee", "Okapi", "Fossa", "Tarsier"
            };


            var sufixes = new List<string>
            {
                "-Corp", "-Tech", "-Org", "-Company", "-Cororation", "-Co"
            };

            for (int i = 0; i < count; i++)
            {
                string clientName = animals[random.Next(1, 20)] + sufixes[random.Next(sufixes.Count)];
                string addedBy = $"Tech_{random.Next(1, 10)}";
                string message = $"Test alert #{i + 1} - Possible issue detected.";

                await InsertAlert(clientName, addedBy, message);
            }

        }

        public static async Task SeedRandomClients(int count = 20)
        {
            var animals = new List<string>
    {
        "Panda", "Koala", "Jaguar", "Otter", "Penguin", "Sloth", "Cheetah", "Narwhal", "Wombat", "Capybara",
        "Lynx", "Meerkat", "Axolotl", "Ibex", "Quokka", "Tapir", "Manatee", "Okapi", "Fossa", "Tarsier"
    };

            var sufixes = new List<string>
    {
        "-Corp", "-Tech", "-Org", "-Company", "-Cororation", "-Co"
    };

            var random = new Random();
            var random2 = new Random();
            using var connection = new SqliteConnection($"Data Source={dbPath}");
            await connection.OpenAsync();

            foreach (var animal in animals.OrderBy(_ => random.Next()).Take(count))
            {
                var command = connection.CreateCommand();
                command.CommandText = @"
            INSERT INTO ClientList (ClientName, Timestamp, AddedBy, KbaUrl, RemoteLocation)
            VALUES (@clientName, @timestamp, @addedBy, @kbaUrl, @remoteLocation);
        ";

                command.Parameters.AddWithValue("@clientName", animal + sufixes[random.Next(sufixes.Count)]);
                command.Parameters.AddWithValue("@timestamp", DateTime.UtcNow.ToString("o"));
                command.Parameters.AddWithValue("@addedBy", "SystemSeeder");
                command.Parameters.AddWithValue("@kbaUrl", $"https://en.wikipedia.org/wiki/{animal}");
                command.Parameters.AddWithValue("@remoteLocation", @"C:\Windows\System32\notepad.exe");

                await command.ExecuteNonQueryAsync();
            }
        }
    }
}
