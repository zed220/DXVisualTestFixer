using DevExpress.Utils;
using DXVisualTestFixer.Common;
using DXVisualTestFixer.UI.Models;
using DXVisualTestFixer.UI.Views;
using Prism.Commands;
using Prism.Interactivity.InteractionRequest;
using Prism.Mvvm;
using Prism.Regions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace DXVisualTestFixer.UI.ViewModels {
    public class ShellViewModel : BindableBase {
        readonly IUpdateService updateService;
        readonly IDXNotification notification;
        readonly INotificationService notificationService;
        readonly IRegionManager regionManager;
        readonly ILoggingService loggingService;
        readonly IAppearanceService appearanceService;
        readonly IConfigSerializer configSerializer;
        readonly ViewItem settingsView;
        readonly Dispatcher dispatcher;

        IConfig Config;

        bool _HasUpdate;
        ViewItem _CurrentView;
        string _CurrentLogLine;
        ProgramStatus _Status;

        public InteractionRequest<INotification> NotificationRequest { get; } = new InteractionRequest<INotification>();

        public ShellViewModel(IUpdateService updateService, 
                              IDXNotification notification, 
                              INotificationService notificationService, 
                              IRegionManager regionManager, 
                              ILoadingProgressController loadingProgressController,
                              ILoggingService loggingService,
                              IConfigSerializer configSerializer, 
                              IAppearanceService appearanceService) {
            this.updateService = updateService;
            this.notification = notification;
            this.notificationService = notificationService;
            this.regionManager = regionManager;
            LoadingProgressController = loadingProgressController;
            this.loggingService = loggingService;
            this.configSerializer = configSerializer;
            this.appearanceService = appearanceService;
            this.dispatcher = Dispatcher.CurrentDispatcher;
            Commands = new List<ICommand>() { new DelegateCommand(Update).ObservesCanExecute(() => HasUpdate) };
            settingsView = new ViewItem("Settings", () => new SettingsView());
            Views = new List<ViewItem>() {
                new ViewItem("Failed Tests", () => new MainView()),
                new ViewItem("Unused Files", () => new RepositoryOptimizerView()),
                new ViewItem("Analyse Timings", () => new RepositoryAnalyzerView()),
                new ViewItem("Resource Viewer", () => new ViewResourcesView()),
                settingsView,
            };
            InitializeUpdateService();
            InitializeLoggingService();
            InitializeNotificationService();
            InitializeConfig();
            UpdateContent();
        }

        void InitializeNotificationService() {
            notificationService.Notification += NotificationService_Notification;
        }
        void InitializeLoggingService() {
            loggingService.MessageReserved += OnLoggingMessageReserved;
        }
        void OnLoggingMessageReserved(object sender, IMessageEventArgs args) {
            CurrentLogLine = args.Message;
        }
        void InitializeUpdateService() {
            if(updateService.HasUpdate) {
                HasUpdate = true;
                if(updateService.IsNetworkDeployment)
                    Update();
                return;
            }
            updateService.PropertyChanged += UpdateService_PropertyChanged;
            updateService.Start();
        }
        void NotificationService_Notification(object sender, INotificationServiceArgs e) {
            ViewModelBase.SetupNotification(notification, e.Title, e.Content, e.Image);
            dispatcher.Invoke(() => {
                NotificationRequest.Raise(notification);
            });
        }
        void UpdateService_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e) {
            if(e.PropertyName == nameof(IUpdateService.HasUpdate))
                HasUpdate = updateService.HasUpdate;
        }

        void Update() {
            updateService.Update();
        }

        void InitializeConfig() {
            loggingService.SendMessage("Checking config");
            var config = configSerializer.GetConfig(false);
            if(Config != null && configSerializer.IsConfigEquals(config, Config))
                return;
            Config = config;
            configSerializer.ConfigChanged += ConfigSerializer_ConfigChanged;
            appearanceService?.SetTheme(/*Config.ThemeName*/"VS2017Light");
            loggingService.SendMessage("Config loaded");
        }

        void ConfigSerializer_ConfigChanged(object sender, EventArgs e) {
            UpdateContent();
        }

        void UpdateContent() {
            if(!IsConfigValid()) {
                CurrentView = settingsView;
                return;
            }
            CurrentView = Views[0];
        }
        bool IsConfigValid() {
            if(Config.Repositories == null || Config.Repositories.Length == 0) {
                notificationService.DoNotification("Add repositories in settings", "Add repositories in settings");
                return false;
            }
            foreach(var repoModel in Config.Repositories.Select(rep => new RepositoryModel(rep))) {
                if(!repoModel.IsValid()) {
                    notificationService.DoNotification("Invalid Settings", "Modify repositories in settings", MessageBoxImage.Warning);
                    return false;
                }
            }
            return true;
        }

        public IEnumerable<ICommand> Commands { get; }
        public ILoadingProgressController LoadingProgressController { get; }
        public List<ViewItem> Views { get; }

        public bool HasUpdate {
            get { return _HasUpdate; }
            set { SetProperty(ref _HasUpdate, value); }
        }
        public ViewItem CurrentView {
            get { return _CurrentView; }
            set { SetProperty(ref _CurrentView, value, OnCurrentViewChanged); }
        }
        public string CurrentLogLine {
            get { return _CurrentLogLine; }
            set { SetProperty(ref _CurrentLogLine, value); }
        }
        public ProgramStatus Status {
            get { return _Status; }
            set { SetProperty(ref _Status, value); }
        }

        void OnCurrentViewChanged() {
            dispatcher.BeginInvoke(new Action(() => {
                regionManager.Regions[Regions.Main].RemoveAll();
                if(CurrentView != null)
                    regionManager.AddToRegion(Regions.Main, CurrentView.GetView());
            }));
        }
    }

    public class ViewItem : ImmutableObject {
        public ViewItem(string displayText, Func<object> getView) {
            DisplayText = displayText;
            GetView = getView;
        }

        public Func<object> GetView { get; }
        public string DisplayText { get; }
        public bool IsEnabled { get; set; }
    }
}
