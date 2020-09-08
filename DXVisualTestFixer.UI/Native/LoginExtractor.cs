using System;
using System.DirectoryServices;
using System.DirectoryServices.AccountManagement;
using System.IO;
using System.Linq;
using System.Security.Principal;
using System.Threading.Tasks;

namespace DXVisualTestFixer.UI.Native {
	class LoginInfo {
		public static readonly LoginInfo Empty = new LoginInfo(null, null);
		
		public readonly string FullName;
		public readonly string Email;
		public LoginInfo(string fullName, string email) {
			FullName = fullName;
			Email = email;
		}
	}
	
	static class LoginExtractor {
		public static async Task<LoginInfo> GetLoginInfoAsync() {
			await Task.Delay(1).ConfigureAwait(false);
			try {
				var path = Path.Combine(@"\\corp\internal\common\visualTests_squirrel\AccessTemp\", Path.GetRandomFileName());
				using var fs = File.Create(path, 1, FileOptions.DeleteOnClose);
				var userId = File.GetAccessControl(path).GetOwner(typeof(SecurityIdentifier));

				using var context = new PrincipalContext(ContextType.Domain, "corp.devexpress.com");
				using var searcher = new PrincipalSearcher(new UserPrincipal(context));
				foreach(var result in searcher.FindAll().OfType<UserPrincipal>()) {
					if(result.Sid.ToString() == userId.Value)
						return new LoginInfo(result.SamAccountName, result.EmailAddress);
				}
				return LoginInfo.Empty;
			}
			catch {
				return LoginInfo.Empty;
			}
		}
		public static async Task<bool> CheckLoginAsync(LoginInfo login) {
			if(login == null || string.IsNullOrEmpty(login.FullName) || string.IsNullOrEmpty(login.Email))
				return false;
			await Task.Delay(1).ConfigureAwait(false);
			try {
				using var context = new PrincipalContext(ContextType.Domain, "corp.devexpress.com");
				using var searcher = new PrincipalSearcher(new UserPrincipal(context));
				foreach(var result in searcher.FindAll().OfType<UserPrincipal>()) {
					if(result.SamAccountName == login.FullName &&
					   result.EmailAddress == login.Email)
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