using System.Diagnostics;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using DevExpress.Utils;
using JetBrains.Annotations;

namespace DXVisualTestFixer.UI.Models {
	public class OpenSolutionModel : ImmutableObject {
		readonly string _solutionPath;

		public OpenSolutionModel(string solutionPath, string associatedProgramPath, string displayName, BitmapSource programImage) {
			_solutionPath = solutionPath;
			AssociatedProgramPath = associatedProgramPath;
			DisplayName = displayName;
			ProgramImage = programImage;
		}

		public string AssociatedProgramPath { get; }
		public string DisplayName { get; }
		public BitmapSource ProgramImage { get; internal set; }

		[UsedImplicitly]
		public void Open() {
			var info = new ProcessStartInfo(AssociatedProgramPath, _solutionPath);
			info.Verb = "runas";
			Process.Start(info);
		}
	}
}