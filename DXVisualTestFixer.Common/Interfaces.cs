﻿using Prism.Interactivity.InteractionRequest;
using Prism.Regions;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace DXVisualTestFixer.Common {
    public interface IThemesProvider {
        List<string> AllThemes { get; }
    }
    public interface IDXNotification : Prism.Interactivity.InteractionRequest.INotification {
        MessageBoxImage ImageType { get; set; }
    }
    public interface IDXConfirmation : IDXNotification, IConfirmation {
    }
    public interface IUpdateService : INotifyPropertyChanged {
        void Start();
        void Stop();
        bool HasUpdate { get; }
        bool IsNetworkDeployment { get; }
        void Update();
    }
    public interface IAppearanceService {
        void SetTheme(string themeName);
    }
    public interface ILoadingProgressController : INotifyPropertyChanged {
        void Enlarge(int delta);
        void Flush();
        void IncreaseProgress(int delta);
        void Start();
        void Stop();

        bool IsEnabled { get; }
        int Maximum { get; }
        int Value { get; }
    }
    public interface ILoggingService {
        void SendMessage(string text);

        event EventHandler<IMessageEventArgs> MessageReserved;
    }
    public interface IMessageEventArgs {
        string Message { get; }
    }
    public interface IVersionService {
        Version Version { get; }
    }
    public interface IApplyChangesViewModel : IConfirmation { }
    public interface IFilterPanelViewModel { }
    public interface ITestInfoModel {
        bool CommitChange { get; set; }
        string TeamName { get; }
        int Dpi { get; }
        string Version { get; }
        TestInfo TestInfo { get; }
        string ToLog();
        TestState Valid { get; }
    }
    public interface IElapsedTimeInfo {
        string Name { get; }
        TimeSpan Time { get; }
    }
    public interface IMainViewModel {
        MergerdTestViewType MergerdTestViewType { get; set; }
        TestViewType TestViewType { get; }
        ITestInfoModel CurrentTest { get; }

        void RaiseMoveNext();
        void RaiseMovePrev();
    }
    public interface IRepositoryAnalyzerViewModel : INotification { }
    public interface IRepositoryOptimizerViewModel : IConfirmation { }
    public interface IConfig {
        string ThemeName { get; set; }
        Repository[] Repositories { get; set; }
    }
    public interface ISettingsViewModel : IConfirmation {
        IConfig Config { get; }
    }
    public interface IShellViewModel {
        IEnumerable<ICommand> Commands { get; }
        void DoNotification(string title, string content, MessageBoxImage image = MessageBoxImage.Information);
    }
    public interface ITestInfoViewModel : INavigationAware { }
    public interface IFarmRefreshedEventArgs {
        FarmRefreshType RefreshType { get; }
        void Parse();
    }
    public interface IFarmStatus {
        FarmIntegrationStatus BuildStatus { get; }
    }
    public interface IFarmIntegrator {
        void Start(Action<IFarmRefreshedEventArgs> invalidateCallback);
        void Stop();
        List<IFarmTaskInfo> GetAllTasks(Repository[] repositories);
    }
    public interface ITestsService : INotifyPropertyChanged {
        ITestInfoContainer ActualState { get; }
        string CurrentFilter { get; set; }

        bool ApplyTest(TestInfo test, Func<string, bool> checkoutFunc);
        Task UpdateTests();
        string GetResourcePath(Repository repository, string relativePath);
    }
    public interface IFarmTaskInfo {
        Repository Repository { get; }
        string Url { get; }
    }
    public interface ITestInfoContainer {
        List<TestInfo> TestList { get; }
        Dictionary<Repository, List<string>> UsedFiles { get; }
        Dictionary<Repository, List<IElapsedTimeInfo>> ElapsedTimes { get; }
        Dictionary<Repository, List<Team>> Teams { get; }
        List<TestInfo> ChangedTests { get; }
    }
    public interface IConfigSerializer {
        IConfig GetConfig(bool useCache = true);
        bool IsConfigEquals(IConfig left, IConfig right);
        void SaveConfig(IConfig options);
    }
    public interface IViewResourcesViewModel : INotification { }
}
