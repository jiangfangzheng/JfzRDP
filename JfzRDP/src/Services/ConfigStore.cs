using System;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using JfzRdp.Models;

namespace JfzRdp.Services
{
    public static class ConfigStore
    {
        private static readonly DataContractJsonSerializer Serializer =
            new DataContractJsonSerializer(typeof(AppConfig));

        public static AppConfig LoadOrCreate(string path, out bool createdDefault, out string warning)
        {
            createdDefault = false;
            warning = null;

            if (!File.Exists(path))
            {
                var defaultConfig = AppConfig.CreateDefault();
                try
                {
                    Save(path, defaultConfig);
                    createdDefault = true;
                }
                catch (Exception ex)
                {
                    warning = "无法创建默认 server.json，程序将使用内置默认配置。\r\n\r\n" + ex.Message;
                }

                return defaultConfig;
            }

            try
            {
                AppConfig config;
                using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    config = (AppConfig)Serializer.ReadObject(stream);
                }

                if (config == null)
                {
                    throw new InvalidDataException("server.json 内容为空或不是有效对象。");
                }

                AppConfig.Normalize(config);
                return config;
            }
            catch (Exception ex)
            {
                warning = "读取 server.json 失败，程序将使用内置默认配置。请检查文件格式。\r\n\r\n"
                    + ex.Message;
                return AppConfig.CreateDefault();
            }
        }

        public static void Save(string path, AppConfig config)
        {
            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var tempPath = path + ".tmp";
            using (var stream = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                using (var writer = JsonReaderWriterFactory.CreateJsonWriter(
                    stream,
                    new UTF8Encoding(false),
                    false,
                    true,
                    "  "))
                {
                    Serializer.WriteObject(writer, config);
                    writer.Flush();
                }
            }

            if (File.Exists(path))
            {
                File.Replace(tempPath, path, null);
            }
            else
            {
                File.Move(tempPath, path);
            }
        }
    }
}
