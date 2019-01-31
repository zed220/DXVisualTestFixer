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
            if(!repository.IsValid())
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
            if(!repository.IsValid())
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

        public async Task<GitCommitResult> Commit(CommonRepository repository) {
            if(!repository.IsValid())
                return await Task.FromResult(GitCommitResult.Error);
            StageCore(repository);
            CommitCore(repository);
            if(!(PushCore(repository)))
                return await Task.FromResult(GitCommitResult.Error);
            return await Task.FromResult(GitCommitResult.None);
        }
        void StageCore(CommonRepository repository) {
            using(var repo = new Repository(repository.Path)) {
                Commands.Stage(repo, "*.png");
                Commands.Stage(repo, "*.xml");
            }
        }
        void CommitCore(CommonRepository repository) {
            string userName = System.Security.Principal.WindowsIdentity.GetCurrent().Name.Split('\\').Last();
            using(var repo = new Repository(repository.Path)) {
                Signature author = new Signature("DXVisualTestsBot", "None@None.com", DateTime.Now);
                Signature committer = author;
                Commit commit = repo.Commit($"Update tests ({userName})", author, committer);
            }
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
            if(!CommonRepository.IsNewVersion(repository.Version))
                return await Task.FromResult(false);
            if(repository.IsValid())
                return await Task.FromResult(true);
            CloneCore(repository);
            if(!CheckoutBranchCore(repository))
                return await Task.FromResult(false);
            return await Task.FromResult(repository.IsValid());
        }
        void CloneCore(CommonRepository repository) {
            var co = new CloneOptions();
            co.CredentialsProvider = (_url, _user, _cred) => new UsernamePasswordCredentials { Username = "DXVisualTestsBot", Password = "DXVisualTestsBot1234" };
            Repository.Clone("http://gitserver/XPF/VisualTests.git", repository.Path, co);
        }
        bool CheckoutBranchCore(CommonRepository repository) {
            using(var repo = new Repository(repository.Path)) {
                var branch = repo.Branches[$"20{repository.Version}"];
                if(branch == null)
                    return false;
                Branch currentBranch = Commands.Checkout(repo, branch);
                return true;
            }
        }
    }
}
