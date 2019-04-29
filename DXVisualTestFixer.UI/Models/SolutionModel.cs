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
using DXVisualTestFixer.Common;
using Microsoft.Win32;

namespace DXVisualTestFixer.UI.Models {
    public class OpenSolutionModel : ImmutableObject {
        readonly string solutionPath;
        readonly string associatedProgramPath;
        
        public OpenSolutionModel(string solutionPath, string associatedProgramPath, string programName, ImageSource programImage) {
            this.solutionPath = solutionPath;
            this.associatedProgramPath = associatedProgramPath;
            ProgramName = programName;
            ProgramImage = programImage;
        }

        public string ProgramName { get; }
        public ImageSource ProgramImage { get; }

        public void Open() {
            ProcessStartInfo info = new ProcessStartInfo(associatedProgramPath, solutionPath);
            info.Verb = "runas";
            Process.Start(info);
        }
    }

    public class SolutionModel : ImmutableObject {
        readonly string SolutionPath;

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
        public bool IsEnabled => File.Exists(SolutionPath) && (CanOpenByVS || CanOpenByRider);

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
                Icon appIcon = Icon.ExtractAssociatedIcon(SolutionPath);
                Bitmap bitmap = appIcon.ToBitmap();
                IntPtr hBitmap = bitmap.GetHbitmap();

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
                Icon appIcon = Icon.ExtractAssociatedIcon(GetRiderPath());
                Bitmap bitmap = appIcon.ToBitmap();
                IntPtr hBitmap = bitmap.GetHbitmap();
                return Imaging.CreateBitmapSourceFromHBitmap(hBitmap, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
            }
            catch {
                return null;
            }
        }

        static string GetRiderPath() {
            var pathToJB = System.IO.Path.Combine(Environment.ExpandEnvironmentVariables("%ProgramW6432%"), "JetBrains");
            if(!Directory.Exists(pathToJB))
                return null;
            return Directory.GetFiles(pathToJB, "rider64.exe", SearchOption.AllDirectories).LastOrDefault();
        }

        public bool CanOpenByVS {
            get { return File.Exists(GetAssociatedProgram()); }
        }
        public bool CanOpenByRider {
            get { return File.Exists(GetRiderPath()); }
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
    }
}
