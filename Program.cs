using System;
//using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;

namespace PasswordManager {
	class Program {
		const uint MB_YESNO = (uint) 0x00000004L;
		const uint MB_ICONASTERISK = (uint) 0x00000040L;
		const uint MB_OKCANCEL = (uint) 0x00000001L;
		const int IDOK = 1;
		const int IDCANCEL = 2;
		const int ID = 6;
		private static PasswordManagerPasswordManager passwordManager = new PasswordManagerPasswordManager();
		[DllImport("User32.dll", CharSet = CharSet.Unicode)]
		public static extern int MessageBox(IntPtr handler, string message, string caption, uint type);

		static Dictionary<string, string> dict;
		static string helpText = "Press Ctrl+Q to quit, Ctrl+S to save, N to create a new entry, R to retrieve an entry, or I to print statistics.";
		static void Main(string[] args) {
			//Console.WriteLine(passwordManager.GetPassword());
			dict = new Dictionary<string, string>();
			Console.WriteLine("Checking for a pre-existing password record file...");
			
			try {
				using (System.IO.FileStream fileStream = new System.IO.FileStream("pass.bin", System.IO.FileMode.Open)) {
					//fileStream.Lock(0, fileStream.Length);
					Console.WriteLine("Password record file found, loading...");

					/** Saving order
					  * 
					  * Serialize
					  * Encrypt
					  * Write binary
					  */

					// Read Binary
					// Decrypt
					// Deserialize
					using (System.IO.BinaryReader br = new System.IO.BinaryReader(fileStream)) {
						try {
							string encryptedDictString = br.ReadString();
							br.Close();
							string dictString = EncryptionHelper.Decrypt(encryptedDictString, ask<string>("Enter master password: "));
							dict = new Dictionary<string, string>();
							try {
								dict = (Dictionary<string, string>) JsonSerializer.Deserialize(dictString, dict.GetType());
							} catch (Exception e) {
								if (e is ArgumentNullException) {
									Console.WriteLine("Cannot write null to filestream. Why? I don't know. You just can't. Period.");
								} else if (e is JsonException) {
									Console.WriteLine("Invalid JSON, maximum object depth allowed, or JSON is incompatible with password storage data type.");
								} else {
									Console.WriteLine("An unexpexted error occured! If you see this message, you didn't screw" +
										" up, I did. Just open up an issue at https://github.com/Catz1301/PasswordManager/issues and provide the following information:");
								}
								Console.WriteLine(Environment.NewLine + e.Message);
								Console.WriteLine(Environment.NewLine + "-+-+-+-+-+-+-+-+-+-+-+-+-" + Environment.NewLine);
								Console.WriteLine(e.StackTrace);
								Console.WriteLine(Environment.NewLine + "Source: " + e.Source);
								Console.WriteLine(Environment.NewLine + "Extra Data: ");
								Console.WriteLine(e.Data);
								Exception exception = new Exception("An error occured while deserializing JSON data. " + e.Message, e.InnerException);
								throw exception;
							}


						} catch (Exception e) {
							if (e is System.IO.IOException) {
								Console.WriteLine("An error occurred while writing to file.");
							} else if (e is System.IO.InternalBufferOverflowException) {
								Console.WriteLine("Internal buffer has overflown, maybe try saving more often?");
							} else if (e is System.IO.EndOfStreamException) {
								Console.WriteLine("Unexpectedly reached the end of stream. This may mean that the file is corrupted. Hope you have a backup or a plan!");
							} else if (e is ObjectDisposedException) {
								Console.WriteLine("Cannot write to disposed object! If you see this message, you didn't screw" +
									" up, I did. Just open up an issue at https://github.com/Catz1301/PasswordManager/issues and provide the following information:");
							} else {
								Console.WriteLine("An unexpexted error occured! If you see this message, you didn't screw" +
									" up, I did. Just open up an issue at https://github.com/Catz1301/PasswordManager/issues and provide the following information:");
							}
							Console.WriteLine(Environment.NewLine + e.Message);
							Console.WriteLine(Environment.NewLine + "-+-+-+-+-+-+-+-+-+-+-+-+-" + Environment.NewLine);
							Console.WriteLine(e.StackTrace);
							Console.WriteLine(Environment.NewLine + "Source: " + e.Source);
							Console.WriteLine(Environment.NewLine + "Extra Data: ");
							Console.WriteLine(e.Data);
							throw;
						} finally {
							br.Close();
						}
			//dict = (Dictionary<string, string>) bf.Deserialize(fileStream);
		}
				}
				/*fileStream.Unlock(0, fileStream.Length);
				fileStream.Close();
				await fileStream.DisposeAsync();*/
			} catch (Exception e) {
				if (e is System.IO.FileNotFoundException) {
					Console.WriteLine("No password record file found!");
					Console.WriteLine("Creating a new one...");
					dict = new Dictionary<string, string>();
				} else if (e is SerializationException) {
					Console.WriteLine("Something went wrong.");
					Console.WriteLine(e);
				}
			}
			
			System.Threading.Thread.Sleep(2000);
			Console.Clear();
			Console.WriteLine(helpText);
			bool quit = false;
			while (!quit) {
				ConsoleKeyInfo keyInfo = Console.ReadKey(true);
				ConsoleKey key = keyInfo.Key;
				ConsoleModifiers modKeys = keyInfo.Modifiers;
				if (modKeys == ConsoleModifiers.Control) {
					if (key == ConsoleKey.S) {
						if (saveConfirmation()) {
							save();
						}
					} else if (key == ConsoleKey.Q) {
						quit = singleCharConfirm("Are you sure?");
					} else {
						continue;
					}
				} else {
					if (key == ConsoleKey.N) {
						createEntry();
					} else if (key == ConsoleKey.R) {
						retrieveEntry();
					} else if (key == ConsoleKey.I) {
						printInfo();
					} else {
						Console.WriteLine("Invalid option. " + helpText);
					}
				}
			}
		}

