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
	public class OpenSolutionModel : ImmutableObject {
		readonly string _associatedProgramPath;
		readonly string _solutionPath;

		public OpenSolutionModel(string solutionPath, string associatedProgramPath, string programName, ImageSource programImage) {
			_solutionPath = solutionPath;
			_associatedProgramPath = associatedProgramPath;
			ProgramName = programName;
			ProgramImage = programImage;
		}

		public string ProgramName { get; }
		public ImageSource ProgramImage { get; }

		[UsedImplicitly]
		public void Open() {
			var info = new ProcessStartInfo(_associatedProgramPath, _solutionPath);
			info.Verb = "runas";
			Process.Start(info);
		}
	}

	public class SolutionModel : ImmutableObject {
		[CanBeNull] readonly string SolutionPath;

		public SolutionModel(string version, string path) {
			Version = version;
			Path = System.IO.Path.Combine(path, "VisualTests");
			SolutionPath = Directory.EnumerateFiles(Path, "*.sln", SearchOption.TopDirectoryOnly).FirstOrDefault();
			OpenSolutionModels = new List<OpenSolutionModel>();
			FillOpenSolutionsModels();
		}

		public string Version { get; }
		public string Path { get; }
		public List<OpenSolutionModel> OpenSolutionModels { get; }

		[UsedImplicitly] public bool IsEnabled => File.Exists(SolutionPath) && (CanOpenByVS || CanOpenByRider);

		public bool CanOpenByVS => File.Exists(GetAssociatedProgram());

		public bool CanOpenByRider => File.Exists(GetRiderPath());

		void FillOpenSolutionsModels() {
			if(CanOpenByVS)
				OpenSolutionModels.Add(new OpenSolutionModel(SolutionPath, GetAssociatedProgram(), "MS Visual Studio", GetImageVS()));
			if(CanOpenByRider)
				OpenSolutionModels.Add(new OpenSolutionModel(SolutionPath, GetRiderPath(), "JB Rider", GetImageRider()));
		}

		ImageSource GetImageVS() {
			if(SolutionPath == null || !File.Exists(SolutionPath))
				return null;
			try {
				var appIcon = Icon.ExtractAssociatedIcon(SolutionPath);
				var bitmap = appIcon.ToBitmap();
				var hBitmap = bitmap.GetHbitmap();

				return Imaging.CreateBitmapSourceFromHBitmap(hBitmap, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
			}
			catch {
				return null;
			}
		}

		ImageSource GetImageRider() {
			if(!CanOpenByRider)
				return null;
			try {
				var appIcon = Icon.ExtractAssociatedIcon(GetRiderPath());
				var bitmap = appIcon.ToBitmap();
				var hBitmap = bitmap.GetHbitmap();
				return Imaging.CreateBitmapSourceFromHBitmap(hBitmap, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
			}
			catch {
				return null;
			}
		}

		static string GetRiderPath() {
			var pathToJB = System.IO.Path.Combine(Environment.ExpandEnvironmentVariables("%ProgramW6432%"), "JetBrains");
			return !Directory.Exists(pathToJB) ? null : Directory.GetFiles(pathToJB, "rider64.exe", SearchOption.AllDirectories).LastOrDefault();
		}

		static string GetAssociatedProgram() {
			try {
				var objExtReg = Registry.ClassesRoot.OpenSubKey(".sln");
				var strExtValue = objExtReg.GetValue("");
				var objAppReg = Registry.ClassesRoot.OpenSubKey(strExtValue + @"\shell\open\command");
				return (objAppReg.GetValue(null)?.ToString() ?? string.Empty).Replace("\"%1\"", string.Empty).Replace("%1", string.Empty).Replace("\"", string.Empty);
			}
			catch {
				return @"C:\Program Files (x86)\Common Files\Microsoft Shared\MSEnv\VSLauncher.exe";
			}
		}

		[UsedImplicitly]
		public void OpenFolder() {
			Process.Start(Path);
		}
	}
}