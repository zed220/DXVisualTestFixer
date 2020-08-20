using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using DXVisualTestFixer.Common;
using Microsoft.Practices.ServiceLocation;

namespace DXVisualTestFixer.Core.Configuration {
	class Config : IConfig {
		public string LastVersion { get; set; }
		public string InstallPath { get; set; }
		public Repository[] Repositories { get; set; } = new Repository[0];
		public string ThemeName { get; set; } = "Office2016White";
		public string Volunteer { get; set; }
		public string Email { get; set; }
		public string WorkingDirectory { get; set; } = @"C:\Work";
		public string WhatsNewSeenForVersion { get; set; }
		public string DefaultPlatform { get; set; }
		

		public IEnumerable<Repository> GetLocalRepositories() {
			return Repositories?.Where(r => r.IsDownloaded()) ?? Enumerable.Empty<Repository>();
		}

		public static Config GenerateDefault(IConfigSerializer configSerializer) {
			var result = Validate(new Config());
			configSerializer.SaveConfig(result);
			return result;
		}

		public static Config Validate(Config config) {
			var currentVersion = ServiceLocator.Current.GetInstance<IVersionService>().Version.ToString();
			if(string.IsNullOrEmpty(config.LastVersion))
				config.LastVersion = currentVersion;
			if(string.IsNullOrEmpty(config.InstallPath))
				config.InstallPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
			if(string.IsNullOrEmpty(config.WorkingDirectory))
				config.WorkingDirectory = @"C:\Work";
			if(string.IsNullOrEmpty(config.WhatsNewSeenForVersion))
				config.WhatsNewSeenForVersion = currentVersion;
			if(config.Repositories == null)
				config.Repositories = new Repository[0];
			if(config.Repositories.Any(r => r.Platform == null))
				config.Repositories = config.Repositories.Where(r => r.Platform != null).ToArray();

			var reposToDownload = new List<Repository>();
			var reposOutdated = new List<Repository>();
			foreach(var platform in ServiceLocator.Current.GetInstance<IPlatformProvider>().PlatformInfos) {
				var conigRepos = config.Repositories.Where(r => r.Platform == platform.Name);
				var repoVersions = RepositoryLoader.GetVersions(platform);
				foreach(var version in repoVersions) {
					if(conigRepos.Select(r => r.Version).Contains(version))
						continue;
					reposToDownload.Add(Repository.CreateRegular(platform.Name, version, Path.Combine(config.WorkingDirectory,platform.LocalPath, version)));
				}
				foreach(var repo in conigRepos) {
					if(!repoVersions.Contains(repo.Version))
						reposOutdated.Add(repo);
				}
			}
			
			var repos = config.Repositories.ToList();
			repos.RemoveAll(reposOutdated.Contains);
			config.Repositories = repos.ToArray();

			if(reposToDownload.Count > 0)
				config.Repositories = config.Repositories.Concat(reposToDownload).ToArray();
			return config;
		}
	}
}