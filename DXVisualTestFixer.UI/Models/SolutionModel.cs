using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using DevExpress.Utils;
using JetBrains.Annotations;
using Microsoft.Win32;

namespace DXVisualTestFixer.UI.Models {
	public class SolutionModel : ImmutableObject {
		[CanBeNull] readonly string SolutionPath;

		public SolutionModel(string version, string path) {
			Version = version;
			Path = System.IO.Path.Combine(path, "VisualTests");
			SolutionPath = Directory.EnumerateFiles(Path, "*.sln", SearchOption.TopDirectoryOnly).FirstOrDefault();
			OpenSolutionModels = new List<OpenSolutionModel>();
			OpenSolutionModels = OpenSolutionModelHelper.GetOpenSolutionModels(SolutionPath);
		}

		public string Version { get; }
		public string Path { get; }
		public List<OpenSolutionModel> OpenSolutionModels { get; }
		[UsedImplicitly] public bool IsEnabled => File.Exists(SolutionPath) && OpenSolutionModels.Count > 0;


		[UsedImplicitly]
		public void OpenFolder() {
			Process.Start(Path);
		}
	}
}