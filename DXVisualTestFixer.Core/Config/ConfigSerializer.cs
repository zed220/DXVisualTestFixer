using System;
using System.IO;
using DXVisualTestFixer.Common;
using Microsoft.Practices.ServiceLocation;

namespace DXVisualTestFixer.Core.Configuration {
	public class ConfigSerializer : IConfigSerializer {
		static readonly ConfigSerializer Instance = new ConfigSerializer();

		static Config cached;

		static readonly string SettingsPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + $"\\{ServiceLocator.Current.GetInstance<IPlatformInfo>().ApplicationName}\\";
		static readonly string SettingsFile = "ui_settings.config";
		static string SettingsFilePath => SettingsPath + SettingsFile;

		public IConfig GetConfig(bool useCache = true) {
			if(useCache && cached != null)
				return cached;
			return cached = GetConfigCore();
		}

		public void SaveConfig(IConfig options) {
			cached = null;
			try {
				Serializer.Serialize(SettingsFilePath, options);
			}
			catch { }
		}

		public bool IsConfigEquals(IConfig left, IConfig right) {
			return GetConfigAsString(left) == GetConfigAsString(right);
		}

		Config GetConfigCore() {
			if(!File.Exists(SettingsFilePath))
				return Config.GenerateDefault(this);
			try {
				return Config.Validate(Serializer.Deserialize<Config>(SettingsFilePath));
			}
			catch {
				return Config.GenerateDefault(this);
			}
		}

		string GetConfigAsString(IConfig config) {
			using var stream = new MemoryStream();
			Serializer.Serialize(stream, config);
			stream.Seek(0, SeekOrigin.Begin);
			using var reader = new StreamReader(stream);
			return reader.ReadToEnd();
		}
	}
}