using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using VS_QuickNavigation.Data;
using TinyJson;

namespace VS_QuickNavigation.Utils
{
	public class CTagsGenerator
	{
		static readonly string s_sCTagsReleasesUrl = @"https://api.github.com/repos/universal-ctags/ctags-win32/releases";
		static readonly string s_sCTagsExeRegex = @".*-x64\.zip";

		public class GithubReleaseAsset
		{
			public string name;
			public string browser_download_url;
		}

		public class GithubRelease
		{
			public string name;
			public bool draft;
			public string published_at;
			public List<GithubReleaseAsset> assets;
		}

		static string GetCTagsPath()
		{
			return Common.Instance.ExtensionFolder + "\\external\\ctags.exe";
		}

		public static bool CTagsPresent
		{
			get
			{
				return System.IO.File.Exists(GetCTagsPath());
			}
		}

		private static bool ValidateRemoteCertificate(object sender, System.Security.Cryptography.X509Certificates.X509Certificate cert, System.Security.Cryptography.X509Certificates.X509Chain chain, System.Net.Security.SslPolicyErrors error)
		{
			// If the certificate is a valid, signed certificate, return true.
			if (error == System.Net.Security.SslPolicyErrors.None)
			{
				return true;
			}

			Console.WriteLine("X509Certificate [{0}] Policy Error: '{1}'",
				cert.Subject,
				error.ToString());

			return false;
		}

		public static List<GithubRelease> RetieveGithubCTagsReleases()
		{
			List<GithubRelease> lCTagsReleases = null;
			try
			{
				string sJson = null;

				System.Net.ServicePointManager.ServerCertificateValidationCallback += ValidateRemoteCertificate;
				System.Net.ServicePointManager.SecurityProtocol =
						System.Net.SecurityProtocolType.Ssl3
						| System.Net.SecurityProtocolType.Tls
						| System.Net.SecurityProtocolType.Tls11
						| System.Net.SecurityProtocolType.Tls12;

				using (System.Net.WebClient oWebClient = new System.Net.WebClient())
				{
					oWebClient.Headers.Add("user-agent", "VisualStudio");
					sJson = oWebClient.DownloadString(s_sCTagsReleasesUrl);
				}

				if (sJson != null)
				{
					lCTagsReleases = sJson.FromJson<List<GithubRelease>>();
				}
			}
			catch(Exception) {};

			return lCTagsReleases;
		}

		public static IEnumerable<string> RetrieveCTagsVersion()
		{
			List<GithubRelease> lCTagsReleases = RetieveGithubCTagsReleases();

			if (lCTagsReleases != null)
			{
				System.Text.RegularExpressions.Regex oRegex = new System.Text.RegularExpressions.Regex(s_sCTagsExeRegex);
				foreach (GithubRelease oRelease in lCTagsReleases)
				{
					if (oRelease != null
						&& oRelease.name != null
						&& oRelease.draft == false
						&& oRelease.assets != null
						&& oRelease.assets.Any(asset => asset != null && oRegex.IsMatch(asset.name)))
					{
						yield return oRelease.name;
					}
				}
			}
		}

		public static bool DownloadCTags(string sVersion)
		{
			List<GithubRelease> lCTagsReleases = RetieveGithubCTagsReleases();
			if (lCTagsReleases != null)
			{
				System.Text.RegularExpressions.Regex oRegex = new System.Text.RegularExpressions.Regex(s_sCTagsExeRegex);
				GithubRelease oRelease = lCTagsReleases.FirstOrDefault(r => r != null && (string.IsNullOrEmpty(sVersion) || r.name.Equals(sVersion)));
				if (oRelease != null)
				{
					GithubReleaseAsset oAsset = oRelease.assets.FirstOrDefault(asset => asset != null && oRegex.IsMatch(asset.name));
					if (oAsset != null)
					{
						try
						{
							System.Net.WebClient oWebClient = new System.Net.WebClient();
							oWebClient.Headers.Add("user-agent", "VisualStudio");
							string sZipPath = System.IO.Path.GetTempFileName();
							oWebClient.DownloadFile(oAsset.browser_download_url, sZipPath);

							bool bFound = false;
							using (System.IO.FileStream oFileStream = new FileStream(sZipPath, FileMode.Open))
							using (System.IO.Compression.ZipArchive oZipArchive = new System.IO.Compression.ZipArchive(oFileStream, System.IO.Compression.ZipArchiveMode.Read))
							{
								System.IO.Compression.ZipArchiveEntry oEntry = oZipArchive.GetEntry("ctags.exe");
								if (oEntry != null)
								{
									using (Stream oEntryStream = oEntry.Open())
									using (System.IO.FileStream oCTagsFileStream = new FileStream(GetCTagsPath(), FileMode.Create))
									{
										oEntryStream.CopyTo(oCTagsFileStream);
									}

									bFound = true;
								}
							}

							System.IO.File.Delete(sZipPath);
							return bFound;
						}
						catch { }
					}
				}
			}
			return false;
		}

