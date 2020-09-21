using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DXVisualTestFixer.Common;
using DXVisualTestFixer.Git;

namespace DXVisualTestFixer.Git {
    public class GitWorkerCmd : IGitWorker {
        const string legacyRemoteName = "origin_http";
        readonly string gitPath64 = Path.Combine(Environment.ExpandEnvironmentVariables("%ProgramW6432%"), @"Git\cmd\git.exe");
        readonly string gitPath86 = Path.Combine(Environment.ExpandEnvironmentVariables("%ProgramFiles%"), @"Git\cmd\git.exe");

        string RunGitProcess(string workingDir, params string[] opt) {
            var code = ProcessHelper.WaitForProcess(GetActualGitPath(), workingDir, out var output, out var errors, opt);
            ProcessHelper.CheckFail(code, output, errors);
            return output;
        }
        string GetActualGitPath() => File.Exists(gitPath64) ? gitPath64 : gitPath86;
        static bool IsGitDir(string path) {
            if(!Directory.Exists(path))
                return false;
            return Directory.EnumerateDirectories(path).Any(x => Path.GetFileName(x) == ".git");
        }

        static string Escape(string str) => $"\"{str}\"";
        static string EscapeDoubleQuotes(string comment) => comment?.Replace("\"", "\\\"");
        static string GetBranch(Repository repository) => $"20{repository.Version}";

        void Stage(string repoPath, string fileMask) {
            try {
                var opt = new[] { "add", fileMask };
                RunGitProcess(repoPath, opt);
            }
            catch { }
        }

        void Commit(string repoPath, string comment, string author, string email) { //, string date
            try {
                var args = new[] {
                    "commit",
                    "-m", Escape(EscapeDoubleQuotes(comment)),
                    "--author", Escape($"{author} <{(string.IsNullOrEmpty(email) ? "" : email)}>"),
                };
                //Environment.SetEnvironmentVariable("GIT_AUTHOR_DATE", date);
                //Environment.SetEnvironmentVariable("GIT_COMMITTER_DATE", date);
                RunGitProcess(repoPath, args);
            }
            catch { }
        }

        void Pull(string repoPath) => RunGitProcess(repoPath, "pull");
        void LFSPull(string repoPath) => RunGitProcess(repoPath, "lfs pull");
        void Push(string repoPath) => RunGitProcess(repoPath, "push");
        void FetchRemoteBranch(string repoPath, string branch) => RunGitProcess(repoPath, "fetch", "origin", $@"{branch}:refs/remotes/origin/{branch}");
        void Checkout(string repoPath, string branch) => RunGitProcess(repoPath, "checkout -B ", branch);
        string DiffWithRemoteBranch(string repoPath, string branch) => RunGitProcess(repoPath, "diff --name-status", branch, $@"origin/{branch}");
        string GetAllRemotes(string repoPath) => RunGitProcess(repoPath, "remote", "-v");
        void RemoveRemote(string repoPath, string remoteName) => RunGitProcess(repoPath, "remote", "rm", remoteName);
        void SetRemoteUrl(string repoPath, string url) => RunGitProcess(repoPath, "remote", "set-url", "origin", url);
        void SetTracking(string repoPath, string branch) => RunGitProcess(repoPath, "branch", "-u", $"origin/{branch}");

        void ShallowClone(string localPath, string branch, string remote) {
            var args = new[] {
                "clone", "--filter=blob:none", "--no-checkout", "--branch", branch, Escape(remote), Escape(localPath)
            };
            RunGitProcess(".", args);
        }

        public bool SetHttpRepository(string serverPath, Repository repository) {
            if(!repository.IsDownloaded())
                return false;
            if(GetAllRemotes(repository.Path).Contains(legacyRemoteName))
                RemoveRemote(repository.Path, legacyRemoteName);
            SetRemoteUrl(repository.Path, serverPath);
            return true;
            // legacyRemoteName

            // using var repo = new Repository(repository.Path);
            // using var remote = repo.Network.Remotes.FirstOrDefault(remote => remote.Name == remoteName);
            // if(remote == null)
            //     repo.Network.Remotes.Add("origin_http", serverPath);
            // return true;
        }

        public async Task<GitUpdateResult> Update(Repository repository) {
            await Task.Yield();
            if(!repository.IsDownloaded())
                return GitUpdateResult.Error;
            var branch = GetBranch(repository);
            SetTracking(repository.Path, branch);
            FetchRemoteBranch(repository.Path, branch);
            try {
                Pull(repository.Path);
                //LFSPull(repository.Path);
            }
            catch {
                return GitUpdateResult.Error;
            }

            return GitUpdateResult.Updated;
        }

        public async Task<bool> IsOutdatedAsync(string serverPath, Repository repository) {
            await Task.Yield();
            if(!repository.IsDownloaded())
                return false;
            var branch = GetBranch(repository);
            FetchRemoteBranch(repository.Path, branch);
            return !string.IsNullOrEmpty(DiffWithRemoteBranch(repository.Path, branch));
        }

        public async Task<GitCommitResult> Commit(Repository repository, string commitCaption, string author, string email) {
            await Task.Yield();
            if(!repository.IsDownloaded())
                return GitCommitResult.Error;
            try {
                Stage(repository.Path, "*.xml");
                Stage(repository.Path, "*.png");
                Stage(repository.Path, "*.sha");
                Commit(repository.Path, commitCaption, author, email);
                Push(repository.Path);
            }
            catch {
                return GitCommitResult.Error;
            }

            return GitCommitResult.Committed;
        }

        public async Task<bool> Clone(string serverPath, Repository repository) {
            await Task.Yield();
            if(IsGitDir(repository.Path))
                return await Task.FromResult(true);
            if(!Directory.Exists(repository.Path))
                Directory.CreateDirectory(repository.Path);
            var branchName = GetBranch(repository);
            ShallowClone(repository.Path, branchName, serverPath);
            Checkout(repository.Path, branchName);
            return repository.IsDownloaded();
        }
    }
}