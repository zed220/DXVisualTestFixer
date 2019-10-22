using DXVisualTestFixer.Common;
using Minio;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;

namespace DXVisualTestFixer.Core {
	public class MinioWorker : IMinioWorker {
		private const string bucketName = "visualtests";
		
		static readonly MinioClient minio = new MinioClient("gitlabci4-minio:9000", "xpfminio", "xpfminiostorage");

		public async Task<string> Download(string path) {
			try {
				string result = null;
				await minio.GetObjectAsync(bucketName, path, stream => {
					using var reader = new StreamReader(stream);
					result = reader.ReadToEnd();
				});
				return result;
			}
			catch {
				return null;
			}
		}
		public async Task<string[]> Discover(string path) {
			try {
				var result = new List<string>();
				var observable = minio.ListObjectsAsync(bucketName, path, false);
				using var subscription = observable.Subscribe
				(
					item => result.Add(item.Key),
					ex => throw ex
				);
				await observable.ToTask();
				return result.ToArray();
			}
			catch {
				return null;
			}
		}
	}
}