		static T ask<T>(String prompt) where T : IConvertible {
			Console.Write(prompt);
			String input = Console.ReadLine();
			return (T) Convert.ChangeType(input, typeof(T));
		}

		static void save() {
			using (System.IO.FileStream fileStream = new System.IO.FileStream("pass.bin", System.IO.FileMode.Create)) {
				string dictString = JsonSerializer.Serialize(dict, dict.GetType());
				
				string encryptedDictString = EncryptionHelper.Encrypt(dictString, ask<string>("Enter password (If this is the first save, enter anything, but write it down!): "));
				try {
					//bf.Serialize(fileStream, dict);
					using (System.IO.BinaryWriter bw = new System.IO.BinaryWriter(fileStream)) {
						try {
							bw.Write(encryptedDictString);
							bw.Close();
							Console.WriteLine("Save successful!");
							System.Threading.Thread.Sleep(1000);
							Console.Clear();
							Console.WriteLine(helpText);
						} catch (Exception e) {
							if (e is System.IO.IOException) {
								Console.WriteLine("An error occurred while writing to file.");
							} else if (e is System.IO.InternalBufferOverflowException) {
								Console.WriteLine("Internal buffer has overflown, maybe try saving more often?");
							} else if (e is ArgumentNullException) {
								Console.WriteLine("Cannot write null to filestream. Why? I don't know. You just can't. Period.");
							} else if (e is ObjectDisposedException) {
								Console.WriteLine("Cannot write to disposed object! If you see this message, you didn't screw" +
									" up, I did. Just open up an issue at https://github.com/Catz1301/PasswordManager/issues and provide the following information:");
							} else {
								Console.WriteLine("An unexpexted error occured! If you see this message, you didn't screw" +
									" up, I did. Just open up an issue at https://github.com/Catz1301/PasswordManager/issues and provide the following information:");
							}
							Console.WriteLine(Environment.NewLine + e.Message);
							Console.WriteLine(Environment.NewLine + "-+-+-+-+-+-+-+-+-+-+-+-+-" + Environment.NewLine);
							Console.WriteLine(e.StackTrace);
							Console.WriteLine(Environment.NewLine + "Source: " + e.Source);
							Console.WriteLine(Environment.NewLine + "Extra Data: ");
							Console.WriteLine(e.Data);
							throw;
						} finally {
							bw.Close();
						}
					}
				} catch (SerializationException e) {
					Console.BackgroundColor = ConsoleColor.Red;
					Console.ForegroundColor = ConsoleColor.Black;
					Console.Clear();
					Console.WriteLine("Failed to serialize! Reason: " + e.Message);
					throw;
				} finally {
					fileStream.Close();
				}
			}
		}

