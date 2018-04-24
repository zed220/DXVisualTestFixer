using DXVisualTestFixer.Common;
using Microsoft.Practices.ServiceLocation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DXVisualTestFixer.UI.Native {
    public static class VersionHelper {
        public static string Version { get { return GetVersion(); } }

        static string GetVersion() {
            return ServiceLocator.Current.GetInstance<IVersionService>().Version.ToString();
        }
    }
}
