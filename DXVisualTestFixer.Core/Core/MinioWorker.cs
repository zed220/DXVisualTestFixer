using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Threading.Tasks;
using System.Text;
using System.Threading.Tasks;
using DXVisualTestFixer.Common;
using LibGit2Sharp;
using Minio;

namespace DXVisualTestFixer.Core {
	public class MinioWorker : IMinioWorker {
		const string bucketName = "visualtests";
		
		static async Task<T> RepeatAsync<T>(Func<Task<T>> action, int iterationCount = 10) {
			Exception exception = null;
			for(var i = 0; i < iterationCount; i++) {
				try {
					return await action();
				}
				catch(Exception e) {
					exception = e;
				}
				await Task.Delay(TimeSpan.FromSeconds(10));
			}

			throw exception;
		}
		static async Task RepeatAsync(Func<Task> action, int iterationCount = 10) {
			Exception exception = null;
			for(var i = 0; i < iterationCount; i++) {
				try {
					await action();
					return;
				}
				catch(Exception e) {
					exception = e;
				}
				await Task.Delay(TimeSpan.FromSeconds(10));
			}

			throw exception;
		}

		
		static MinioClient CreateClient() => new MinioClient("gitlabci4-minio:9000", "xpfminio", "xpfminiostorage");

		public async Task<string> Download(string path) {
			return await RepeatAsync(async () => {
				string result = null;
				await CreateClient().GetObjectAsync(bucketName, path, stream => {
					using var reader = new StreamReader(stream);
					result = reader.ReadToEnd();
				});
				return result;
			});
		}
		
		public async Task<bool> Exists(string root, string child) {
			return await RepeatAsync(async () => {
				var result = new List<string>();
				var observable = CreateClient().ListObjectsAsync(bucketName, root);
				var tcs = new TaskCompletionSource<bool>();
				using var subscription = observable.Subscribe
				(
					item => result.Add(item.Key),
					tcs.SetException,
					() => tcs.SetResult(true)
				);
				await tcs.Task;
				var fullPath = root + child + "/";
				return result.Contains(fullPath);
			});
		}

		public async Task WaitIfObjectNotLoaded(string root, string child) {
			if(!await Exists(root, child))
				await Task.FromException(new NotFoundException(root + child));
		}
		
		public async Task<string[]> Discover(string path) {
			return await RepeatAsync(async () => {
				var result = new List<string>();
				var observable = CreateClient().ListObjectsAsync(bucketName, path);
				var tcs = new TaskCompletionSource<bool>();
				using var subscription = observable.Subscribe
				(
					item => result.Add(item.Key),
					tcs.SetException,
					() => tcs.SetResult(true)
				);
				await tcs.Task;
				return result.ToArray();
			});
		}
		
		public async Task<string> DiscoverLast(string path) => await RepeatAsync(async () => (await Discover(path)).Last());

		public async Task<string> DiscoverPrev(string path) {
			var all = await Discover(path);
			return all.Reverse().Skip(1).First();
		}
		public async Task<string[]> DetectUserPaths() {
			var excludedPaths = new[] { "XPF/", "usedfiles/", "visualtestsscripts-2.0/", "visualtestsscripts-3.0/", "visualtestsscripts-4.0/" };
			
			var rootPaths = await Discover(null);
			rootPaths = rootPaths.Except(excludedPaths).ToArray();

			var result = new List<string>();
			foreach(var rootPath in rootPaths) {
				foreach(var version in await Discover(rootPath + "Common/")) 
					result.Add(await DiscoverLast(version));
			}
			
			return result.ToArray();
		}
	}
}