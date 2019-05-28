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
        public Repository[] Repositories { get; set; } = new Repository[0];
        public string LastVersion { get; set; }
        public string InstallPath { get; set; }
        public string ThemeName { get; set; } = "Office2016White";
        public string WorkingDirectory { get; set; } = @"C:\Work";

        public IEnumerable<Repository> GetLocalRepositories() {
            return Repositories?.Where(r => r.IsDownloaded()) ?? Enumerable.Empty<Repository>();
        }

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
            if(config.Repositories == null)
                config.Repositories = new Repository[0];
            var versions = RepositoryLoader.GetVersions();
            var currentRepos = config.Repositories.Where(repo => versions.Contains(repo.Version)).ToArray();
            if(currentRepos.Length != config.Repositories.Length)
                config.Repositories = currentRepos;
            var reposToDownload = new List<Repository>();
            foreach(var version in versions) {
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
