namespace RemoteAccessPortal.Classes
{

    public class DeviceConfig
    {
        public string ServerURL { get; set; }
        public int ServerPort { get; set; }
        public int ClientPort { get; set; }
        public string AdminUsername { get; set; }
        public string AdminPassword { get; set; }
        public string DatabasePath { get; set; }

    }
}
