using System.IO;
using System.Threading.Tasks;
using DXVisualTestFixer.Common;
using Minio;

namespace DXVisualTestFixer.Core {
	public class MinioWorker : IMinioWorker {
		static readonly MinioClient minio = new MinioClient("gitlabci7-minio:9000", "xpfminio", "xpfminiostorage");

		public async Task<string> Download(string path) {
			try {
				string result = null;
				await minio.GetObjectAsync("visualtests", path, stream => {
					using var reader = new StreamReader(stream);
					result = reader.ReadToEnd();
				});
				return result;
			}
			catch {
				return null;
			}
		}
	}
}