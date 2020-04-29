using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DXVisualTestFixer.Common;
using DXVisualTestFixer.Configuration;

namespace DXVisualTestFixer.Services {
	public class VersionService : IVersionService {
		public Version Version => VersionInfo.Version;

		public (Version version, string content)[] WhatsNewInfo { get; } = GetWhatsNewInfo(); 

		static (Version version, string content)[] GetWhatsNewInfo() {
			var result = new List<(Version version, string content)>();
			using var stream = typeof(VersionService).Assembly.GetManifestResourceStream("DXVisualTestFixer.WhatsNew.txt");
			using var sr = new StreamReader(stream);
			while(!sr.EndOfStream) {
				var line = sr.ReadLine();
				var versionAndContent = line.Split(new [] { "::"}, StringSplitOptions.RemoveEmptyEntries);
				if(versionAndContent.Length != 2)
					continue;
				result.Add((new Version(versionAndContent[0].Split(new [] { " "}, StringSplitOptions.RemoveEmptyEntries).Last()), versionAndContent[1]));
			}
			
			return result.ToArray();
		}
	}
}