using DevExpress.Xpf.Core;
using DXVisualTestFixer.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DXVisualTestFixer.UI.Native {
    public class ThemesProvider : IThemesProvider {
        List<string> _AllThemes;

        public List<string> AllThemes {
            get {
                if(_AllThemes == null)
                    _AllThemes = Theme.Themes.Select(t => t.Name).ToList();
                return _AllThemes;
            }
        }
    }
}
