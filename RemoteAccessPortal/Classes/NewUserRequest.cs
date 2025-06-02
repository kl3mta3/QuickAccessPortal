using System.Text.Json.Serialization;

namespace RemoteAccessPortal.Classes
{
    public class NewUserRequest
    {
        public User User { get; set; }

        [JsonPropertyName("password")]
        public string Password{ get; set; }

    }
}
