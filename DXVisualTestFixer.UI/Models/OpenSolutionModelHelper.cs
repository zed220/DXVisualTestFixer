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
using Microsoft.Win32;

namespace DXVisualTestFixer.UI.Models {
	public static class OpenSolutionModelHelper {
		static List<OpenSolutionModel> OpenSolutionModels;
		
		public static List<OpenSolutionModel> GetOpenSolutionModels(string solutionPath) {
			if(OpenSolutionModels == null) {
				OpenSolutionModels = new List<OpenSolutionModel>();
				FillOpenSolutionsModels(solutionPath);
			}
			return OpenSolutionModels;
		}
		
		static void FillOpenSolutionsModels(string solutionPath) {
			if(File.Exists(GetAssociatedProgram()))
				OpenSolutionModels.Add(new OpenSolutionModel(solutionPath, GetAssociatedProgram(), "Associated", GetImageAssociated(solutionPath)));
			foreach(var vsPath in GetVSPaths())
				OpenSolutionModels.Add(new OpenSolutionModel(solutionPath, vsPath, GetExeDisplayText(vsPath), GetImageFromExe(vsPath)));
			foreach(var riderPath in GetRiderPaths())
				OpenSolutionModels.Add(new OpenSolutionModel(solutionPath, riderPath, GetExeDisplayText(riderPath), GetImageFromExe(riderPath)));
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
		
		static ImageSource GetImageAssociated(string solutionPath) {
			if(solutionPath == null || !File.Exists(solutionPath))
				return null;
			try {
				var appIcon = Icon.ExtractAssociatedIcon(solutionPath);
				var bitmap = appIcon.ToBitmap();
				var hBitmap = bitmap.GetHbitmap();

				return Imaging.CreateBitmapSourceFromHBitmap(hBitmap, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
			}
			catch {
				return null;
			}
		}

		static ImageSource GetImageFromExe(string path) {
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
	}
}