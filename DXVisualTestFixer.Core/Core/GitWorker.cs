using System;
using System.IO;
using System.Linq;
using System.Security.Principal;
using System.Threading.Tasks;
using DXVisualTestFixer.Common;
using LibGit2Sharp;
using CommonRepository = DXVisualTestFixer.Common.Repository;
using Repository = LibGit2Sharp.Repository;

namespace DXVisualTestFixer.Core {
	public class GitWorker : IGitWorker {
		public bool SetHttpRepository(CommonRepository repository) {
			if(!repository.IsDownloaded())
				return false;
			using var repo = new Repository(repository.Path);
			foreach(var remote in repo.Network.Remotes.Where(remote => !remote.PushUrl.StartsWith("http"))) repo.Network.Remotes.Update(remote.Name, r => r.PushUrl = r.Url = "http://gitserver/XPF/VisualTests.git");
			return true;
		}

		public async Task<GitUpdateResult> Update(CommonRepository repository) {
			if(!repository.IsDownloaded())
				return await Task.FromResult(GitUpdateResult.Error);
			FetchCore(repository);
			if(!PullCore(repository))
				return await Task.FromResult(GitUpdateResult.Error);
			return await Task.FromResult(GitUpdateResult.None);
		}

		public async Task<GitCommitResult> Commit(CommonRepository repository, string commitCaption) {
			if(!repository.IsDownloaded())
				return await Task.FromResult(GitCommitResult.Error);
			StageCore(repository);
			try {
				CommitCore(repository, commitCaption);
			}
			catch {
				return await Task.FromResult(GitCommitResult.Error);
			}

			if(!PushCore(repository))
				return await Task.FromResult(GitCommitResult.Error);
			return await Task.FromResult(GitCommitResult.None);
		}

		public async Task<bool> Clone(CommonRepository repository) {
			if(repository.IsDownloaded())
				return await Task.FromResult(true);
			if(!Directory.Exists(repository.Path))
				Directory.CreateDirectory(repository.Path);
			if(!CloneCore(repository))
				return await Task.FromResult(false);
			return await Task.FromResult(repository.IsDownloaded());
		}

		void FetchCore(CommonRepository repository) {
			var logMessage = "";
			using var repo = new Repository(repository.Path);
			var options = new FetchOptions();
			options.CredentialsProvider = (url, usernameFromUrl, types) =>
				new UsernamePasswordCredentials {
					Username = "DXVisualTestsBot",
					Password = "DXVisualTestsBot1234"
				};
			foreach(var remote in repo.Network.Remotes) {
				var refSpecs = remote.FetchRefSpecs.Select(x => x.Specification);
				Commands.Fetch(repo, remote.Name, refSpecs, options, logMessage);
			}
		}

		bool PullCore(CommonRepository repository) {
			using var repo = new Repository(repository.Path);
			var options = new PullOptions();
			options.FetchOptions = new FetchOptions();
			options.FetchOptions.CredentialsProvider = (url, usernameFromUrl, types) =>
				new UsernamePasswordCredentials {
					Username = "DXVisualTestsBot",
					Password = "DXVisualTestsBot1234"
				};

			var signature = new Signature(new Identity("DXVisualTestsBot", "None@None.com"), DateTimeOffset.Now);
			return Commands.Pull(repo, signature, options).Status != MergeStatus.Conflicts;
		}

		void StageCore(CommonRepository repository) {
			using var repo = new Repository(repository.Path);
			Commands.Stage(repo, "*.png");
			Commands.Stage(repo, "*.xml");
			Commands.Stage(repo, "*.sha");
		}

		void CommitCore(CommonRepository repository, string commitCaption) {
			using var repo = new Repository(repository.Path);
			var author = TryGetAuthor(repo);
			var committer = CreateDefaultSignature();
			var userNamePath = string.Empty;
			if(author == null) {
				author = CreateDefaultSignature();
				userNamePath = $" ({WindowsIdentity.GetCurrent().Name.Split('\\').Last()})";
			}

			var commit = repo.Commit($"{commitCaption} {userNamePath}", author, committer);
		}

		static Signature TryGetAuthor(Repository repo) {
			var config = repo.Config;
			if(!config.HasConfig(ConfigurationLevel.Global))
				return null;
			var userName = config.GetValueOrDefault("user.name", ConfigurationLevel.Global, (string) null);
			if(string.IsNullOrEmpty(userName))
				return null;
			var email = config.GetValueOrDefault("user.email", ConfigurationLevel.Global, (string) null);
			return string.IsNullOrEmpty(email) ? null : new Signature(userName, email, DateTime.Now);
		}

		static Signature CreateDefaultSignature() {
			return new Signature("DXVisualTestsBot", "None@None.com", DateTime.Now);
		}

		bool PushCore(CommonRepository repository) {
			using var repo = new Repository(repository.Path);
			var options = new PushOptions();
			options.CredentialsProvider = (url, usernameFromUrl, types) =>
				new UsernamePasswordCredentials {
					Username = "DXVisualTestsBot",
					Password = "DXVisualTestsBot1234"
				};
			var branch = repo.Branches.FirstOrDefault(b => b.FriendlyName == $"20{repository.Version}");
			if(branch == null)
				return false;
			repo.Network.Push(branch, options);
			return true;
		}

		bool CloneCore(CommonRepository repository) {
			var co = new CloneOptions();
			co.CredentialsProvider = (_url, _user, _cred) => new UsernamePasswordCredentials {Username = "DXVisualTestsBot", Password = "DXVisualTestsBot1234"};
			co.BranchName = $"20{repository.Version}";
			var result = Repository.Clone("http://gitserver/XPF/VisualTests.git", repository.Path, co);
			return Directory.Exists(result);
		}

		//bool CheckoutBranchCore(CommonRepository repository) {
		//    using(var repo = new Repository(repository.Path)) {
		//        var branch = repo.Branches[$"20{repository.Version}"];
		//        if(branch == null)
		//            return false;
		//        Branch currentBranch = Commands.Checkout(repo, branch);
		//        return true;
		//    }
		//}
	}
}