using System.Collections.Generic;

namespace JfzRDP
{
    public class ServerSettings
    {
        public ServerSettings()
        {
            ServerList = new List<ServerSetting>();
        }

        public List<ServerSetting> ServerList { get; set; }
    }
}