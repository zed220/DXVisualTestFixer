using System.Diagnostics;
using System.Windows.Media;
using DevExpress.Utils;
using JetBrains.Annotations;

namespace DXVisualTestFixer.UI.Models {
	public class OpenSolutionModel : ImmutableObject {
		readonly string _solutionPath;

		public OpenSolutionModel(string solutionPath, string associatedProgramPath, string displayName, ImageSource programImage) {
			_solutionPath = solutionPath;
			AssociatedProgramPath = associatedProgramPath;
			DisplayName = displayName;
			ProgramImage = programImage;
		}

		public string AssociatedProgramPath { get; }
		public string DisplayName { get; }
		public ImageSource ProgramImage { get; }

		[UsedImplicitly]
		public void Open() {
			var info = new ProcessStartInfo(AssociatedProgramPath, _solutionPath);
			info.Verb = "runas";
			Process.Start(info);
		}
	}
}