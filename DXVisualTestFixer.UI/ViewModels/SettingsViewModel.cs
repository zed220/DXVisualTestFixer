using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using DevExpress.Utils.CommonDialogs;
using DevExpress.Utils.CommonDialogs.Internal;
using DevExpress.Xpf.Bars;
using DevExpress.Xpf.Core;
using DXVisualTestFixer.Common;
using DXVisualTestFixer.UI.Models;
using JetBrains.Annotations;
using Microsoft.Practices.ServiceLocation;
using Prism.Interactivity.InteractionRequest;

namespace DXVisualTestFixer.UI.ViewModels {
	public class PlatformModel : BindableBase {
		public PlatformModel(IPlatformInfo platform, ObservableCollection<RepositoryModel> repositories) {
			Platform = platform;
			Repositories = repositories;
		}

		public IPlatformInfo Platform { get; }
		public ObservableCollection<RepositoryModel> Repositories { get; }
		public override string ToString() => Platform.Name;
	}
	
	public class SettingsViewModel : ViewModelBase, ISettingsViewModel {
		readonly IConfigSerializer _configSerializer;
		readonly IPlatformProvider _platformProvider;

		IConfig _config;
		ObservableCollection<PlatformModel> _platformModels;
		string _themeName;
		string _workingDirectory;
		PlatformModel _defaultPlatform; 

		public SettingsViewModel(IConfigSerializer configSerializer) {
			Title = "Settings";
			_configSerializer = configSerializer;
			_platformProvider = ServiceLocator.Current.GetInstance<IPlatformProvider>();
			Config = configSerializer.GetConfig();
			Commands = UICommand.GenerateFromMessageButton(MessageButton.OKCancel, new DialogService(), MessageResult.OK, MessageResult.Cancel);
			Commands.Single(c => c.IsDefault).Command = new DelegateCommand(Save, () => !IsAnyRepositoryDownloading() && DefaultPlatform != null);
			Commands.Single(c => c.IsCancel).Command = new DelegateCommand(Cancel, () => !IsAnyRepositoryDownloading() && DefaultPlatform != null);
		}

		[UsedImplicitly]
		public ObservableCollection<PlatformModel> PlatformModels {
			get => _platformModels;
			set => SetProperty(ref _platformModels, value);
		}

		[PublicAPI]
		public PlatformModel DefaultPlatform {
			get => _defaultPlatform;
			set => SetProperty(ref _defaultPlatform, value,() => Config.DefaultPlatform = DefaultPlatform?.Platform.Name);
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
			foreach(var repo in PlatformModels.SelectMany(r => r.Repositories).Where(r => r.DownloadState == DownloadState.ReadyToDownload)) {
				repo.Path = Path.Combine(WorkingDirectory, string.Format(GetPlatform(_platformProvider, repo.Repository.Platform).LocalPath, repo.Version));
				repo.UpdateDownloadState();
			}
			foreach(var platformModel in PlatformModels) {
				RepositoryModel.ActualizeRepositories(platformModel.Platform, platformModel.Repositories, WorkingDirectory);	
			}
		}

		static IPlatformInfo GetPlatform(IPlatformProvider platformProvider, string platform) {
			return platformProvider.PlatformInfos.Single(i => i.Name == platform);
		}

		void OnConfigChanged() {
			LoadRepositories();
			ThemeName = Config.ThemeName;
			WorkingDirectory = Config.WorkingDirectory;
			DefaultPlatform = PlatformModels.FirstOrDefault(p => p.Platform.Name == Config.DefaultPlatform);
		}

		void LoadRepositories() {
			if(PlatformModels != null)
				PlatformModels.CollectionChanged -= Repositories_CollectionChanged;
			PlatformModels = new ObservableCollection<PlatformModel>();
			foreach(var platformAndRepo in (Config.Repositories ?? Enumerable.Empty<Repository>()).GroupBy(x => x.Platform)) {
				var platform = GetPlatform(_platformProvider, platformAndRepo.Key);
				PlatformModels.Add(new PlatformModel(platform, platformAndRepo.Select(r => new RepositoryModel(r, platform)).ToObservableCollection()));
			}
			PlatformModels.CollectionChanged += Repositories_CollectionChanged;
		}

		void Repositories_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e) {
			Config.Repositories = PlatformModels.SelectMany(r => r.Repositories).Select(r => r.Repository).ToArray();
		}

		bool IsChanged() {
			return !_configSerializer.IsConfigEquals(_configSerializer.GetConfig(false), Config);
		}

		bool IsAnyRepositoryDownloading() {
			return PlatformModels.SelectMany(r => r.Repositories).Any(repo => repo.DownloadState == DownloadState.Downloading);
		}

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
			foreach(var repository in PlatformModels.SelectMany(p => p.Repositories))
				repository.UpdateDownloadState();
		}
	}
}