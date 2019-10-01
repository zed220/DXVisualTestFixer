using Prism.Interactivity.InteractionRequest;
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
        bool IsInUpdate { get; }
        bool IsNetworkDeployment { get; }
        void Update();
    }
    public interface IAppearanceService {
        void SetTheme(string themeName, string palette);
    }
    public interface IActiveService : INotifyPropertyChanged {
        bool IsActive { get; set; }
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
    public interface ITestInfoModel : INotifyPropertyChanged {
        bool CommitChange { get; set; }
        string TeamName { get; }
        int Problem { get; }
        string ProblemName { get; }
        string Theme { get; }
        bool Optimized { get; }
        string AdditionalParameters { get; }
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
    public interface INotificationService {
        void DoNotification(string title, string content, MessageBoxImage image = MessageBoxImage.Information);
        event EventHandler<INotificationServiceArgs> Notification;
    }
    public interface IConfig {
        string ThemeName { get; set; }
        Repository[] Repositories { get; set; }
        string WorkingDirectory { get; set; }

        IEnumerable<Repository> GetLocalRepositories();
    }
    public interface ISettingsViewModel : IConfirmation {
        IConfig Config { get; }
    }
    public interface IFarmRefreshedEventArgs {
        FarmRefreshType RefreshType { get; }
        void Parse();
    }
    public interface IFarmStatus {
        FarmIntegrationStatus BuildStatus { get; }
    }
    public interface IFarmIntegrator {
        Task<List<IFarmTaskInfo>> GetAllTasks(Repository[] repositories);
    }
    public interface ITestsService : INotifyPropertyChanged {
        ITestInfoContainer ActualState { get; }
        string CurrentFilter { get; set; }

        bool ApplyTest(TestInfo test, Func<string, bool> checkoutFunc);
        Task UpdateTests(INotificationService notificationService);
        string GetResourcePath(Repository repository, string relativePath);
    }
    public interface IFarmTaskInfo {
        Repository Repository { get; }
        string Url { get; }
        bool Success { get; }
    }
    public interface ITestInfoContainer {
        List<TestInfo> TestList { get; }
        Dictionary<Repository, List<string>> UsedFilesLinks { get; }
        Dictionary<Repository, List<IElapsedTimeInfo>> ElapsedTimes { get; }
        Dictionary<Repository, List<Team>> Teams { get; }
        List<TestInfo> ChangedTests { get; }
        List<TimingInfo> Timings { get; }
    }
    public interface IConfigSerializer {
        IConfig GetConfig(bool useCache = true);
        bool IsConfigEquals(IConfig left, IConfig right);
        void SaveConfig(IConfig options);
    }
    public interface INotificationServiceArgs {
        string Title { get; }
        string Content { get; }
        MessageBoxImage Image { get; }
    }
    public interface IGitWorker {
        bool SetHttpRepository(Repository repository);
        Task<GitUpdateResult> Update(Repository repository);
        Task<GitCommitResult> Commit(Repository repository, string commitCaption);
        Task<bool> Clone(Repository repository);
    }
    public interface IMinioWorker {
        Task<string> Download(string path);
    }
}
