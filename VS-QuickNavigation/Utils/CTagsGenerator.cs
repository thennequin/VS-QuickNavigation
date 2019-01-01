using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
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

		static Process ExecCTags(string args)
		{
			Process process = new Process();
			process.StartInfo.UseShellExecute = false;
			process.StartInfo.FileName = GetCTagsPath();
			process.StartInfo.CreateNoWindow = true;
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

			return process;
			/*
			string output = process.StandardOutput.ReadToEnd();
			// 5. clang-format is done, wait until it is fully shut down.
			process.WaitForExit();
			if (process.ExitCode != 0)
			{
				// FIXME: If clang-format writes enough to the standard error stream to block,
				// we will never reach this point; instead, read the standard error asynchronously.
				throw new Exception(process.StandardError.ReadToEnd());
			}
			//return output;
			*/
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

		static public IEnumerable<SymbolData> GeneratorFromFilesWithProgress(IEnumerable<string> filePaths)
		{
			EnvDTE.StatusBar sbar = Common.Instance.DTE2.StatusBar;
			sbar.Progress(true, "QuickNavigation Scan solution ...", 0, 1);

			List<SymbolData> lSymbols = new List<SymbolData>();

			var filePackets = filePaths
				.Select((s, i) => new { Value = s, Index = i })
				.GroupBy(i => i.Index / 50, i => i.Value)
				.Cast<IEnumerable<string>>();

			int current = 0;
			int count = filePaths.Count();

			filePackets
				.AsParallel()
				.WithDegreeOfParallelism(4)
				.ForAll(p =>
					{
						IEnumerable<SymbolData> newSymbols = GeneratorFromFiles(p);
						lock(lSymbols)
						{
							lSymbols.AddRange(newSymbols);

							current += p.Count();

							sbar.Progress(true, "QuickNavigation Scan solution " + current + "/" + count, current, count);
						}
					});

			sbar.Progress(false);

			return lSymbols;
		}

		static public IEnumerable<SymbolData> GeneratorFromFiles(IEnumerable<string> filePaths, Action<int, int> progressAction = null)
		{
			string filePath = GetTempFile();

			File.WriteAllLines(filePath, filePaths);
			int fileCount = filePaths.Count();

			string tagsPath = GetTempFile();
			string args = "";
			if (null != progressAction)
			{
				args += "-V ";                      // Verbose for checking current file
			}

			args += "-n ";                          // Symbol stored by line number instead of pattern or line number
			args += "-L \"" + filePath + "\" ";     // Input file list
			args += "-f \"" + tagsPath + "\" ";     // Output tag file

			args += "−−c++−kinds=";                 // C++ kinds
			args += "+p";                           // Include function prototypes
			args += "+l";                           // Include local variables
			args += " ";

			args += "--fields=* ";                  // Fields

			args += "−−extras=";                    // Extras
			args += "+q";                           // Include an extra class-qualified tag entry for each tag
			args += "+r";                           // Include an extra class-qualified tag entry for each tag
			args += " ";

			IEnumerable<SymbolData> results = null;
			using (Process process = ExecCTags(args))
			{
				if (null != progressAction)
				{
					bool startScan = false;
					int currentFile = 0;
					while (!process.HasExited)
					{
						string line = process.StandardOutput.ReadLine();
						if (startScan)
						{
							if (line.StartsWith("OPENING ") || line.StartsWith("ignoring ") || line.StartsWith("ctags: Warning: cannot open"))
							{
								++currentFile;
								progressAction(currentFile, fileCount);
							}
						}
						else
						{
							if (line == "Reading list file")
							{
								startScan = true;
							}
						}
					}
				}
				else
				{
					process.WaitForExit();
				}

				if (process.ExitCode == 0)
				{
					results = ParseTagFile(tagsPath);
				}
			}

			System.IO.File.Delete(filePath);
			System.IO.File.Delete(tagsPath);

			return results;
		}

		static public void GeneratorFromSolution()
		{
			IEnumerable<string> files = Common.Instance.SolutionWatcher.Files.Select(file => file.Path);

			/*EnvDTE.StatusBar sbar = Common.Instance.DTE2.StatusBar;
			Action<int, int> progressAction = (current, total) =>
			 {
				 if ((current % 5) == 0)
				 {
					 sbar.Progress(true, "QuickNavigation Scan solution " + current + "/" + total, current, total);
				 }
			 };
			sbar.Progress(true, "QuickNavigation Scan solution ...", 0, 0);
			GeneratorFromFiles(files, progressAction).ToArray();
			sbar.Progress(false);*/
			GeneratorFromFilesWithProgress(files);
		}

		/*
		CTags tags for C/C++
			c	class name
			d	define (from #define XXX)
			e	enumerator
			f	function or method name
			F	file name
			g	enumeration name
			m	member (of structure or class data)
			p	function prototype
			s	structure name
			t	typedef
			u	union name
			v	variable
		CTags tags for C#
		*/
		static IEnumerable<SymbolData> ParseTagFile(string filePath)
		{
			List<SymbolData> symbols = new List<SymbolData>();

			using (StreamReader reader = File.OpenText(filePath))
			{
				while (!reader.EndOfStream)
				{
					string line = reader.ReadLine();
					if (!line.StartsWith("!"))
					{
						string[] tagInfos = line.Split('\t');
						//tagInfos[0] // symbol
						//tagInfos[1] // file
						//tagInfos[2] // line number
						//tagInfos[3] // type
						if (null != tagInfos && tagInfos.Length >= 4)
						{
							//string ext = Path.GetExtension(tagInfos[1]).ToLower();

							int fileLine;
							if (!ExtractLine(tagInfos, out fileLine))
								continue;

							string kind = tagInfos[3];

							if (!kind.StartsWith("kind:"))
								continue;

							kind = kind.Substring("kind:".Length);

							ESymbolType eType = ESymbolType.Namespace;
							bool bTypeFound = false;

							switch (kind)
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
								case "struct":
									eType = ESymbolType.Struct;
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
									eType = ESymbolType.Field;
									bTypeFound = true;
									break;
								case "local":
									eType = ESymbolType.Local;
									bTypeFound = true;
									break;
							}

							if (bTypeFound)
							{
								string cleanCymbol = tagInfos[0];
								int iPos = cleanCymbol.LastIndexOf(':');
								iPos = Math.Max(iPos, cleanCymbol.LastIndexOf('.'));
								if (iPos != -1)
								{
									cleanCymbol = cleanCymbol.Substring(iPos + 1);
								}

								SymbolData oSymbol = new SymbolData(cleanCymbol, fileLine, eType);

								for (int i = 4; i < tagInfos.Length; ++i)
								{
									if (tagInfos[i].StartsWith("signature:"))
									{
										oSymbol.Parameters = tagInfos[i].Substring("signature:".Length);
									}
									else if (tagInfos[i].StartsWith("scope:"))
									{
										oSymbol.Scope = tagInfos[i].Substring("scope:".Length);
									}
									else if (tagInfos[i].StartsWith("typeref:"))
									{
										string sTypeRef = tagInfos[i].Substring("typeref:".Length);
										if (sTypeRef.StartsWith("typename:"))
											sTypeRef = sTypeRef.Substring("typename:".Length);

										oSymbol.TypeRef = sTypeRef;
									}
									else if (tagInfos[i].StartsWith("access:"))
									{
										oSymbol.Access = tagInfos[i].Substring("access:".Length);
									}
									else if (tagInfos[i].StartsWith("inherits:"))
									{
										oSymbol.Inherits = tagInfos[i].Substring("inherits:".Length).Split(',');
									}
									else if (tagInfos[i].StartsWith("end:"))
									{
										string sEndLine = tagInfos[i].Substring("end:".Length);
										if (int.TryParse(sEndLine, out int iEndLine))
										{
											oSymbol.EndLine = iEndLine;
										}
									}
								}

								string sFileName = tagInfos[1];
								sFileName = sFileName.Replace("\\\\", "\\");
								oSymbol.AssociatedFile = Common.Instance.SolutionWatcher.GetFileDataByPath(sFileName);
								symbols.Add(oSymbol);
							}
						}
						else
						{
							System.Diagnostics.Debug.WriteLine("WTF! wrong formatted CTags");
							System.Diagnostics.Debug.Assert(false);
						}
					}
				}
			}

			return symbols;
		}

		static bool ExtractLine(string[] tagInfos, out int line)
		{
			string tagLineNumberStr = tagInfos[2];
			if (tagLineNumberStr.EndsWith(";\""))
			{
				tagLineNumberStr = tagLineNumberStr.Substring(0, tagLineNumberStr.Length - 2); // remove ;"
			}
			return int.TryParse(tagLineNumberStr, out line);
		}
	}
}
