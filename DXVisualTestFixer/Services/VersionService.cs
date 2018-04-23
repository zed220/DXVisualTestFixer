using DXVisualTestFixer.Common;
using DXVisualTestFixer.Configuration;
using DXVisualTestFixer.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DXVisualTestFixer.Services {
    public class VersionService : IVersionService {
        public Version Version { get { return VersionInfo.Version; } }
    }
}
