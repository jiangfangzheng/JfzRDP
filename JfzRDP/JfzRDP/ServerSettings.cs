using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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