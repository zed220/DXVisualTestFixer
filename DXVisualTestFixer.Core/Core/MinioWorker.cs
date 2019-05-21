using DXVisualTestFixer.Common;
using Minio;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DXVisualTestFixer.Core {
    public class MinioWorker : IMinioWorker {
        static MinioClient minio = new MinioClient("gitlabci1-minio:9000", "xpfminio", "xpfminiostorage");

        public async Task<string> Download(string path) {
            try {
                string result = null;
                await minio.GetObjectAsync("visualtests", path, stream => {
                    using(var reader = new StreamReader(stream)) {
                        result = reader.ReadToEnd();
                    }
                });
                return result;
            }
            catch {
                return null;
            }
        }
    }
}
