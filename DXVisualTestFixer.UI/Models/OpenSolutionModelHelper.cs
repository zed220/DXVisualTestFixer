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
		class CodeApp {
			public CodeApp(string path, string name, BitmapSource icon) {
				Path = path;
				Name = name;
				Icon = icon;
			}
			
			public readonly string Path;
			public readonly string Name;
			public readonly BitmapSource Icon;
		}
		
		static List<CodeApp> CodeApps;
		
		public static List<OpenSolutionModel> GetOpenSolutionModels(string solutionPath) {
			if(CodeApps == null) {
				CodeApps = new List<CodeApp>();
				FillCodeApps(solutionPath);
			}
			return CodeApps.Select(app => new OpenSolutionModel(solutionPath, app.Path, app.Name, app.Icon)).ToList();
		}
		
		static void FillCodeApps(string solutionPath) {
			if(File.Exists(GetAssociatedProgram()))
				CodeApps.Add(new CodeApp(GetAssociatedProgram(), "Associated", GetImageAssociated(solutionPath)));
			foreach(var vsPath in GetVSPaths())
				CodeApps.Add(new CodeApp(vsPath, GetExeDisplayText(vsPath), GetImageFromExe(vsPath)));
			foreach(var riderPath in GetRiderPaths())
				CodeApps.Add(new CodeApp(riderPath, GetExeDisplayText(riderPath), GetImageFromExe(riderPath)));
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
		
		static BitmapSource GetImageAssociated(string solutionPath) {
			if(solutionPath == null || !File.Exists(solutionPath))
				return null;
			try {
				var appIcon = Icon.ExtractAssociatedIcon(solutionPath);
				var bitmap = appIcon.ToBitmap();
				var hBitmap = bitmap.GetHbitmap();

				var result = Imaging.CreateBitmapSourceFromHBitmap(hBitmap, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
				if(result.IsDownloading) {
					
				}
				result.Freeze();
				return result;
			}
			catch {
				return null;
			}
		}

		static BitmapSource GetImageFromExe(string path) {
			try {
				var appIcon = Icon.ExtractAssociatedIcon(path);
				var bitmap = appIcon.ToBitmap();
				var hBitmap = bitmap.GetHbitmap();
				
				var result = Imaging.CreateBitmapSourceFromHBitmap(hBitmap, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
				if(result.IsDownloading) {
					
				}
				result.Freeze();
				return result;
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