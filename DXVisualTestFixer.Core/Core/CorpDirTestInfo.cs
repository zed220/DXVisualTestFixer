using CommonServiceLocator;
using DXVisualTestFixer.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DXVisualTestFixer.Core {
    public class CorpDirTestInfo {
        public const string ErrorTeamName = "Error";

        public IFarmTaskInfo FarmTaskInfo { get; private set; }

        public string CurrentTextEditPath { get; private set; }
        public string InstantTextEditPath { get; private set; }
        public string CurrentImagePath { get; private set; }
        public string InstantImagePath { get; private set; }
        public string ImageDiffPath { get; private set; }

        public string TeamName { get; private set; }
        public string ServerFolderName { get; private set; }
        public string TestName { get; private set; }
        public string TestNameWithNamespace { get; private set; }
        public string ResourceFolderName { get; private set; }
        public string ThemeName { get; private set; }
        public string StackTrace { get; private set; }
        public string ErrorText { get; private set; }
        public int Dpi { get; private set; } = 96;

        static string GetTestName(string testNameAndNamespace) {
            if(!testNameAndNamespace.Contains('.'))
                return testNameAndNamespace;
            if(!testNameAndNamespace.Contains('('))
                return testNameAndNamespace.Split('.').Last();
            string firstTestNamePart = testNameAndNamespace.Split('(').First();
            return GetTestName(firstTestNamePart) + testNameAndNamespace.Remove(0, firstTestNamePart.Length);
        }

        public static CorpDirTestInfo CreateError(IFarmTaskInfo farmTaskInfo, string testNameAndNamespace, string errorText, string stackTrace) {
            CorpDirTestInfo result = new CorpDirTestInfo();
            result.FarmTaskInfo = farmTaskInfo;
            result.ErrorText = errorText;
            result.TeamName = "Error";
            result.StackTrace = stackTrace;
            result.TestName = GetTestName(testNameAndNamespace);
            result.TestNameWithNamespace = testNameAndNamespace;
            return result;
        }
        public static bool TryCreate(IFarmTaskInfo farmTaskInfo, string testNameAndNamespace, List<string> corpPaths, out CorpDirTestInfo result) {
            result = null;
            CorpDirTestInfo temp = new CorpDirTestInfo();
            temp.FarmTaskInfo = farmTaskInfo;
            temp.TestName = GetTestName(testNameAndNamespace);
            temp.TestNameWithNamespace = testNameAndNamespace;
            foreach(var path in corpPaths) {
                if(path.EndsWith("CurrentTextEdit.xml")) {
                    temp.CurrentTextEditPath = path;
                    continue;
                }
                if(path.EndsWith("InstantTextEdit.xml")) {
                    temp.InstantTextEditPath = path;
                    continue;
                }
                if(path.EndsWith("CurrentBitmap.png")) {
                    temp.CurrentImagePath = path;
                    continue;
                }
                if(path.EndsWith("InstantBitmap.png")) {
                    temp.InstantImagePath = path;
                    continue;
                }
                if(path.EndsWith("BitmapDif.png")) {
                    temp.ImageDiffPath = path;
                    continue;
                }
            }
            if(temp.CurrentTextEditPath != null && temp.CurrentImagePath != null) {// && temp.ImageDiffPath != null
                                                                                   //&& temp.InstantTextEditPath != null && temp.InstantImagePath != null
                temp.ServerFolderName = temp.CurrentTextEditPath.Split(new string[] { @"\\corp\builds\testbuilds\" }, StringSplitOptions.RemoveEmptyEntries).First().Split('\\').First();
                if(temp.ServerFolderName.Contains("_dpi_")) {
                    var nameAndDpi = temp.ServerFolderName.Split(new[] { "_dpi_" }, StringSplitOptions.RemoveEmptyEntries);
                    temp.TeamName = nameAndDpi[0];
                    temp.Dpi = Int32.Parse(nameAndDpi[1]);
                }
                else
                    temp.TeamName = temp.ServerFolderName;
                string folderNameAndTheme = Path.GetDirectoryName(temp.CurrentTextEditPath).Split('\\').Last();
                if(!TryUpdateThemeAndFolderName(folderNameAndTheme, temp))
                    return false;
                //string[] testNameAndTheme = Path.GetDirectoryName(temp.CurrentTextEditPath).Split('\\').Last().Split('.');
                //temp.TestName = testNameAndTheme[0];
                //temp.ThemeName = testNameAndTheme[1];
                //if(temp.InstantTextEditPath == null || temp.InstantImagePath == null) {
                //    temp.PossibleNewTest = true;
                //}
                //if(testNameAndTheme.Length > 2)
                //    temp.ThemeName += '.' + testNameAndTheme[2];
                result = temp;
                return true;
            }
            return false;
        }
        static bool TryUpdateThemeAndFolderName(string folderNameAndTheme, CorpDirTestInfo result) {
            List<string> allThemes = ServiceLocator.Current.GetInstance<IThemesProvider>().AllThemes.ToList();
            allThemes.Add("Base");
            allThemes.Add("Super");
            allThemes.Sort(new ThemeNameComparer());
            foreach(string theme in allThemes.Where(t => t.Contains("Touch")).Concat(allThemes.Where(t => !t.Contains("Touch")))) {
                string themeName = theme.Replace(";", ".");
                if(!folderNameAndTheme.Contains(themeName))
                    continue;
                result.ThemeName = themeName;
                result.ResourceFolderName = folderNameAndTheme.Replace("." + themeName, "");
                return true;
            }
            return false;
        }
    }

    class ThemeNameComparer : IComparer<string> {
        public int Compare(string x, string y) {
            if(x.Length > y.Length)
                return -1;
            if(x.Length < y.Length)
                return 1;
            return Comparer<string>.Default.Compare(x, y);
        }
    }
}
