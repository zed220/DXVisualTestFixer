using DXVisualTestFixer.UI.Models;
using DXVisualTestFixer.UI.ViewModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace DXVisualTestFixer.UI.Common {
    public class FilePreviewTemplateSelector : DataTemplateSelector {
        public DataTemplate ImageTemplate { get; set; }
        public DataTemplate TextTemplate { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container) {
            RepositoryFileModel repositoryFileModel = item as RepositoryFileModel;
            if(item == null)
                return null;
            switch(Path.GetExtension(repositoryFileModel.Path)) {
                case ".png":
                case ".jpg":
                case ".jpeg":
                    return ImageTemplate;
                case ".xml":
                default:
                    return TextTemplate;
            }
        }
    }
}
