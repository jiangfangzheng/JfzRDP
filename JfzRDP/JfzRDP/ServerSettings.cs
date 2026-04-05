using System.Collections.Generic;

namespace JfzRDP
{
    public class ServerSettings
    {
        public ServerSettings()
        {
            Maximized = false;
            ServerList = new List<ServerSetting>();
            Width = 1920;
            Height = 1080;
        }

        public bool Maximized { get; set; }

        public int Width { get; set; }

        public int Height { get; set; }

        public List<ServerSetting> ServerList { get; set; }
    }
}