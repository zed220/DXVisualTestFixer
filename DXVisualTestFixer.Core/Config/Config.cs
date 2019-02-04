using DXVisualTestFixer.Common;
using DXVisualTestFixer.Core;
using Microsoft.Practices.ServiceLocation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace DXVisualTestFixer.Core.Configuration {
    class Config : IConfig {
        public const string ConfigFileName = "ui.config";
        public Repository[] Repositories { get; set; }
        public string LastVersion { get; set; }
        public string InstallPath { get; set; }
        public string ThemeName { get; set; } = "Office2016White";
        public string WorkingDirectory { get; set; } = @"C:\Work";

        public static Config GenerateDefault(IConfigSerializer configSerializer) {
            var result = Validate(new Config());
            configSerializer.SaveConfig(result);
            return result;
        }


        public static Config Validate(Config config) {
            if(string.IsNullOrEmpty(config.LastVersion))
                config.LastVersion = ServiceLocator.Current.GetInstance<IVersionService>().Version.ToString();
            if(string.IsNullOrEmpty(config.InstallPath))
                config.InstallPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            if(string.IsNullOrEmpty(config.WorkingDirectory))
                config.WorkingDirectory = @"C:\Work";
            var currentRepos = config.Repositories.Where(repo => Repository.Versions.Contains(repo.Version)).ToArray();
            if(currentRepos.Length != config.Repositories.Length)
                config.Repositories = currentRepos;
            var reposToDownload = new List<Repository>();
            foreach(var version in Repository.Versions) {
                if(config.Repositories.Select(r => r.Version).Contains(version))
                    continue;
                reposToDownload.Add(new Repository() { Version = version, Path = System.IO.Path.Combine(config.WorkingDirectory, $"20{version}_VisualTests") });
            }
            if(reposToDownload.Count > 0)
                config.Repositories = Enumerable.Concat(config.Repositories, reposToDownload).ToArray();
            return config;
        }
    }
}
