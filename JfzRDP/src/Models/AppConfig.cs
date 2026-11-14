using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace JfzRdp.Models
{
    [DataContract]
    public sealed class AppConfig
    {
        [DataMember(Name = "Maximized", Order = 0)]
        public bool Maximized { get; set; }

        [DataMember(Name = "Width", Order = 1)]
        public int Width { get; set; }

        [DataMember(Name = "Height", Order = 2)]
        public int Height { get; set; }

        [DataMember(Name = "ServerList", Order = 3)]
        public List<ServerEntry> ServerList { get; set; }

        public static AppConfig CreateDefault()
        {
            return new AppConfig
            {
                Maximized = false,
                Width = 1920,
                Height = 1080,
                ServerList = new List<ServerEntry>
                {
                    new ServerEntry
                    {
                        ServerName = "jfz123",
                        Server = "127.0.0.1",
                        UserName = "Sandeepin",
                        Password = "123456",
                        Port = 3389
                    },
                    new ServerEntry
                    {
                        ServerName = "JfzVictus-192",
                        Server = "192.168.233.168",
                        UserName = "Sandeepin",
                        Password = "1",
                        Port = 3389
                    }
                }
            };
        }

        public static void Normalize(AppConfig config)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            if (config.Width < 320 || config.Width > 7680)
            {
                config.Width = 1920;
            }

            if (config.Height < 240 || config.Height > 4320)
            {
                config.Height = 1080;
            }

            if (config.ServerList == null)
            {
                config.ServerList = new List<ServerEntry>();
                return;
            }

            var validServers = new List<ServerEntry>(config.ServerList.Count);
            foreach (var entry in config.ServerList)
            {
                if (entry == null)
                {
                    continue;
                }

                entry.Server = (entry.Server ?? string.Empty).Trim();
                if (entry.Server.Length == 0)
                {
                    continue;
                }

                entry.ServerName = string.IsNullOrWhiteSpace(entry.ServerName)
                    ? entry.Server
                    : entry.ServerName.Trim();
                entry.UserName = entry.UserName ?? string.Empty;
                entry.Password = entry.Password ?? string.Empty;

                if (entry.Port < 1 || entry.Port > 65535)
                {
                    entry.Port = 3389;
                }

                validServers.Add(entry);
            }

            config.ServerList = validServers;
        }
    }

    [DataContract]
    public sealed class ServerEntry
    {
        [DataMember(Name = "ServerName", Order = 0)]
        public string ServerName { get; set; }

        [DataMember(Name = "Server", Order = 1)]
        public string Server { get; set; }

        [DataMember(Name = "UserName", Order = 2)]
        public string UserName { get; set; }

        [DataMember(Name = "Password", Order = 3)]
        public string Password { get; set; }

        [DataMember(Name = "Port", Order = 4)]
        public int Port { get; set; }
    }
}
