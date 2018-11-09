using DXVisualTestFixer.Common;
using DXVisualTestFixer.Configuration;
using System;

namespace DXVisualTestFixer.Services {
    public class VersionService : IVersionService {
        public Version Version { get { return VersionInfo.Version; } }
    }
}
