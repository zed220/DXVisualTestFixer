using DevExpress.Xpf.Grid;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DXVisualTestFixer.Controls {
    public class GridControlEx : GridControl {
        public List<string> GetProducts() {
            return GetUniqueColumnValues(Columns["TeamName"], true, true).Cast<string>().ToList();
        }
    }
}
