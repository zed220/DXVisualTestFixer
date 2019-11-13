using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Threading.Tasks;
using System.Text;
using System.Threading.Tasks;
using DXVisualTestFixer.Common;
using Minio;

namespace DXVisualTestFixer.Core {
	public class MinioWorker : IMinioWorker {
		const string bucketName = "visualtests";


		static readonly MinioClient minio = new MinioClient("gitlabci4-minio:9000", "xpfminio", "xpfminiostorage");

		static async Task<T> RepeatableAction<T>(Func<Task<T>> action, int iterationCount = 5) {
			Exception exception = null;
			for(var i = 0; i < iterationCount; i++) {
				try {
					return await action();
				}
				catch(Exception e) {
					exception = e;
				}
			}

			throw exception;
		}

		public async Task<string> Download(string path) {
			return await RepeatableAction(async () => {
				string result = null;
				await minio.GetObjectAsync(bucketName, path, stream => {
					using var reader = new StreamReader(stream);
					result = reader.ReadToEnd();
				});
				return result;
			});
		}

		public async Task<string[]> Discover(string path) {
			return await RepeatableAction(async () => {
				var result = new List<string>();
				var observable = minio.ListObjectsAsync(bucketName, path);
				using var subscription = observable.Subscribe
				(
					item => result.Add(item.Key),
					ex => throw ex
				);
				await observable.ToTask();
				return result.ToArray();
			});
		}
		
		public async Task<string> DiscoverLast(string path) {
			return await RepeatableAction(async () => {
				var result = new List<string>();
				var observable = minio.ListObjectsAsync(bucketName, path);
				using var subscription = observable.Subscribe
				(
					item => result.Add(item.Key),
					ex => throw ex
				);
				await observable.ToTask();
				return result.ToArray().LastOrDefault();
			});
		}
	}
}