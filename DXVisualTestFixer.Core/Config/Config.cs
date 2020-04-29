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
		public string WorkingDirectory { get; set; } = @"C:\Work";
		public string WhatsNewSeenForVersion { get; set; } = "1.0.0";

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
			var versions = RepositoryLoader.GetVersions();
			var currentRepos = config.Repositories.Where(repo => versions.Contains(repo.Version)).ToArray();
			if(currentRepos.Length != config.Repositories.Length)
				config.Repositories = currentRepos;
			var reposToDownload = new List<Repository>();
			foreach(var version in versions) {
				if(config.Repositories.Select(r => r.Version).Contains(version))
					continue;
				reposToDownload.Add(
					Repository.CreateRegular(
						ServiceLocator.Current.GetInstance<IPlatformInfo>().GitRepository, version,
						Path.Combine(config.WorkingDirectory, String.Format(ServiceLocator.Current.GetInstance<IPlatformInfo>().LocalPath, version))));
			}

			if(reposToDownload.Count > 0)
				config.Repositories = config.Repositories.Concat(reposToDownload).ToArray();
			return config;
		}
	}
}