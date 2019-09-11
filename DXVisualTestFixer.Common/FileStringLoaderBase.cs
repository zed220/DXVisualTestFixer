using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace DXVisualTestFixer.Common {
    public abstract class FileStringLoaderBase {
        readonly string _ServerPath;
        DateTime downloadedTime;

        public FileStringLoaderBase(string serverPath) => _ServerPath = serverPath;

        List<string> _Result;

        public List<string> Result {
            get {
                if(_Result == null || DateTime.Now - downloadedTime > TimeSpan.FromMinutes(5))
                    _Result = Load();
                return _Result;
            }
        }

        List<string> Load() {
            if(!File.Exists(_ServerPath))
                return LoadIfFileNotFound();
            downloadedTime = DateTime.Now;
            return File.ReadAllLines(_ServerPath).ToList();
        }

        protected abstract List<string> LoadIfFileNotFound();
    }
}
