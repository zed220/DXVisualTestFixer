using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using Prism.Interactivity.InteractionRequest;

namespace DXVisualTestFixer.Common {
	public interface IDXNotification : INotification {
		MessageBoxImage ImageType { get; set; }
	}

	public interface IDXConfirmation : IDXNotification, IConfirmation { }

	public interface IUpdateService : INotifyPropertyChanged {
		bool HasUpdate { get; }
		bool IsInUpdate { get; }
		bool IsNetworkDeployment { get; }
		void Start();
		void Stop();
		void Update();
	}

	public interface IActiveService : INotifyPropertyChanged {
		bool IsActive { get; set; }
	}

	public interface ILoadingProgressController : INotifyPropertyChanged {
		bool IsEnabled { get; }
		int Maximum { get; }
		int Value { get; }
		void Enlarge(int delta);
		void Flush();
		void IncreaseProgress(int delta);
		void Start();
		void Stop();
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
		(Version version, string content)[] WhatsNewInfo { get; }
	}

	public interface ITestInfoModel : INotifyPropertyChanged {
		bool CommitChange { get; set; }
		bool CommitAsBlinking { get; set; }
		string TeamName { get; }
		string VersionAndFork { get; }
		int Problem { get; }
		string ProblemName { get; }
		string Name { get; }
		string Theme { get; }
		bool Optimized { get; }
		bool Colorized { get; }
		int Dpi { get; }
		string Version { get; }
		string Volunteer { get; }
		string VolunteerShort { get; }
		TestInfo TestInfo { get; }
		TestState Valid { get; }
		string ToLog();
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
		string Volunteer { get; set; }
		string Email { get; set; }
		Repository[] Repositories { get; set; }
		string WorkingDirectory { get; set; }
		string WhatsNewSeenForVersion { get; set; }
		string DefaultPlatform { get; set; }

		IEnumerable<Repository> GetLocalRepositories();
	}

	public interface ISettingsViewModel : IConfirmation {
		IConfig Config { get; }
	}

	public interface ITestsService : INotifyPropertyChanged {
		ITestInfoContainer SelectedState { get; }
		string CurrentFilter { get; set; }

		bool ApplyTest(TestInfo test, Func<string, bool> checkoutFunc);
		Task SelectState(string platform, string stateName);
		Dictionary<string, List<Repository>> States { get; }
	}

	public interface ITestInfoContainer {
		List<TestInfo> TestList { get; }
		Dictionary<Repository, List<string>> UsedFilesLinks { get; }
		Dictionary<Repository, List<IElapsedTimeInfo>> ElapsedTimes { get; }
		List<TestInfo> ChangedTests { get; }
		List<TimingInfo> Timings { get; }
		bool AllowEditing { get; }
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
		bool SetHttpRepository(string serverPath, Repository repository);
		Task<GitUpdateResult> Update(Repository repository);
		Task<bool> IsOutdatedAsync(string serverPath, Repository repository);
		Task<GitCommitResult> Commit(Repository repository, string commitCaption, string author, string email);
		Task<bool> Clone(string serverPath, Repository repository);
	}

	public interface IMinioWorker {
		Task<string> Download(string path);
		Task<Stream> GetBinary(string path);
		Task<string[]> Discover(string path);
		Task<string> DiscoverLast(string path);
		Task<string> DiscoverPrev(string path, int prevCount);
		Task WaitIfObjectNotLoaded(string root, string child);
		Task<bool> ExistsDir(string root, string child);
		Task<bool> ExistsFile(string path);
		Task<string[]> DetectUserPaths(string platform, string forkFolderName);
	}

	public interface IPlatformInfo {
		string Name { get; }
		string GitRepository { get; }
		string MinioRepository { get; }
		string LocalPath { get; }
		string VersionsFileName { get; }
		string FarmTaskName { get; }
		string ForkFolderName { get; }
	}

	public interface IPlatformProvider {
		public IPlatformInfo[] PlatformInfos { get; }
	}
	public interface ICCNetProblemsLoader {
		Task<List<ICCNetProblem>> GetProblemsAsync(string projectName);
		Task<bool> TakeVolunteers(string projectName, string[] testFullNames, string volunteer);
	}
	public interface ICCNetProblem {
		string TestName { get; }
		string Volunteer { get; }
	}
	public interface ICache<T> {
		public T GetOrAdd(byte[] sha256, Func<T> getValue);
	}
}