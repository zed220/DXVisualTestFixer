using DevExpress.Mvvm;
using DevExpress.Xpf.Core;
using DXVisualTestFixer.Configuration;
using DXVisualTestFixer.Core;
using DXVisualTestFixer.PrismCommon;
using DXVisualTestFixer.ViewModels;
using Prism.Interactivity.InteractionRequest;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace DXVisualTestFixer.ViewModels {
    public interface ISettingsViewModel : IConfirmation {
        Config Config { get; }
    }

    public class SettingsViewModel : BindableBase, ISettingsViewModel {
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

        public SettingsViewModel() {
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
            //IFolderBrowserDialogService service = GetService<IFolderBrowserDialogService>();
            //bool? result = service?.ShowDialog();
            //if(!result.HasValue || !(bool)result)
            //    return;
            //if(!Directory.Exists(service.ResultPath))
            //    return;
            //List<string> savedVersions = Repositories.Select(r => r.Version).ToList();
            //foreach(var ver in Repository.Versions.Where(v => !savedVersions.Contains(v))) {
            //    //if(Repository.InNewVersion(ver))
            //    string verDir = String.Format("20{0}", ver);
            //    //string verPath = Path.Combine(service.ResultPath, verDir);
            //    foreach(var directoryPath in Directory.GetDirectories(service.ResultPath)) {
            //        string dirName = Path.GetFileName(directoryPath);
            //        if(dirName.Contains(String.Format("20{0}", ver)) || dirName.Contains(ver)) {
            //            if(Repository.InNewVersion(ver)) {
            //                if(!File.Exists(directoryPath + "\\VisualTestsConfig.xml"))
            //                    continue;
            //                Repositories.Add(new RepositoryModel(new Repository() { Version = ver, Path = directoryPath + "\\" }));
            //                continue;
            //            }
            //            string visualTestsPathVar = Path.Combine(directoryPath, "XPF\\");
            //            if(!Directory.Exists(visualTestsPathVar)) {
            //                visualTestsPathVar = Path.Combine(directoryPath, "common\\XPF\\");
            //                if(!Directory.Exists(visualTestsPathVar))
            //                    continue;
            //            }
            //            Repositories.Add(new RepositoryModel(new Repository() { Version = ver, Path = visualTestsPathVar }));
            //        }
            //    }
            //}
        }
    }
}
