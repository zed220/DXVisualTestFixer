using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DXVisualTestFixer.Configuration {
    public static class VersionInfo {
        public static string VersionString = GetVersion();
        public static readonly Version Version = new Version(VersionString);

        static string GetVersion() {
            if(System.Deployment.Application.ApplicationDeployment.IsNetworkDeployed) {
                System.Deployment.Application.ApplicationDeployment ad = System.Deployment.Application.ApplicationDeployment.CurrentDeployment;
                return ad.CurrentVersion.ToString(3);
            }
            return "0.0.0";
        }
    }
}
