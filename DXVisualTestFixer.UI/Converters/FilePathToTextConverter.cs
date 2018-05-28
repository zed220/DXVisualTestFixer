using DXVisualTestFixer.UI.Models;
using DXVisualTestFixer.UI.ViewModels;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace DXVisualTestFixer.UI.Converters {
    public class FilePathToTextConverter : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            RepositoryFileModel repositoryFileModel = value as RepositoryFileModel;
            if(repositoryFileModel == null)
                return null;
            if(!File.Exists(repositoryFileModel.Path))
                return null;
            return File.ReadAllText(repositoryFileModel.Path);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            throw new NotImplementedException();
        }
    }
}
