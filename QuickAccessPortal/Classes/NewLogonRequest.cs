using System.Text.Json.Serialization;

namespace RemoteAccessPortal.Classes
{
    public class NewLogonRequest
    {
        [JsonPropertyName("hashedUsername")]
        public string HashedUsername { get; set; }

        [JsonPropertyName("hashedPassword")]
        public string HashedPassword { get; set; }
    }
}
