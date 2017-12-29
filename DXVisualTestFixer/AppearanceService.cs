using DevExpress.Xpf.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DXVisualTestFixer {
    public interface IAppearanceService {
        void SetTheme(string themeName);
    }

    public class AppearanceService : IAppearanceService {
        public void SetTheme(string themeName) {
            ApplicationThemeHelper.ApplicationThemeName = themeName;
        }
    }
}
