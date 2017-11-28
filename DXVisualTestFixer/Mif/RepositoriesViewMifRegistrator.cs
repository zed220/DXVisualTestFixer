using DevExpress.Mvvm.ModuleInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DXVisualTestFixer.Mif {
    public class RepositoriesViewMifRegistrator : IMifRegistrator {
        public void Dispose() {
            ModuleManager.DefaultManager.Clear(Regions.Main);
        }

        public void RegisterUI() {
            ModuleManager.DefaultManager.InjectOrNavigate(Regions.Main, Modules.Main);
        }

        public bool LoadState(string logicalstate, string visualState) {
            return false;
        }

        public void Reset() {
        }
    }
}
