using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace DXVisualTestFixer.Common {
	public abstract class FileStringLoaderBase {
		readonly string _serverPath;
		DateTime _downloadedTime;

		List<string> _result;

		protected FileStringLoaderBase(string serverPath) {
			_serverPath = serverPath;
		}

		protected List<string> Result {
			get {
				if(_result == null || DateTime.Now - _downloadedTime > TimeSpan.FromMinutes(5))
					_result = Load();
				return _result;
			}
		}

		List<string> Load() {
			if(!File.Exists(_serverPath))
				return LoadIfFileNotFound();
			_downloadedTime = DateTime.Now;
			return File.ReadAllLines(_serverPath).ToList();
		}

		protected abstract List<string> LoadIfFileNotFound();
	}
}