using System;
using System.IO;
using System.Linq;

namespace DXVisualTestFixer.Common {
    public static class CmdWhere {
        public static bool TryFind(string nameWinExtension, out string programPath) {
            foreach(var envDir in Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.Machine)?.Split(';') ?? new string[0]) {
                var fullPath = Path.Combine(envDir, nameWinExtension);
                if(!File.Exists(fullPath)) 
                    continue;
                programPath = fullPath;
                return true;
            }
            programPath = null;
            return false;
        }
    }
}