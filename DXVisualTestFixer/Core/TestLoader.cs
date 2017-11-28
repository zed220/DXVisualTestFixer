using KellermanSoftware.CompareNetObjects;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace DXVisualTestFixer.Core {
    public static class TestLoader {
        static string ServerPath = @"\\corp\builds\testbuilds\4DXGridTeam\";
        

        public static List<TestInfo> Load() {
            List<TestInfo> result = new List<TestInfo>();
            if(!Directory.Exists(ServerPath))
                return result;
            foreach(string verDir in Directory.GetDirectories(ServerPath)) {
                string lastDate = Directory.GetDirectories(verDir).OrderBy(d => d).LastOrDefault();
                if(String.IsNullOrEmpty(lastDate))
                    continue;
                string version = verDir.Split('\\').Last();
                foreach(var testDir in Directory.GetDirectories(lastDate)) {
                    TestInfo testInfo = new TestInfo() { Version = version };
                    UpdateTestInfo(testInfo, testDir.Split('\\').Last());
                    LoadTextFile(Path.Combine(testDir, "InstantTextEdit.xml"), s => { testInfo.TextBefore = s; });
                    LoadTextFile(Path.Combine(testDir, "CurrentTextEdit.xml"), s => { testInfo.TextCurrent = s; });
                    BuildTextDiff(testInfo);
                    LoadImage(Path.Combine(testDir, "InstantBitmap.png"), s => { testInfo.ImageBefore = s; });
                    LoadImage(Path.Combine(testDir, "CurrentBitmap.png"), s => { testInfo.ImageCurrent = s; });
                    LoadImage(Path.Combine(testDir, "BitmapDif.png"), s => { testInfo.ImageDiff = s; });
                    result.Add(testInfo);
                }
            }
            return result;
        }

        static void UpdateTestInfo(TestInfo testInfo, string testDirName) {
            string[] splitted = testDirName.Split('.');
            testInfo.Name = splitted[0];
            if(splitted.Length < 2) {
                //log
                return;
            }
            testInfo.Theme = splitted[1];
            if(splitted.Length > 2)
                testInfo.Theme += '.' + splitted[2];
        }
        static void LoadTextFile(string path, Action<string> saveAction) {
            if(!File.Exists(path)) {
                //log
                return;
            }
            saveAction(File.ReadAllText(path));
        }
        static void BuildTextDiff(TestInfo testInfo) {
            if(String.IsNullOrEmpty(testInfo.TextBefore) && String.IsNullOrEmpty(testInfo.TextCurrent))
                return;
            if(String.IsNullOrEmpty(testInfo.TextBefore)) {
                testInfo.TextDiff = testInfo.TextCurrent;
                return;
            }
            if(String.IsNullOrEmpty(testInfo.TextCurrent)) {
                testInfo.TextDiff = testInfo.TextBefore;
                return;
            }
            CompareLogic compareLogic = new CompareLogic();
            KellermanSoftware.CompareNetObjects.ComparisonResult result = compareLogic.Compare(testInfo.TextBefore, testInfo.TextCurrent);
            if(!result.AreEqual)
                testInfo.TextDiff = result.DifferencesString;
        }
        static void LoadImage(string path, Action<ImageSource> saveAction) {
            if(!File.Exists(path)) {
                //log
                return;
            }
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.UriSource = new Uri(path);
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.EndInit();
            saveAction(bitmap);
        }
    }
}