		static string GetTempFile()
		{
			//System.IO.Path.GetTempPath() + Guid.NewGuid().ToString() + ".db";
#if DEBUG
			return Common.Instance.DataFolder + "\\temp-" + Guid.NewGuid().ToString() + ".db";
#else
			return System.IO.Path.GetTempFileName();
#endif
		}

		static public IEnumerable<SymbolData> GeneratorFromDocument(EnvDTE.Document document)
		{
			if (null != document)
			{
				if (document.Saved)
				{
					return GeneratorFromFile(document.FullName);
				}
				else
				{
					EnvDTE.TextDocument doc = (EnvDTE.TextDocument)Common.Instance.DTE2.ActiveDocument.Object("TextDocument");
					if (doc != null)
					{
						string fileContent = doc.StartPoint.CreateEditPoint().GetText(doc.EndPoint);

						return GeneratorFromString(fileContent, Path.GetExtension(document.FullName));
					}
				}
			}
			return null;
		}

		static public IEnumerable<SymbolData> GeneratorFromString(string fileContent, string ext)
		{
			string filePath = GetTempFile() + "." + ext;
			File.WriteAllText(filePath, fileContent);

			IEnumerable<SymbolData> results = GeneratorFromFile(filePath);

			System.IO.File.Delete(filePath);

			return results;
		}

		static public IEnumerable<SymbolData> GeneratorFromFile(string filePath)
		{
			return GeneratorFromFiles(CommonUtils.ToArray<string>(filePath));
		}

		static public IEnumerable<SymbolData> GeneratorFromFilesWithProgress(IEnumerable<string> filePaths, CancellationTokenSource oCancellationToken)
		{
			EnvDTE.StatusBar sbar = Common.Instance.DTE2.StatusBar;
			sbar.Progress(true, "QuickNavigation Scan solution ...", 0, 1);
			sbar.Animate(true, EnvDTE.vsStatusAnimation.vsStatusAnimationGeneral);

			Stopwatch oSW = Stopwatch.StartNew();
			IEnumerable<SymbolData> lSymbols = GeneratorFromFiles(filePaths, (iCurrent, iCount) =>
			{
				if (iCurrent == 1 || iCurrent == iCount || oSW.ElapsedMilliseconds > 250)
				{
					oSW.Restart();
					sbar.Progress(true, "QuickNavigation Scan solution " + iCurrent + "/" + iCount, iCurrent, iCount);
				}
			}, oCancellationToken);

			sbar.Animate(false, EnvDTE.vsStatusAnimation.vsStatusAnimationGeneral);
			sbar.Progress(false);

			return lSymbols;
		}

		static public IEnumerable<SymbolData> GeneratorFromFiles(IEnumerable<string> filePaths, Action<int, int> progressAction = null, CancellationTokenSource oCancellationToken = null)
		{
			FutureList<System.Collections.Generic.IEnumerable<Data.SymbolData>> oFutures = CTagsTask.GetInstance().AddFiles(filePaths);
			if (progressAction != null)
			{
				int iCompletedCount = 0;
				int iFileCount = oFutures.Count();
				while (oFutures.IsCompleted == false && (oCancellationToken == null || oCancellationToken.IsCancellationRequested == false))
				{
					oFutures.WaitAny();

					++iCompletedCount;
					progressAction(iCompletedCount, iFileCount);
				}
			}
			else
			{
				while (oFutures.IsCompleted == false && (oCancellationToken == null || oCancellationToken.IsCancellationRequested == false))
				{
					oFutures.WaitAny();
				}
			}

			if (oCancellationToken != null && oCancellationToken.IsCancellationRequested)
			{
				CTagsTask.GetInstance().Cancels(oFutures);
			}

			return oFutures.SelectMany(f => f.Result);
		}

		public class CTagsTask : IDisposable
		{
			class CTagCommand
			{
				public string	command;
			}

			class CTagCommandGenerateTagsFile : CTagCommand
			{
				public CTagCommandGenerateTagsFile(string sFile)
				{
					command = "generate-tags";
					filename = sFile;
				}

				public string	filename;
			}

#pragma warning disable 0649
			class CTagResult
			{
				public string	_type;
			}

			class CTagResultProgram : CTagResult
			{
				public string	name;
				public string	version;
			}

			class CTagResultTag : CTagResult
			{
				public string	name;
				public string	path;
				public string	pattern;
				public string	language;
				public int		line;
				public string	signature;
				public string	typeref;
				public string	inherits;
				public string	kind;
				public string	roles;
				public string	scope;
				public string	scopeKind;
				public int		end;
				public string	access;
				public string	extras;
			}
#pragma warning restore 0649

