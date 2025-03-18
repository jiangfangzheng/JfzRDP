using AxMSTSCLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JfzRDP
{
    internal class JRdpObj
    {
        public JRdpObj(ServerSetting setting, string connectMsg)
        {
            Setting = setting;
            ConnectMsg = connectMsg;
            RdpClient = new AxMsRdpClient11NotSafeForScripting();
            ConnectMsg = "";
            ErrMsg = "";
            Index = -1;
        }

        public ServerSetting Setting { get; set; }
        public AxMsRdpClient11NotSafeForScripting RdpClient { get; set; }
        public string ConnectMsg { get; set; }
        public string ErrMsg { get; set; }
        public int Index { get; set; }
    }
}
