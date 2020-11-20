using CredentialManagement;
using System;
using System.Collections.Generic;
using System.Security;
using System.Text;
using System.Runtime.InteropServices;

namespace PasswordManager {
	class PasswordManagerPasswordManager {
		private SecureString passWord = null;


		public unsafe void RequestPassword() {
			Console.Clear();
			Console.Write("Enter master password: ");
			string tmpPass = Console.ReadLine();
			IntPtr p = Marshal.StringToHGlobalAnsi(tmpPass);
			char* newCharStr = (char*)(p.ToPointer());
			passWord = new SecureString(newCharStr, tmpPass.Length);
			SavePassword(passWord);
			//tmpPass = null;
			//newCharStr = null;
			//p = new IntPtr(null);
		}

		public void SavePassword(SecureString password) {
			try {
				using (var cred = new Credential()) {
					cred.SecurePassword = password;
					cred.Target = Console.ReadLine();
					cred.Type = CredentialType.Generic;
					cred.PersistanceType = PersistanceType.LocalComputer;
					cred.Save();
				}
			}catch (System.TypeInitializationException e) {
				Console.WriteLine(e.HelpLink);
				Console.WriteLine(e.Message);
			}
		}

		public string GetPassword() {
			if (passWord == null)
				RequestPassword();
			using (var cred = new Credential()) {
				cred.Target = Console.ReadLine();
				cred.Load();
				Console.WriteLine(cred.SecurePassword);
				return cred.Password;
			}
		}
	}
}
