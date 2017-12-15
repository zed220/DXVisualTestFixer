using DXVisualTestFixer.Configuration;
using KellermanSoftware.CompareNetObjects;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace DXVisualTestFixer.Core {
    public static class TestsService {
        static readonly List<Team> Teams = new List<Team>() {
            new Team() { Name = "Grid", ServerFolderName = "4DXGridTeam", TestResourcesPath = "DevExpress.Xpf.VisualTests\\DevExpress.Xpf.VisualTests\\WpfGrid\\ThemesImages", SupportOptimized = true },
            new Team() { Name = "TabControl", ServerFolderName = "4DXTabControl", TestResourcesPath = "DevExpress.Xpf.VisualTests\\DevExpress.Xpf.VisualTests\\WpfCore\\ThemesImages" },
            new Team() { Name = "CoreAndEditors", ServerFolderName = "4DXCoreTeam", TestResourcesPath = "DevExpress.Xpf.VisualTests\\DevExpress.Xpf.VisualTests\\WpfCore\\ThemesImages" },
            new Team() { Name = "Pivot", ServerFolderName = "4Pivot", TestResourcesPath = "DevExpress.Xpf.VisualTests\\DevExpress.Xpf.VisualTests\\WpfPivot\\ThemesImages" },
            new Team() { Name = "Scheduler", ServerFolderName = "4WpfScheduler", TestResourcesPath = "DevExpress.Xpf.VisualTests\\DevExpress.Xpf.VisualTests\\WpfScheduler\\ThemesImages" },
            new Team() { Name = "Navigation", ServerFolderName = "4XPFNavigationTeam", TestResourcesPath = "DevExpress.Xpf.VisualTests\\DevExpress.Xpf.VisualTests\\Navigation\\ThemesImages" },
            new Team() { Name = "Svg", ServerFolderName = "Svg2Images", TestResourcesPath = "DevExpress.Xpf.VisualTests\\DevExpress.Xpf.VisualTests\\WpfCore\\Svg2Images" },
        };

        public static List<TestInfo> Load(List<FarmTaskInfo> farmTasks) {
            List<TestInfo> result = new List<TestInfo>();
            foreach(FarmTaskInfo farmTaskInfo in farmTasks) {
                foreach(CorpDirTestInfo corpDirTestInfo in TestLoader.LoadFromInfo(farmTaskInfo)) {
                    TestInfo testInfo = new TestInfo();
                    testInfo.Name = corpDirTestInfo.TestName;
                    testInfo.Team = Teams.First(t => t.ServerFolderName == corpDirTestInfo.TeamName);
                    testInfo.Theme = corpDirTestInfo.ThemeName;
                    if(testInfo.Team.SupportOptimized)
                        testInfo.Optimized = !farmTaskInfo.Url.Contains("UnoptimizedMode");
                    testInfo.Version = farmTaskInfo.Repository.Version;
                    LoadTextFile(corpDirTestInfo.InstantTextEditPath, s => { testInfo.TextBefore = s; });
                    LoadTextFile(corpDirTestInfo.CurrentTextEditPath, s => { testInfo.TextCurrent = s; });
                    BuildTextDiff(testInfo);
                    LoadImage(corpDirTestInfo.InstantImagePath, s => { testInfo.ImageBeforeArr = s; });
                    LoadImage(corpDirTestInfo.CurrentImagePath, s => { testInfo.ImageCurrentArr = s; });
                    LoadImage(corpDirTestInfo.ImageDiffPath, s => { testInfo.ImageDiffArr = s; });
                    if(TestValid(testInfo))
                        result.Add(testInfo);
                }
            }
            return result;
        }
        
        static void LoadTextFile(string path, Action<string> saveAction) {
            string pathWithExtension = Path.ChangeExtension(path, ".xml");
            if(!File.Exists(pathWithExtension)) {
                //log
                Debug.WriteLine("fire LoadTextFile");
                return;
            }
            saveAction(File.ReadAllText(pathWithExtension));
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
            string differences = null;
            if(!IsTextEquals(testInfo.TextBefore, testInfo.TextCurrent, out differences))
                testInfo.TextDiff = differences;
        }
        static bool IsTextEquals(string left, string right, out string diff) {
            CompareLogic compareLogic = new CompareLogic();
            ComparisonResult result = compareLogic.Compare(left, right);
            diff = null;
            if(!result.AreEqual)
                diff = result.DifferencesString;
            return result.AreEqual;
        }
        static bool IsImageEquals(byte[] left, byte[] right) {
            CompareLogic compareLogic = new CompareLogic();
            Bitmap imgLeft = null;
            using(MemoryStream s = new MemoryStream(left)) {
                imgLeft = Image.FromStream(s) as Bitmap;
            }
            Bitmap imgRight = null;
            using(MemoryStream s = new MemoryStream(right)) {
                imgRight = Image.FromStream(s) as Bitmap;
            }
            return IsImageEqualsCore(imgLeft, imgRight);
        }
        static bool IsImageEqualsCore(Bitmap firstImage, Bitmap secondImage) {
            if(firstImage == null || secondImage == null) {
                //log
                Debug.WriteLine("fire IsImageEqualsCore");
                return false;
            }
            if(firstImage.Width != secondImage.Width || firstImage.Height != secondImage.Height)
                return false;
            for(int i = 0; i < firstImage.Width; i++) {
                for(int j = 0; j < firstImage.Height; j++) {
                    string firstPixel = firstImage.GetPixel(i, j).ToString();
                    string secondPixel = secondImage.GetPixel(i, j).ToString();
                    if(firstPixel != secondPixel) {
                        return false;
                    }
                }
            }
            return true;
        }

        static bool LoadImage(string path, Action<byte[]> saveAction) {
            if(!File.Exists(path)) {
                //log
                Debug.WriteLine("fire LoadImage");
                return false;
            }
            saveAction(File.ReadAllBytes(path));
            return true;
        }

        static bool TestValid(TestInfo test) {
            string actualTestResourceName = GetTestResourceName(test);
            string xmlPath = GetXmlFilePath(actualTestResourceName, test);
            if(xmlPath == null) {
                //log
                return false;
            }
            string imagePath = GetImageFilePath(actualTestResourceName, test);
            if(imagePath == null) {
                //log
                return false;
            }
            if(!IsTextEquals(test.TextBefore, File.ReadAllText(xmlPath), out _)) {
                //log -> outdated sources
                return false;
            }
            if(!String.IsNullOrEmpty(test.TextDiff))
                return true;
            byte[] imageSource = null;
            if(!LoadImage(imagePath, img => imageSource = img)) {
                //log
                Debug.WriteLine("fire GetActualFailedTests imageSource");
                return false;
            }
            if(IsImageEquals(test.ImageCurrentArr, imageSource))
                return false;
            return true;
        }

        static string GetTestResourceName(TestInfo test) {
            var repository = ConfigSerializer.GetConfig().Repositories.Where(r => r.Version == test.Version).FirstOrDefault();
            if(repository == null) {
                //log
                Debug.WriteLine("fire GetActualFailedTests repository");
                return null;
            }
            string actualTestResourcesPath = Path.Combine(repository.Path, test.Team.TestResourcesPath, test.Name);
            if(!Directory.Exists(actualTestResourcesPath)) {
                //log;
                Debug.WriteLine("fire GetActualFailedTests actualTestResourcesPath");
                return null;
            }
            return Path.Combine(actualTestResourcesPath, test.Theme);
        }
        static string GetXmlFilePath(string testResourceName, TestInfo test) {
            if(test.Optimized)
                testResourceName = testResourceName + "_optimized";
            string xmlPath = Path.ChangeExtension(testResourceName, "xml");
            if(!File.Exists(xmlPath)) {
                //log
                Debug.WriteLine("fire GetActualFailedTests xmlPath");
                return null;
            }
            return xmlPath;
        }
        static string GetImageFilePath(string testResourceName, TestInfo test) {
            string imagePath = Path.ChangeExtension(testResourceName, "png");
            if(!File.Exists(imagePath)) {
                //log
                Debug.WriteLine("fire GetActualFailedTests imagePath");
                return null;
            }
            return imagePath;
        }

        public static bool ApplyTest(TestInfo test, Func<string, bool> checkoutFunc) {
            string actualTestResourceName = GetTestResourceName(test);
            string xmlPath = GetXmlFilePath(actualTestResourceName, test);
            string imagePath = GetImageFilePath(actualTestResourceName, test);
            if(imagePath == null || xmlPath == null) {
                //log
                //Debug.WriteLine("fire save test" + test.ToLog());
                return false;
            }
            if(!SafeDeleteFile(xmlPath, checkoutFunc))
                return false;
            if(!SafeDeleteFile(imagePath, checkoutFunc))
                return false;
            File.WriteAllText(xmlPath, test.TextCurrent);
            File.WriteAllBytes(imagePath, test.ImageCurrentArr);
            return true;
        }
        static bool SafeDeleteFile(string path, Func<string, bool> checkoutFunc) {
            if((File.GetAttributes(path) & FileAttributes.ReadOnly) != FileAttributes.ReadOnly)
                return true;
            if(checkoutFunc(path)) {
                if((File.GetAttributes(path) & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
                    return false;
                File.Delete(path);
                return true;
            }
            return false;
        }
    }
}
