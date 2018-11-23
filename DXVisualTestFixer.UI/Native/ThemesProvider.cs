using DevExpress.Xpf.Core;
using DXVisualTestFixer.Common;
using Microsoft.Practices.ServiceLocation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DXVisualTestFixer.UI.Native {
    public class ThemesProvider : IThemesProvider {
        const string serverPath = @"\\corp\internal\common\visualTests_squirrel\themes.xml";

        List<string> _AllThemes;

        public List<string> AllThemes {
            get {
                if(_AllThemes == null)
                    _AllThemes = GetThemes();
                return _AllThemes;
            }
        }

        List<string> GetThemes() {
            if(!File.Exists(serverPath)) {
                return Theme.Themes.Select(t => t.Name).ToList();
            }
            var result = File.ReadAllLines(serverPath).ToList();
            return result;
        }
    }
}
