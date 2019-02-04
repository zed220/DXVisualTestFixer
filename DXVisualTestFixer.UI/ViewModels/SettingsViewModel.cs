using DevExpress.Mvvm;
using DevExpress.Mvvm.UI;
using DevExpress.Utils.CommonDialogs;
using DevExpress.Xpf.Core;
using DevExpress.Xpf.Dialogs;
using DevExpress.Xpf.Layout.Core;
using DXVisualTestFixer.Common;
using DXVisualTestFixer.UI.Models;
using Microsoft.Practices.ServiceLocation;
using Microsoft.Practices.Unity;
using Prism.Interactivity.InteractionRequest;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace DXVisualTestFixer.UI.ViewModels {
    public class SettingsViewModel : ViewModelBase, ISettingsViewModel {
        readonly IConfigSerializer configSerializer;

        IConfig _Config;
        ObservableCollection<RepositoryModel> _Repositories;
        string _ThemeName;
        string _WorkingDirectory;
        bool _IsLoading;

        public bool IsLoading {
            get { return _IsLoading; }
            private set { SetProperty(ref _IsLoading, value); }
        }
        public IConfig Config {
            get { return _Config; }
            private set { SetProperty(ref _Config, value, OnConfigChanged); }
        }
        public ObservableCollection<RepositoryModel> Repositories {
            get { return _Repositories; }
            set { SetProperty(ref _Repositories, value); }
        }
        public string ThemeName {
            get { return _ThemeName; }
            set { SetProperty(ref _ThemeName, value, () => Config.ThemeName = ThemeName); }
        }
        public string WorkingDirectory {
            get { return _WorkingDirectory; }
            set { SetProperty(ref _WorkingDirectory, value, OnWorkingDirectoryChanged); }
        }

        public IEnumerable<UICommand> Commands { get; }
        public InteractionRequest<IConfirmation> ConfirmationRequest { get; } = new InteractionRequest<IConfirmation>();

        public bool Confirmed { get; set; }
        public string Title { get; set; }
        public object Content { get; set; }

        public SettingsViewModel(IConfigSerializer configSerializer)  {
            Title = "Settings";
            this.configSerializer = configSerializer;
            Config = configSerializer.GetConfig();
            Commands = UICommand.GenerateFromMessageButton(MessageButton.OKCancel, new DialogService(), MessageResult.OK, MessageResult.Cancel);
            Commands.Where(c => c.IsDefault).Single().Command = new DelegateCommand(Save, () => !IsAnyRepositoryDownloading());
            Commands.Where(c => c.IsCancel).Single().Command = new DelegateCommand(Cancel, () => !IsAnyRepositoryDownloading());
        }

        void OnWorkingDirectoryChanged() {
            Config.WorkingDirectory = WorkingDirectory;
            foreach(var repo in Repositories.Where(r => r.DownloadState == DownloadState.ReadyToDownload)) {
                repo.Path = System.IO.Path.Combine(WorkingDirectory, $"20{repo.Version}_VisualTests");
                repo.UpdateDownloadState();
            }
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

        void Repositories_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e) {
            Config.Repositories = Repositories.Select(r => r.Repository).ToArray();
        }

        bool IsChanged() {
            return !configSerializer.IsConfigEquals(configSerializer.GetConfig(false), Config);
        }
        bool IsAnyRepositoryDownloading() {
            foreach(var repo in Repositories) {
                if(repo.DownloadState == DownloadState.Downloading)
                    return true;
            }
            return false;
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
