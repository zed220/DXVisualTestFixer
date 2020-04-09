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

		[UsedImplicitly] public bool IsEnabled => File.Exists(SolutionPath) && (CanOpenByAssociated || CanOpenByVS || CanOpenByRider);

		public bool CanOpenByAssociated => File.Exists(GetAssociatedProgram());
		public bool CanOpenByVS => GetVSPaths().Any(File.Exists);
		public bool CanOpenByRider => GetRiderPaths().Any(File.Exists);

		void FillOpenSolutionsModels() {
			if(CanOpenByAssociated)
				OpenSolutionModels.Add(new OpenSolutionModel(SolutionPath, GetAssociatedProgram(), "Associated", GetImageAssociated()));
			foreach(var vsPath in GetVSPaths())
				OpenSolutionModels.Add(new OpenSolutionModel(SolutionPath, vsPath, GetExeDisplayText(vsPath), GetImageFromExe(vsPath)));
			foreach(var riderPath in GetRiderPaths())
				OpenSolutionModels.Add(new OpenSolutionModel(SolutionPath, riderPath, GetExeDisplayText(riderPath), GetImageFromExe(riderPath)));
		}

		static string GetExeDisplayText(string path) {
			try {
				var info = FileVersionInfo.GetVersionInfo(path);
				return info.ProductName + $" ({info.ProductVersion})";
			}
			catch {
				return System.IO.Path.GetFileNameWithoutExtension(path);
			}
		}
		
		ImageSource GetImageAssociated() {
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

		ImageSource GetImageFromExe(string path) {
			if(!CanOpenByRider)
				return null;
			try {
				var appIcon = Icon.ExtractAssociatedIcon(path);
				var bitmap = appIcon.ToBitmap();
				var hBitmap = bitmap.GetHbitmap();
				return Imaging.CreateBitmapSourceFromHBitmap(hBitmap, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
			}
			catch {
				return null;
			}
		}

		static IEnumerable<string> GetVSPaths() {
			var pathToVS = System.IO.Path.Combine(Environment.ExpandEnvironmentVariables("%programfiles(x86)%"), "Microsoft Visual Studio");
			return !Directory.Exists(pathToVS) ? Enumerable.Empty<string>() : Directory.GetFiles(pathToVS, "devenv.exe", SearchOption.AllDirectories);
		}
		static IEnumerable<string> GetRiderPaths() {
			var pathToJB = System.IO.Path.Combine(Environment.ExpandEnvironmentVariables("%ProgramW6432%"), "JetBrains");
			return !Directory.Exists(pathToJB) ? Enumerable.Empty<string>() : Directory.GetFiles(pathToJB, "rider64.exe", SearchOption.AllDirectories).Where(x => !x.Contains("Back"));
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