		static bool saveConfirmation() {
			int confirm = MessageBox((IntPtr) 0, "Do you wish to save your passwords?", "Save Confirmation", MB_ICONASTERISK | MB_YESNO);
			return (confirm == 6);
		}

		static void printInfo() {
			List<string> passes = new List<string>();
			int numTimesReused = 0;
			foreach(KeyValuePair<string, string> keyValue in dict) {
				if (passes.Contains(keyValue.Value))
					numTimesReused++;
				else
					passes.Add(keyValue.Value);
			}
			Console.WriteLine("{0} passwords saved, {1} cases of password reuse.", dict.Count, numTimesReused);

			if (numTimesReused != 0) {
				Console.WriteLine("Password reuse can be bad for online security; if there is a data breach that contains a password that you've reused, or an attacker has discovered one of your passwords, then an attacker may go to other sites that you are registered at and attempt to break into your account. It's pretty rare, but because there are so many accounts and so many attackers, it's becoming increasingly common.");
				char quitConfirmationChar = ask<char>("Would you like to learn more about password safety? y/N: ");
				bool showInfo = (quitConfirmationChar.ToString().ToLower() == "y");
				if (showInfo) {
					int passwdSafetyMB = MessageBox((IntPtr) 0, "This will take you to https://edu.gcfglobal.org/en/internetsafety/creating-strong-passwords/1/. Do you wish to continue?", "My Message Box", MB_ICONASTERISK | MB_YESNO);
					if (passwdSafetyMB == IDOK) {
						OpenUrl("https://edu.gcfglobal.org/en/internetsafety/creating-strong-passwords/1/");
					}
				}
			}
		}

		static bool createEntry() {
			string domain = ask<string>("Website: ");
			string password = ask<string>("Password: ");
			bool success = true;
			if (!dict.TryAdd(domain, password)) {
				if (singleCharConfirm("A password for this website already exists. Do you wish to overwrite it?")) {
					if (!dict.Remove(domain)) {
						Console.WriteLine("Could not remove keyvalue {0}", domain);
						success = false;
					} else {
						dict.Add(domain, password);
						Console.WriteLine("Password for {0} has been set.", domain);
					}
				} else {
					Console.WriteLine("Cancelling...");
					success = false;
				}
			} else {
				
				Console.WriteLine("Password for {0} has been set.", domain);
			}
			System.Threading.Thread.Sleep(1000);
			Console.Clear();
			Console.WriteLine(helpText);
			return success;
		}

		static void retrieveEntry() {
			string domain = ask<string>("Website: ");
			if (dict.ContainsKey(domain)) {
				string passwd;
				dict.TryGetValue(domain, out passwd);
				Console.WriteLine(passwd);
				System.Threading.Thread.Sleep(5000);
				Console.Clear();
				Console.WriteLine();
			} else {
				Console.WriteLine("No password exists for {0};", domain);
				System.Threading.Thread.Sleep(1000);
			}
		}

		static bool singleCharConfirm(string prompt) {
			if (!prompt.EndsWith(" "))
				prompt += " ";
			Console.Write(prompt + "y/N: ");
			char confirmChar = Convert.ToChar(Console.Read());
			return (confirmChar == 'y' || confirmChar == 'Y');
		}

		static void OpenUrl(string url) {
			try {
				Process.Start(url);
			} catch {
				// hack because of this: https://github.com/dotnet/corefx/issues/10361
				if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
					url = url.Replace("&", "^&");
					Process.Start(new ProcessStartInfo("cmd", $"/c start {url}") { CreateNoWindow = true });
				} else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) {
					Process.Start("xdg-open", url);
				} else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) {
					Process.Start("open", url);
				} else {
					throw;
				}
			}
		}
	}
}
