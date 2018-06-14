using DXVisualTestFixer.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DXVisualTestFixer.Farm {
    public class FarmTaskInfo : IFarmTaskInfo {
        public FarmTaskInfo(Repository repository, string url) {
            Repository = repository;
            Url = url;
        }

        public override bool Equals(object obj) {
            if(obj == null || GetType() != obj.GetType())
                return false;
            FarmTaskInfo other = (FarmTaskInfo)obj;
            return Repository.Path == other.Repository.Path && Repository.Version == other.Repository.Version;
        }

        public override int GetHashCode() {
            return Repository.GetHashCode() ^ Repository.Version.GetHashCode();
        }

        public Repository Repository { get; private set; }
        public string Url { get; private set; }
        public bool Success { get; set; }
    }
}
