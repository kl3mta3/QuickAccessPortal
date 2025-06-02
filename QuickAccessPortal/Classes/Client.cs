using System.Text.Json.Serialization;

namespace RemoteAccessPortal.Classes
{
    public class Client
    {
        [JsonPropertyName("clientName")]
        public string ClientName { get; set; }

        [JsonPropertyName("kbaUrl")]
        public string KbaUrl { get; set; }

        [JsonPropertyName("remoteLocation")]
        public string RemoteLocation { get; set; }

        [JsonPropertyName("timestamp")]
        public string Timestamp { get; set; }

        [JsonPropertyName("addedBy")]
        public string AddedBy { get; set; }
    }
}
