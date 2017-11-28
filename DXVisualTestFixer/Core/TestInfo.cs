using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace DXVisualTestFixer.Core {
    public class TestInfo {
        public string Name { get; set; }
        public string Theme { get; set; }
        public string Fixture { get; set; }
        public string Version { get; set; }
        public int Dpi { get; set; }
        public ImageSource ImageBefore { get; set; }
        public ImageSource ImageCurrent { get; set; }
        public ImageSource ImageDiff { get; set; }
        public string TextBefore { get; set; }
        public string TextCurrent { get; set; }
        public string TextDiff { get; set; }
    }
}
