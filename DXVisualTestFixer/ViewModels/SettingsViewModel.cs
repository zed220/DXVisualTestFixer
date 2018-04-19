using DevExpress.Mvvm;
using DevExpress.Mvvm.UI;
using DevExpress.Xpf.Core;
using DevExpress.Xpf.Dialogs;
using DXVisualTestFixer.Configuration;
using DXVisualTestFixer.Core;
using DXVisualTestFixer.PrismCommon;
using DXVisualTestFixer.ViewModels;
using Microsoft.Practices.Unity;
using Prism.Interactivity.InteractionRequest;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Media;

namespace DXVisualTestFixer.ViewModels {
    public interface ISettingsViewModel : IConfirmation {
        Config Config { get; }
    }

    public class SettingsViewModel : BindableBase, ISettingsViewModel {
        readonly IUnityContainer unityContainer;

        public Config Config {
            get { return GetProperty(() => Config); }
            private set { SetProperty(() => Config, value, OnConfigChanged); }
        }
        public ObservableCollection<RepositoryModel> Repositories {
            get { return GetProperty(() => Repositories); }
            set { SetProperty(() => Repositories, value); }
        }
        public string ThemeName {
            get { return GetProperty(() => ThemeName); }
            set { SetProperty(() => ThemeName, value, () => Config.ThemeName = ThemeName); }
        }

        public IEnumerable<UICommand> Commands { get; }
        public InteractionRequest<IConfirmation> ConfirmationRequest { get; } = new InteractionRequest<IConfirmation>();

        public bool Confirmed { get; set; }
        public string Title { get; set; }
        public object Content { get; set; }

        public SettingsViewModel(IUnityContainer container) {
            unityContainer = container;
            Title = "Settings";
            Config = ConfigSerializer.GetConfig();
            Commands = UICommand.GenerateFromMessageButton(MessageButton.OKCancel, new DialogService(), MessageResult.OK, MessageResult.Cancel);
            Commands.Where(c => c.IsDefault).Single().Command = new DelegateCommand(Save);
            Commands.Where(c => c.IsCancel).Single().Command = new DelegateCommand(Cancel);
        }

        void OnConfigChanged() {
            LoadRepositories();
            ThemeName = Config.ThemeName;
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
            return !ConfigSerializer.IsConfigEquals(ConfigSerializer.GetConfig(), Config);
        }
        void Save() {
            if(!IsChanged())
                return;
            Confirmed = true;
        }
        void Cancel() {
            if(!IsChanged())
                return;
            DXConfirmation confirmation = new DXConfirmation(MessageBoxImage.Warning) { Title = "Save changes?", Content = "Save changes?" };
            ConfirmationRequest.Raise(confirmation);
            if(confirmation.Confirmed)
                Save();
        }

        public void LoadFromWorkingFolder() {
            var dialog = unityContainer.Resolve<IFolderBrowserDialog>();
            var result = dialog.ShowDialog();
            if(result != DialogResult.OK)
                return;
            if(!Directory.Exists(dialog.SelectedPath))
                return;
            RepositoryModel.ActualizeRepositories(Repositories, dialog.SelectedPath);
        }
    }
}
