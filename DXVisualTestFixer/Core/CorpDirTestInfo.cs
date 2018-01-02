using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DXVisualTestFixer.Core {
    public class CorpDirTestInfo {
        public FarmTaskInfo FarmTaskInfo { get; private set; }

        public string CurrentTextEditPath { get; private set; }
        public string InstantTextEditPath { get; private set; }
        public string CurrentImagePath { get; private set; }
        public string InstantImagePath { get; private set; }
        public string ImageDiffPath { get; private set; }

        public string TeamName { get; private set; }
        public string TestName { get; private set; }
        public string ThemeName { get; private set; }

        public static bool TryCreate(FarmTaskInfo farmTaskInfo, List<string> corpPaths, out CorpDirTestInfo result) {
            result = null;
            CorpDirTestInfo temp = new CorpDirTestInfo();
            temp.FarmTaskInfo = farmTaskInfo;
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
            if(temp.CurrentTextEditPath != null && temp.InstantTextEditPath != null && temp.CurrentImagePath != null && temp.InstantImagePath != null) {// && temp.ImageDiffPath != null
                temp.TeamName = temp.CurrentTextEditPath.Split(new string[] { @"\\corp\builds\testbuilds\" }, StringSplitOptions.RemoveEmptyEntries).First().Split('\\').First();
                string[] testNameAndTheme = Path.GetDirectoryName(temp.CurrentTextEditPath).Split('\\').Last().Split('.');
                temp.TestName = testNameAndTheme[0];
                temp.ThemeName = testNameAndTheme[1];
                if(testNameAndTheme.Length > 2)
                    temp.ThemeName += '.' + testNameAndTheme[2];
                result = temp;
                return true;
            }
            return false;
        }
    }
}
