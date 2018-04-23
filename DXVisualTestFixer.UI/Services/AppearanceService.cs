using DevExpress.Xpf.Core;
using DXVisualTestFixer.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DXVisualTestFixer.UI.Services {
    public class AppearanceService : IAppearanceService {
        public void SetTheme(string themeName) {
            ApplicationThemeHelper.ApplicationThemeName = themeName;
        }
    }
}
