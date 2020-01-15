using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using DevExpress.Mvvm;
using DevExpress.Utils.CommonDialogs;
using DevExpress.Utils.CommonDialogs.Internal;
using DevExpress.Xpf.Core;
using DXVisualTestFixer.Common;
using DXVisualTestFixer.UI.Models;
using JetBrains.Annotations;
using Microsoft.Practices.ServiceLocation;
using Prism.Interactivity.InteractionRequest;

namespace DXVisualTestFixer.UI.ViewModels {
	public class SettingsViewModel : ViewModelBase, ISettingsViewModel {
		readonly IConfigSerializer _configSerializer;

		IConfig _config;
		ObservableCollection<RepositoryModel> _repositories;
		string _themeName;
		string _workingDirectory;

		public SettingsViewModel(IConfigSerializer configSerializer) {
			Title = "Settings";
			_configSerializer = configSerializer;
			Config = configSerializer.GetConfig();
			Commands = UICommand.GenerateFromMessageButton(MessageButton.OKCancel, new DialogService(), MessageResult.OK, MessageResult.Cancel);
			Commands.Single(c => c.IsDefault).Command = new DelegateCommand(Save, () => !IsAnyRepositoryDownloading());
			Commands.Single(c => c.IsCancel).Command = new DelegateCommand(Cancel, () => !IsAnyRepositoryDownloading());
		}

		[UsedImplicitly]
		public ObservableCollection<RepositoryModel> Repositories {
			get => _repositories;
			set => SetProperty(ref _repositories, value);
		}

		[PublicAPI]
		public string ThemeName {
			get => _themeName;
			set { SetProperty(ref _themeName, value, () => Config.ThemeName = ThemeName); }
		}

		[UsedImplicitly]
		public string WorkingDirectory {
			get => _workingDirectory;
			set => SetProperty(ref _workingDirectory, value, OnWorkingDirectoryChanged);
		}

		[UsedImplicitly] public IEnumerable<UICommand> Commands { get; }

		[UsedImplicitly] public InteractionRequest<IConfirmation> ConfirmationRequest { get; } = new InteractionRequest<IConfirmation>();

		public IConfig Config {
			get => _config;
			private set => SetProperty(ref _config, value, OnConfigChanged);
		}

		public bool Confirmed { get; set; }
		public string Title { get; set; }
		public object Content { get; set; }

		void OnWorkingDirectoryChanged() {
			Config.WorkingDirectory = WorkingDirectory;
			foreach(var repo in Repositories.Where(r => r.DownloadState == DownloadState.ReadyToDownload)) {
				repo.Path = Path.Combine(WorkingDirectory, $"20{repo.Version}_VisualTests");
				repo.UpdateDownloadState();
			}

			RepositoryModel.ActualizeRepositories(Repositories, WorkingDirectory);
		}

		void OnConfigChanged() {
			LoadRepositories();
			ThemeName = Config.ThemeName;
			WorkingDirectory = Config.WorkingDirectory;
		}

		void LoadRepositories() {
			if(Repositories != null)
				Repositories.CollectionChanged -= Repositories_CollectionChanged;
			Repositories = new ObservableCollection<RepositoryModel>((Config.Repositories ?? new Repository[0]).Select(r => new RepositoryModel(r)));
			Repositories.CollectionChanged += Repositories_CollectionChanged;
		}

		void Repositories_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e) {
			Config.Repositories = Repositories.Select(r => r.Repository).ToArray();
		}

		bool IsChanged() {
			return !_configSerializer.IsConfigEquals(_configSerializer.GetConfig(false), Config);
		}

		bool IsAnyRepositoryDownloading() => Repositories.Any(repo => repo.DownloadState == DownloadState.Downloading);

		void Save() {
			if(IsAnyRepositoryDownloading())
				return;
			if(!IsChanged())
				return;
			Confirmed = true;
		}

		void Cancel() {
			if(IsAnyRepositoryDownloading())
				return;
			if(!IsChanged())
				return;
			if(CheckConfirmation(ConfirmationRequest, "Save changes?", "Save changes?"))
				Save();
		}

		[UsedImplicitly]
		public void SelectWorkDirectory() {
			var dialog = ServiceLocator.Current.TryResolve<IFolderBrowserDialog>();
			var result = dialog.ShowDialog();
			if(result != DialogResult.OK)
				return;
			if(!Directory.Exists(dialog.SelectedPath))
				return;
			WorkingDirectory = dialog.SelectedPath;
			foreach(var repository in Repositories)
				repository.UpdateDownloadState();
		}
	}
}