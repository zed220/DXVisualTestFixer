using System;
using System.IO;
using System.Linq;
using System.Security.Principal;
using System.Threading.Tasks;
using DXVisualTestFixer.Common;
using LibGit2Sharp;
using LibGit2Sharp.Handlers;
using CommonRepository = DXVisualTestFixer.Common.Repository;
using Repository = LibGit2Sharp.Repository;

namespace DXVisualTestFixer.Git {
	public class GitWorker : IGitWorker {
		public bool SetHttpRepository(string serverPath, CommonRepository repository) {
			if(!repository.IsDownloaded())
				return false;
			using var repo = new Repository(repository.Path);
			foreach(var remote in repo.Network.Remotes.Where(remote => !remote.PushUrl.StartsWith("http"))) repo.Network.Remotes.Update(remote.Name, r => r.PushUrl = r.Url = serverPath);
			return true;
		}

		public async Task<GitUpdateResult> Update(CommonRepository repository) {
			if(!repository.IsDownloaded())
				return await Task.FromResult(GitUpdateResult.Error);
			Fetch(repository);
			if(!PullCore(repository))
				return await Task.FromResult(GitUpdateResult.Error);
			return await Task.FromResult(GitUpdateResult.None);
		}
		public async Task<bool> IsOutdatedAsync(string serverPath, CommonRepository repository) {
			if(!repository.IsDownloaded())
				return false;
			await Task.Run(() => Fetch(repository));
			using var repo = new Repository(repository.Path);
			var currentSha = repo.Head.Tip.Sha;
			var remoteBranchName = repo.Head.CanonicalName.Replace("refs/heads/", "");
			foreach(var remoteBranch in Repository.ListRemoteReferences(serverPath).OfType<DirectReference>()) {
				if(remoteBranch.CanonicalName.Replace("refs/heads/", "") == remoteBranchName) {
					var sha = remoteBranch.TargetIdentifier;
					if(sha != currentSha)
						return true;
				}
			}
			return false;
		}
		
		void Fetch(CommonRepository repository) {
			var logMessage = "";
			using var repo = new Repository(repository.Path);
			var options = new FetchOptions();
			options.CredentialsProvider = CredentialsHandler;
			foreach(var remote in repo.Network.Remotes) {
				var refSpecs = remote.FetchRefSpecs.Select(x => x.Specification);
				Commands.Fetch(repo, remote.Name, refSpecs, options, logMessage);
			}
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

		public async Task<bool> Clone(string serverPath, CommonRepository repository) {
			if(repository.IsDownloaded())
				return await Task.FromResult(true);
			if(!Directory.Exists(repository.Path))
				Directory.CreateDirectory(repository.Path);
			if(!CloneCore(serverPath, repository))
				return await Task.FromResult(false);
			return await Task.FromResult(repository.IsDownloaded());
		}

		bool PullCore(CommonRepository repository) {
			using var repo = new Repository(repository.Path);
			var options = new PullOptions();
			options.FetchOptions = new FetchOptions();
			options.FetchOptions.CredentialsProvider = CredentialsHandler;
			return Commands.Pull(repo, CreateDefaultSignature(), options).Status != MergeStatus.Conflicts;
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

		bool PushCore(CommonRepository repository) {
			using var repo = new Repository(repository.Path);
			var options = new PushOptions();
			options.CredentialsProvider = CredentialsHandler;
			var branch = repo.Branches.FirstOrDefault(b => b.FriendlyName == $"20{repository.Version}");
			if(branch == null)
				return false;
			repo.Network.Push(branch, options);
			return true;
		}

		bool CloneCore(string serverPath, CommonRepository repository) {
			var co = new CloneOptions();
			co.CredentialsProvider = CredentialsHandler;
			co.BranchName = $"20{repository.Version}";
			var result = Repository.Clone(serverPath, repository.Path, co);
			return Directory.Exists(result);
		}

		static Signature CreateDefaultSignature() {
			return new Signature(new Identity("DXVisualTestsBot", "None@None.com"), DateTimeOffset.Now);
		}
		static CredentialsHandler CredentialsHandler => (url, usernameFromUrl, types) =>
			new UsernamePasswordCredentials {
				Username = "DXVisualTestsBot",
				Password = "DXVisualTestsBot1234"
			};
	}
}