using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DXVisualTestFixer.Farm {
    public interface ILog { }

    public static class Log {
        public static void Error(string text, Exception e = null) { }

        public static void Message(string text) { }
    }
}