			struct File
			{
				public string sFile;
				public Future<IEnumerable<SymbolData>> oResults;
			}

			static CTagsTask s_oInstance = null;

			List<File> m_sFilesToParse = new List<File>();
			CancellationTokenSource m_oCancellationToken;
			Semaphore m_oSemaphore;
			List<Thread> m_lThreads;

			int m_iThreadCount = Environment.ProcessorCount;
			public int ThreadCount
			{
				get
				{
					return m_iThreadCount;
				}
				set
				{
					if (value > 0)
					{
						m_iThreadCount = value;
					}
				}
			}

			static public void CreateInstance()
			{
				s_oInstance = new CTagsTask();
			}

			static public CTagsTask GetInstance()
			{
				return s_oInstance;
			}

			CTagsTask()
			{
				m_oSemaphore = new Semaphore(0, int.MaxValue);

				m_lThreads = new List<Thread>();
				StartThreads();
			}

			void StartThreads()
			{
				StopThreads();
				m_oCancellationToken = new CancellationTokenSource();
				for (int i = 0; i < ThreadCount; ++i)
				{
					System.Threading.Thread oThread = new System.Threading.Thread(new ThreadStart(TaskCTagsInteractive));
					oThread.Start();
					m_lThreads.Add(oThread);
				}
			}

			void StopThreads()
			{
				if (m_oCancellationToken != null)
				{
					m_oCancellationToken.Cancel();
					m_oSemaphore.Release(m_lThreads.Count);
					foreach (Thread oThread in m_lThreads)
					{
						oThread.Join();
					}
					m_lThreads.Clear();
					m_oCancellationToken = null;
				}
			}

			public void Dispose()
			{
				StopThreads();
				s_oInstance = null;
			}

			public Future<IEnumerable<SymbolData>> AddFile(string sFile)
			{
				if (string.IsNullOrWhiteSpace(sFile))
					return null;

				Future<IEnumerable<SymbolData>> oResults = null;
				Monitor.Enter(m_sFilesToParse);
				if (m_sFilesToParse.Any(f => sFile.Equals(f.sFile, StringComparison.InvariantCultureIgnoreCase)) == false)
				{
					File oFile = new File { sFile = sFile, oResults = new Future<IEnumerable<SymbolData>>() };
					m_sFilesToParse.Add(oFile);
					oResults = oFile.oResults;
					m_oSemaphore.Release();
				}
				Monitor.Exit(m_sFilesToParse);
				return oResults;
			}

			public FutureList<IEnumerable<SymbolData>> AddFiles(IEnumerable<string> sFiles)
			{
				FutureList<IEnumerable<SymbolData>> oResults = new FutureList<IEnumerable<SymbolData>>();
				foreach (string sFile in sFiles)
				{
					oResults.Add(AddFile(sFile));
				}
				return oResults;
			}

			public void Cancel(Future<IEnumerable<SymbolData>> oFuture)
			{
				Monitor.Enter(m_sFilesToParse);
				m_sFilesToParse.RemoveAll(f => f.oResults == oFuture);
				Monitor.Exit(m_sFilesToParse);
			}

			public void Cancels(FutureList<IEnumerable<SymbolData>> oFutures)
			{
				Monitor.Enter(m_sFilesToParse);
				m_sFilesToParse.RemoveAll(f => oFutures.Where(f2=>f2.Result == f.oResults).Any());
				Monitor.Exit(m_sFilesToParse);
			}

