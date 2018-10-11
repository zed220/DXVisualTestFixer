using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using DevExpress.Utils;
using DXVisualTestFixer.Common;
using Microsoft.Win32;

namespace DXVisualTestFixer.UI.Models {
    public class SolutionModel : ImmutableObject {
        readonly string SolutionPath;

        public SolutionModel(string version, string path) {
            Version = version;
            Path = GetRealPath(path);
            SolutionPath = Directory.EnumerateFiles(Path, "*.sln", SearchOption.TopDirectoryOnly).FirstOrDefault();
            Image = GetImage();
        }

        public string Version { get; }
        public string Path { get; }
        public ImageSource Image { get; }

        ImageSource GetImage() {
            if(SolutionPath == null || !File.Exists(SolutionPath))
                return null;
            try {
                Icon appIcon = Icon.ExtractAssociatedIcon(SolutionPath);
                Bitmap bitmap = appIcon.ToBitmap();
                IntPtr hBitmap = bitmap.GetHbitmap();

                return Imaging.CreateBitmapSourceFromHBitmap(hBitmap, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
            }
            catch {
                return null;
            }
        }
        public void OpenSolution() {
            if(SolutionPath == null || !File.Exists(SolutionPath))
                return;
            string associatedProgramPath = GetAssociatedProgram();
            if(!File.Exists(associatedProgramPath))
                return;
            ProcessStartInfo info = new ProcessStartInfo(associatedProgramPath, SolutionPath);
            info.Verb = "runas";
            Process.Start(info);
        }
        static string GetAssociatedProgram() {
            try {
                var objExtReg = Registry.ClassesRoot.OpenSubKey(".sln");
                var strExtValue = objExtReg.GetValue("");
                var objAppReg = Registry.ClassesRoot.OpenSubKey(strExtValue + @"\shell\open\command");
                return (objAppReg.GetValue(null)?.ToString() ?? string.Empty).Replace("\"%1\"", String.Empty).Replace("%1", String.Empty).Replace("\"", string.Empty);
            }
            catch {
                return @"C:\Program Files (x86)\Common Files\Microsoft Shared\MSEnv\VSLauncher.exe";
            }
        }
        public void OpenFolder() {
            Process.Start(Path);
        }
        string GetRealPath(string path) {
            string folderName = Repository.IsNewVersion(Version) ? "VisualTests" : "DevExpress.Xpf.VisualTests";
            return System.IO.Path.Combine(path, folderName);
        }
    }
}
