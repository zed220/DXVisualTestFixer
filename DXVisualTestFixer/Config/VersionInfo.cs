using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DXVisualTestFixer.Configuration {
    public static class VersionInfo {
        public const string VersionString = "1.0.31"; // do not specify revision if 0
        public static readonly Version Version = new Version(VersionString);
    }
}
