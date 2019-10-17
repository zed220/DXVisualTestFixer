using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace DXVisualTestFixer.Common {
	public abstract class FileStringLoaderBase {
		readonly string serverPath;

		List<string> result;
		DateTime downloadedTime;

		protected FileStringLoaderBase(string serverPath) {
			this.serverPath = serverPath;
		}

		protected List<string> Result {
			get {
				if(result == null || DateTime.Now - downloadedTime > TimeSpan.FromMinutes(5))
					result = Load();
				return result;
			}
		}

		List<string> Load() {
			if(!File.Exists(serverPath))
				return LoadIfFileNotFound();
			downloadedTime = DateTime.Now;
			return File.ReadAllLines(serverPath).ToList();
		}

		protected abstract List<string> LoadIfFileNotFound();
	}
}