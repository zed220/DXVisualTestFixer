using DXVisualTestFixer.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace DXVisualTestFixer.Configuration {
    public class Config {
        public const string ConfigFileName = "ui.config";
        public Repository[] Repositories { get; set; }
        public string LastVersion { get; set; }
        public string InstallPath { get; set; }
        public string ThemeName { get; set; } = "Office2016White";

        public static Config GenerateDefault() {
            var result = Validate(new Config());
            ConfigSerializer.SaveConfig(result);
            return result;
        }


        public static Config Validate(Config config) {
            if(string.IsNullOrEmpty(config.LastVersion))
                config.LastVersion = VersionInfo.Version.ToString();
            if(string.IsNullOrEmpty(config.InstallPath))
                config.InstallPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            return config;
        }
    }
}
