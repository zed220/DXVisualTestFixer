using DevExpress.Utils;
using DXVisualTestFixer.Common;
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

        bool _HasUpdate;
        ViewItem _CurrentView;
        string _CurrentLogLine;

        public InteractionRequest<INotification> NotificationRequest { get; } = new InteractionRequest<INotification>();

        public ShellViewModel(IUpdateService updateService, 
                              IDXNotification notification, 
                              INotificationService notificationService, 
                              IRegionManager regionManager, 
                              ILoadingProgressController loadingProgressController,
                              ILoggingService loggingService) {
            this.updateService = updateService;
            this.notification = notification;
            this.notificationService = notificationService;
            this.regionManager = regionManager;
            LoadingProgressController = loadingProgressController;
            this.loggingService = loggingService;
            notificationService.Notification += NotificationService_Notification;
            Commands = new List<ICommand>() { new DelegateCommand(Update).ObservesCanExecute(() => HasUpdate) };
            Views = new List<ViewItem>() {
                new ViewItem("Failed Tests", () => new MainView()),
                new ViewItem("Unused Files", () => new RepositoryOptimizerView()),
                new ViewItem("Analyse Timings", () => new RepositoryAnalyzerView()),
                new ViewItem("Resource Viewer", () => new ViewResourcesView())
            };
            CurrentView = Views[0];
            InitializeUpdateService();
            InitializeLoggingService();
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
            NotificationRequest.Raise(notification);
        }

        void UpdateService_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e) {
            if(e.PropertyName == nameof(IUpdateService.HasUpdate))
                HasUpdate = updateService.HasUpdate;
        }

        void Update() {
            updateService.Update();
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

        void OnCurrentViewChanged() {
            Dispatcher.CurrentDispatcher.BeginInvoke(new Action(() => {
                regionManager.Regions[Regions.Main].RemoveAll();
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
