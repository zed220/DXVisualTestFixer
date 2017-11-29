using DevExpress.Mvvm;
using DevExpress.Mvvm.ModuleInjection;
using DevExpress.Xpf.Core;
using DXVisualTestFixer.Configuration;
using DXVisualTestFixer.Core;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DXVisualTestFixer.ViewModels {
    public interface ISettingsViewModel { }

    public class SettingsViewModel : ViewModelBase, ISettingsViewModel {
        public Config Config {
            get { return GetProperty(() => Config); }
            private set { SetProperty(() => Config, value, OnConfigChanged); }
        }
        public ObservableCollection<RepositoryModel> Repositories {
            get { return GetProperty(() => Repositories); }
            set { SetProperty(() => Repositories, value); }
        }

        public IEnumerable<UICommand> DialogCommands { get; private set; }

        public SettingsViewModel() {
            ModuleManager.DefaultManager.GetEvents(this).ViewModelRemoving += SettingsViewModel_ViewModelRemoving;
            CreateCommands();
            Config = ConfigSerializer.GetConfig();
        }

        void CreateCommands() {
            List<UICommand> dialogCommands = new List<UICommand>();
            dialogCommands.Add(new UICommand() { IsDefault = true, Command = new DelegateCommand(Save), Caption = DXMessageBoxLocalizer.GetString(DXMessageBoxStringId.Ok) });
            dialogCommands.Add(new UICommand() { IsCancel = true, Caption = DXMessageBoxLocalizer.GetString(DXMessageBoxStringId.Cancel) });
            DialogCommands = dialogCommands;
        }

        void OnConfigChanged() {
            LoadRepositories();
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
            ConfigSerializer.SaveConfig(Config);
        }

        void SettingsViewModel_ViewModelRemoving(object sender, ViewModelRemovingEventArgs e) {
            ModuleManager.DefaultManager.GetEvents(this).ViewModelRemoving -= SettingsViewModel_ViewModelRemoving;
            if(!IsChanged())
                return;
            MessageResult? result = GetService<IMessageBoxService>()?.ShowMessage("Save changes?", "Save changes?", MessageButton.YesNoCancel);
            if(!result.HasValue || result.Value == MessageResult.Cancel || result.Value == MessageResult.None) {
                e.Cancel = true;
                ModuleManager.DefaultManager.GetEvents(this).ViewModelRemoving += SettingsViewModel_ViewModelRemoving;
                return;
            }
            if(result.Value == MessageResult.Yes)
                Save();
        }

        public void LoadFromWorkingFolder() {
            IFolderBrowserDialogService service = GetService<IFolderBrowserDialogService>();
            bool? result = service?.ShowDialog();
            if(!result.HasValue || !(bool)result)
                return;
            if(!Directory.Exists(service.ResultPath))
                return;
            List<string> savedVersions = Repositories.Select(r => r.Version).ToList();
            foreach(var ver in Repository.Versions.Where(v => !savedVersions.Contains(v))) {
                string verDir = String.Format("20{0}", ver);
                string verPath = Path.Combine(service.ResultPath, verDir);
                if(!Directory.Exists(verPath))
                    continue;

                string visualTestsPathVar = Path.Combine(verPath, "XPF\\");
                if(!Directory.Exists(visualTestsPathVar)) {
                    visualTestsPathVar = Path.Combine(verPath, "common\\XPF\\");
                    if(!Directory.Exists(visualTestsPathVar))
                        continue;
                }
                Repositories.Add(new RepositoryModel(new Repository() { Version = ver, Path = visualTestsPathVar }));
            }
        }
    }
}
