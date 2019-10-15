using DXVisualTestFixer.Common;
using LibGit2Sharp;
using LibGit2Sharp.Handlers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommonRepository = DXVisualTestFixer.Common.Repository;
using Repository = LibGit2Sharp.Repository;

namespace DXVisualTestFixer.Core {
    public class GitWorker : IGitWorker {
        public bool SetHttpRepository(CommonRepository repository) {
            if(!repository.IsDownloaded())
                return false;
            using(var repo = new Repository(repository.Path)) {
                foreach(Remote remote in repo.Network.Remotes.ToList()) {
                    if(!remote.PushUrl.StartsWith("http"))
                        repo.Network.Remotes.Update(remote.Name, r => r.PushUrl = r.Url = "http://gitserver/XPF/VisualTests.git");
                }
            }
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
        void FetchCore(CommonRepository repository) {
            string logMessage = "";
            using(var repo = new Repository(repository.Path)) {
                FetchOptions options = new FetchOptions();
                options.CredentialsProvider = new CredentialsHandler((url, usernameFromUrl, types) =>
                    new UsernamePasswordCredentials() {
                        Username = "DXVisualTestsBot",
                        Password = "DXVisualTestsBot1234"
                    });
                foreach(Remote remote in repo.Network.Remotes) {
                    IEnumerable<string> refSpecs = remote.FetchRefSpecs.Select(x => x.Specification);
                    Commands.Fetch(repo, remote.Name, refSpecs, options, logMessage);
                }
            }
        }
        bool PullCore(CommonRepository repository) {
            using(var repo = new Repository(repository.Path)) {
                LibGit2Sharp.PullOptions options = new LibGit2Sharp.PullOptions();
                options.FetchOptions = new FetchOptions();
                options.FetchOptions.CredentialsProvider = new CredentialsHandler(
                    (url, usernameFromUrl, types) =>
                        new UsernamePasswordCredentials() {
                            Username = "DXVisualTestsBot",
                            Password = "DXVisualTestsBot1234"
                        });

                var signature = new LibGit2Sharp.Signature(new Identity("DXVisualTestsBot", "None@None.com"), DateTimeOffset.Now);
                return Commands.Pull(repo, signature, options).Status != MergeStatus.Conflicts;
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
            if(!(PushCore(repository)))
                return await Task.FromResult(GitCommitResult.Error);
            return await Task.FromResult(GitCommitResult.None);
        }
        void StageCore(CommonRepository repository) {
            using(var repo = new Repository(repository.Path)) {
                Commands.Stage(repo, "*.png");
                Commands.Stage(repo, "*.xml");
                Commands.Stage(repo, "*.sha");
            }
        }
        void CommitCore(CommonRepository repository, string commitCaption) {
            using(var repo = new Repository(repository.Path)) {
                Signature author = TryGetAuthor(repo);
                Signature committer = CreateDefaultSignature();
                string userNamePath = string.Empty;
                if(author == null) {
                    author = CreateDefaultSignature();
                    userNamePath = string.Format(" ({0})", System.Security.Principal.WindowsIdentity.GetCurrent().Name.Split('\\').Last());
                }
                Commit commit = repo.Commit($"{commitCaption} {userNamePath}", author, committer);
            }
        }
        static Signature TryGetAuthor(Repository repo) {
            var config = repo.Config;
            if(!config.HasConfig(ConfigurationLevel.Global))
                return null;
            string userName = config.GetValueOrDefault("user.name", ConfigurationLevel.Global, (string)null);
            if(string.IsNullOrEmpty(userName))
                return null;
            string email = config.GetValueOrDefault("user.email", ConfigurationLevel.Global, (string)null);
            if(string.IsNullOrEmpty(email))
                return null;
            return new Signature(userName, email, DateTime.Now);
        }
        static Signature CreateDefaultSignature() {
            return new Signature("DXVisualTestsBot", "None@None.com", DateTime.Now);
        }
        bool PushCore(CommonRepository repository) {
            using(var repo = new Repository(repository.Path)) {
                LibGit2Sharp.PushOptions options = new LibGit2Sharp.PushOptions();
                options.CredentialsProvider = new CredentialsHandler(
                    (url, usernameFromUrl, types) =>
                        new UsernamePasswordCredentials() {
                            Username = "DXVisualTestsBot",
                            Password = "DXVisualTestsBot1234"
                        });
                var branch = repo.Branches.FirstOrDefault(b => b.FriendlyName == $"20{repository.Version}");
                if(branch == null)
                    return false;
                repo.Network.Push(branch, options);
                return true;
            }
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
        bool CloneCore(CommonRepository repository) {
            var co = new CloneOptions();
            co.CredentialsProvider = (_url, _user, _cred) => new UsernamePasswordCredentials { Username = "DXVisualTestsBot", Password = "DXVisualTestsBot1234" };
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
