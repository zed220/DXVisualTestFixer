using System;
using System.DirectoryServices;
using System.DirectoryServices.AccountManagement;
using System.IO;
using System.Security.Principal;
using System.Threading.Tasks;

namespace DXVisualTestFixer.UI.Native {
	static class LoginExtractor {
		public static async Task<string> GetLoginAsync() {
			await Task.Delay(1).ConfigureAwait(false);
			try {
				var path = Path.Combine(@"\\corp\internal\common\visualTests_squirrel\AccessTemp\", Path.GetRandomFileName());
				using var fs = File.Create(path, 1, FileOptions.DeleteOnClose);
				var userId = File.GetAccessControl(path).GetOwner(typeof(SecurityIdentifier));

				using var context = new PrincipalContext(ContextType.Domain, "corp.devexpress.com");
				using var searcher = new PrincipalSearcher(new UserPrincipal(context));
				foreach(var result in searcher.FindAll()) {
					if(result.Sid.ToString() == userId.Value)
						return result.SamAccountName;
				}
				return null;
			}
			catch {
				return null;
			}
		}
		public static async Task<bool> CheckLoginAsync(string login) {
			await Task.Delay(1).ConfigureAwait(false);
			try {
				using var context = new PrincipalContext(ContextType.Domain, "corp.devexpress.com");
				using var searcher = new PrincipalSearcher(new UserPrincipal(context));
				foreach(var result in searcher.FindAll()) {
					if(result.SamAccountName == login)
						return true;
				}
			}
			catch {
				return false;
			}
			return false;
		}
	}
}