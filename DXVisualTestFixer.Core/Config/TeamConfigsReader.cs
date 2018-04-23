using DXVisualTestFixer.Common;
using DXVisualTestFixer.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DXVisualTestFixer.Core.Configuration {
    public static class TeamConfigsReader {
        const string ConfigFolder = "XmlConfigs";
        static List<Team> configs = null;
        //public IEnumerable<Team> RegisteredConfigs => configs.Values;
        //public static Team this[int] => configs[name];

        public static Team GetTeam(string version, string serverFolderName, out TeamInfo info) {
            //var v = Application.ProductVersion;
            //System.Reflection.Assembly assembly = System.Reflection.Assembly.GetExecutingAssembly();
            //FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(assembly.Location);
            //string v = fvi.FileVersion;
            //var ver = typeof(TeamConfigsReader).Assembly.GetName().Version;

            //System.Version ver = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
            foreach(var team in configs.Where(c => c.Version == version)) {
                info = team.TeamInfos.FirstOrDefault(i => i.ServerFolderName == serverFolderName);
                if(info != null)
                    return team;
            }
            info = null;
            return null;
            //return configs.Where(c => c.Version == version).Where(c => c.ServerFolderName == serverFolderName).FirstOrDefault();
        }

        static TeamConfigsReader() {
            configs = GetRegisteredConfigs();
            //configs = GetRegisteredConfigs().ToDictionary(x => x.Name, team => team);
        }
        static List<Team> GetRegisteredConfigs() {
            string appDir = Path.GetDirectoryName(typeof(TeamConfigsReader).Assembly.Location);
            var dir = Path.Combine(appDir, ConfigFolder);
            if(Directory.Exists(dir))
                return Directory.GetFiles(dir, "*.config", SearchOption.AllDirectories).Select(Serializer.Deserialize<Team>).ToList();
            return new List<Team>();
        }
        public static List<Team> GetAllTeams() {
            return new List<Team>(configs);
        }
        public static string SaveConfig(Team team) {
            using(MemoryStream s = new MemoryStream()) {
                Serializer.Serialize(s, team);
                s.Seek(0, SeekOrigin.Begin);
                using(StreamReader r = new StreamReader(s)) {
                    return r.ReadToEnd();
                }
            }
        }
    }
}
