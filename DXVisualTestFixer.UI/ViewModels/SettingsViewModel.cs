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
            set { SetProperty(ref _WorkingDirectory, value, () => Config.WorkingDirectory = WorkingDirectory); }
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
            Commands.Where(c => c.IsDefault).Single().Command = new DelegateCommand(Save);
            Commands.Where(c => c.IsCancel).Single().Command = new DelegateCommand(Cancel);
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
        void Save() {
            if(!IsChanged())
                return;
            Confirmed = true;
        }
        void Cancel() {
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

        //public void LoadFromWorkingFolder() {
        //    var dialog = ServiceLocator.Current.TryResolve<IFolderBrowserDialog>();
        //    var result = dialog.ShowDialog();
        //    if(result != DialogResult.OK)
        //        return;
        //    if(!Directory.Exists(dialog.SelectedPath))
        //        return;
        //    RepositoryModel.ActualizeRepositories(Repositories, dialog.SelectedPath);
        //}
        //public void Clone181() {
        //    var git = ServiceLocator.Current.GetInstance<IGitWorker>();
        //    Repository repo = new Repository();
        //    repo.Path = @"C:\Work\2018.1_VisualTests";
        //    repo.Version = "18.1";
        //    git.Clone(repo);
        //}
    }
}
