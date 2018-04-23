using DevExpress.Data.Filtering;
using DevExpress.Mvvm;
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
    public interface IDXNotification : Prism.Interactivity.InteractionRequest.INotification {
        MessageBoxImage ImageType { get; set; }
        IEnumerable<UICommand> Commands { get; }
    }
    public interface IDXConfirmation : IDXNotification, IConfirmation {
    }
    public interface IUpdateService : INotifyPropertyChanged {
        void Start();
        void Stop();
        bool HasUpdate { get; }
        bool IsNetworkDeployment { get; }
    }
    public interface IAppearanceService {
        void SetTheme(string themeName);
    }
    public interface ILoadingProgressController {
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
    public interface ITestInfoWrapper {
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
        List<ITestInfoWrapper> Tests { get; }
        MergerdTestViewType MergerdTestViewType { get; set; }
        TestViewType TestViewType { get; }
        ITestInfoWrapper CurrentTest { get; }
        Dictionary<Repository, List<string>> UsedFiles { get; }
        Dictionary<Repository, List<Team>> Teams { get; }
        Dictionary<Repository, List<IElapsedTimeInfo>> ElapsedTimes { get; }

        void SetFilter(CriteriaOperator op);
        void RaiseMoveNext();
        void RaiseMovePrev();
        List<ITestInfoWrapper> GetChangedTests();
    }
    public interface IRepositoryAnalyzerViewModel : Prism.Interactivity.InteractionRequest.INotification { }
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
    }
    public interface ITestInfoViewModel : INavigationAware { }
}
