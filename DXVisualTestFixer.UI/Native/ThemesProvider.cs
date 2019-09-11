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
    public class ThemesProvider : FileStringLoaderBase, IThemesProvider {
        public ThemesProvider() : base(@"\\corp\internal\common\visualTests_squirrel\themes.xml") { }

        public List<string> AllThemes => Result;

        protected override List<string> LoadIfFileNotFound() => Theme.Themes.Select(t => t.Name).ToList();
    }
}
