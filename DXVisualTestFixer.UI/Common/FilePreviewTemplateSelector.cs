using System.IO;
using System.Windows;
using System.Windows.Controls;
using DXVisualTestFixer.UI.Models;

namespace DXVisualTestFixer.UI.Common {
	public class FilePreviewTemplateSelector : DataTemplateSelector {
		public DataTemplate ImageTemplate { get; set; }
		public DataTemplate TextTemplate { get; set; }

		public override DataTemplate SelectTemplate(object item, DependencyObject container) {
			var repositoryFileModel = item as RepositoryFileModel;
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