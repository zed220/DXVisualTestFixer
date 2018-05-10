using DXVisualTestFixer.UI.ViewModels;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace DXVisualTestFixer.UI.Converters {
    public class FilePathToImageSourceConverter : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            RepositoryFileModel repositoryFileModel = value as RepositoryFileModel;
            if(repositoryFileModel == null)
                return null;
            return new Uri(repositoryFileModel.Path, UriKind.Absolute);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            throw new NotImplementedException();
        }
    }
}
