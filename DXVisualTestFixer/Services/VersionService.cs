using System;
using DXVisualTestFixer.Common;
using DXVisualTestFixer.Configuration;

namespace DXVisualTestFixer.Services {
	public class VersionService : IVersionService {
		public Version Version => VersionInfo.Version;
	}
}