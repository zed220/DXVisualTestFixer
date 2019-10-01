using System.ComponentModel;
using DXVisualTestFixer.Common;
using Prism.Mvvm;

namespace DXVisualTestFixer.UI.Services {
	public class IsActiveService : BindableBase, IActiveService {
		bool _IsActive = true;
		
		public bool IsActive {
			get => _IsActive;
			set => SetProperty(ref _IsActive, value);
		}
	}
}