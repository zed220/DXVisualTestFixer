using System;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DXVisualTestFixer.Git {
    static class ProcessHelper {
        public static int WaitForProcess(string fileName, string workingDir, out string output, out string errors, params string[] args) {
            var proc = new Process();
            proc.StartInfo.WorkingDirectory = workingDir;
            proc.StartInfo.FileName = fileName;
            proc.StartInfo.Arguments = args.Length == 0 ? "" : args.Aggregate((l, r) => l + " " + r);
            proc.StartInfo.UseShellExecute = false;
            proc.StartInfo.CreateNoWindow = true;
            proc.StartInfo.RedirectStandardOutput = true;
            proc.StartInfo.RedirectStandardError = true;
            proc.Start();
            if(!proc.HasExited) {
                try {
                    proc.PriorityClass = ProcessPriorityClass.High;
                }
                catch { }
            }


            var outputTask = Task.Factory.StartNew(() => proc.StandardOutput.ReadToEnd());
            var errorTask = Task.Factory.StartNew(() => proc.StandardError.ReadToEnd());

            output = string.Empty;
            errors = string.Empty;

            if(!proc.WaitForExit(12000000)) {
                proc.Kill();
                throw new Exception("process timed out");
            }

            Task.WaitAll(outputTask, errorTask);
            output = outputTask.Result;
            errors = errorTask.Result;
            return proc.ExitCode;
        }

        public static void CheckFail(int code, string output, string errors) {
            if(code == 0) return;
            var sb = new StringBuilder();
            sb.AppendLine("Git invocation failed.");
            sb.AppendLine("Git return output:");
            sb.AppendLine(output);
            sb.AppendLine("Git return errors:");
            sb.AppendLine(errors);
            throw new Exception(sb.ToString());
        }
    }
}