			void TaskCTagsInteractive()
			{
				string args = "";

				args += "-n ";                          // Symbol stored by line number instead of pattern or line number

				args += "−−c++−kinds=";                 // C++ kinds
				args += "+p";                           // Include function prototypes
				args += "+l";                           // Include local variables
				args += " ";

				args += "--fields=* ";                  // Fields

				args += "−−extras=";                    // Extras
				args += "+r";                           // References
				args += "+F";                           // File scope
				args += "+p";                           // Pseudo tag
				args += " ";

				args += "--_interactive ";

				Process process = new Process();
				process.StartInfo.UseShellExecute = false;
				process.StartInfo.FileName = GetCTagsPath();
				process.StartInfo.CreateNoWindow = true;
				process.StartInfo.RedirectStandardInput = true;
				process.StartInfo.RedirectStandardOutput = true;
				process.StartInfo.RedirectStandardError = true;
				process.StartInfo.Arguments = args;

				try
				{
					process.Start();
					process.PriorityClass = ProcessPriorityClass.BelowNormal;
				}
				catch (Exception e)
				{
					throw new Exception(
						"Cannot execute " + process.StartInfo.FileName + ".\n\"" +
						e.Message + "\".\nPlease make sure it is on the PATH.");
				}

				string sJson = process.StandardOutput.ReadLine();

				if (sJson != null)
				{
					CTagResultProgram oProgramResult = sJson.FromJson<CTagResultProgram>();
				}
				else
				{
					sJson = process.StandardError.ReadLine();
				}

				while (process.HasExited == false)
				{
					m_oSemaphore.WaitOne();
					if (m_oCancellationToken.IsCancellationRequested)
					{
						process.Kill();
					}
					else
					{
						Monitor.Enter(m_sFilesToParse);
						if (m_sFilesToParse.Count > 0)
						{
							File oFile = m_sFilesToParse[0];
							m_sFilesToParse.RemoveAt(0);

							Monitor.Exit(m_sFilesToParse);

							CTagCommandGenerateTagsFile oCommand = new CTagCommandGenerateTagsFile(oFile.sFile);

							List<SymbolData> symbols = new List<SymbolData>();

							process.StandardInput.WriteLine(oCommand.ToJson());
							while (true)
							{
								sJson = process.StandardOutput.ReadLine();
								//System.Diagnostics.Debug.WriteLine(sJson);

								CTagResultTag oResult = sJson.FromJson<CTagResultTag>();
								if (oResult != null)
								{
									if (oResult._type == "completed")
									{
										break;
									}
									else if (oResult._type == "tag")
									{
										ESymbolType eType = ESymbolType.Namespace;
										bool bTypeFound = false;

										switch (oResult.kind)
										{
											case "function":
											case "method":
												eType = ESymbolType.Method;
												bTypeFound = true;
												break;
											case "prototype":
												eType = ESymbolType.MethodPrototype;
												bTypeFound = true;
												break;
											case "property":
												eType = ESymbolType.Property;
												bTypeFound = true;
												break;
											case "namespace":
												eType = ESymbolType.Namespace;
												bTypeFound = true;
												break;
											case "struct":
												eType = ESymbolType.Struct;
												bTypeFound = true;
												break;
											case "union":
												eType = ESymbolType.Union;
												bTypeFound = true;
												break;
											case "class":
												eType = ESymbolType.Class;
												bTypeFound = true;
												break;
											case "interface":
												eType = ESymbolType.Interface;
												bTypeFound = true;
												break;
											case "typedef":
												eType = ESymbolType.TypeDef;
												bTypeFound = true;
												break;
											case "macro":
												eType = ESymbolType.Macro;
												bTypeFound = true;
												break;
											case "enum":
												eType = ESymbolType.Enumeration;
												bTypeFound = true;
												break;
											case "enumerator":
												eType = ESymbolType.Enumerator;
												bTypeFound = true;
												break;
											case "member":
											case "field":
												eType = ESymbolType.Field;
												bTypeFound = true;
												break;
											case "variable":
												eType = ESymbolType.Variable;
												bTypeFound = true;
												break;
											case "local":
												eType = ESymbolType.Local;
												bTypeFound = true;
												break;
										}
										if (bTypeFound)
										{
											string cleanCymbol = oResult.name;
											int iPos = cleanCymbol.LastIndexOf(':');
											iPos = Math.Max(iPos, cleanCymbol.LastIndexOf('.'));
											if (iPos != -1)
											{
												cleanCymbol = cleanCymbol.Substring(iPos + 1);
											}

											SymbolData oSymbol = new SymbolData(cleanCymbol, oResult.line, eType);

											if (oResult.signature != null)
											{
												oSymbol.Parameters = oResult.signature;
											}

											if (oResult.scope != null)
											{
												if (oResult.scopeKind != null)
													oSymbol.Scope = oResult.scopeKind + ":" + oResult.scope;
												else
													oSymbol.Scope = "unknown:" + oResult.scope;
											}

											if (oResult.typeref != null)
											{
												string sTypeRef = oResult.typeref;
												if (sTypeRef.StartsWith("typename:"))
													sTypeRef = sTypeRef.Substring("typename:".Length);
												oSymbol.TypeRef = sTypeRef;
											}

											if (oResult.access != null)
											{
												oSymbol.Access = oResult.access;
											}

											if (oResult.inherits != null)
											{
												oSymbol.Inherits = oResult.inherits.Split(',');
											}

											if (oResult.end != -1)
											{
												oSymbol.EndLine = oResult.end;
											}

											string sFileName = oResult.path;
											oSymbol.AssociatedFile = Common.Instance.SolutionWatcher.GetFileDataByPath(sFileName);
											symbols.Add(oSymbol);
										}
									}
								}
							};

							//Thread.Sleep(1000);
							oFile.oResults.SetResults(symbols);
						}
						else
						{
							Monitor.Exit(m_sFilesToParse);
							Thread.Sleep(150); //Should not happen
						}
					}
				}
			}
		}
	}
}
