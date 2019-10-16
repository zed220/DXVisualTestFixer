using System.Collections.Generic;
using System.Linq;
using DevExpress.Xpf.Core;
using DXVisualTestFixer.Common;

namespace DXVisualTestFixer.UI.Native {
	public class ThemesProvider : FileStringLoaderBase, IThemesProvider {
		public ThemesProvider() : base(@"\\corp\internal\common\visualTests_squirrel\themes.xml") { }

		public List<string> AllThemes => Result;

		protected override List<string> LoadIfFileNotFound() {
			return Theme.Themes.Select(t => t.Name).ToList();
		}
	}
}