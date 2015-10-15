using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using VS_QuickNavigation.Data;

namespace VS_QuickNavigation.Utils
{
	public class CTagsGenerator
	{
		static string GetCTagsPath()
		{
			return Common.Instance.ExtensionFolder + "\\external\\ctags.exe";
		}

		static Process ExecCTags(string args)
		{
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
			return System.IO.Path.GetTempFileName();
			//String dataFolder = Common.Instance.DataFolder;
			//return dataFolder + "\\temp." + ext;
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
			string filePath = GetTempFile();
			File.WriteAllText(filePath, fileContent);

			IEnumerable<SymbolData> results = GeneratorFromFile(filePath);
			System.IO.File.Delete(filePath);
			return results;
		}

		

		static public IEnumerable<SymbolData> GeneratorFromFile(string filePath)
		{
			string tagsPath = GetTempFile();
			string args = "";
			args += "-n ";							// Symbol stored by line number instead of pattern or line number
			args += "-f \"" + tagsPath + "\" ";		// Output tag file
			args += "−−c++−kinds=+p ";				// Include declarations
			args += "--fields=+S+m ";               // Add parameters fields & Implementation information
			args += filePath;                       // Input file list
			using (Process process = ExecCTags(args))
			{
				process.WaitForExit();
				IEnumerable<SymbolData> results = null;
				if (process.ExitCode == 0)
				{
					results = ParseTagFile(tagsPath, SymbolData.ESymbolType.Method);
				}
			}
			System.IO.File.Delete(tagsPath);
			return results;
		}

		static public void GeneratorFromSolution2()
		{
			IEnumerable<string> files = Common.Instance.SolutionWatcher.Files.Select(file => file.Path);

			EnvDTE.StatusBar sbar = Common.Instance.DTE2.StatusBar;

			int fileCount = files.Count();
			int currentFile = 0;
			foreach (string file in files)
			{
				sbar.Progress(true, "QuickNavigation Scan solution " + currentFile + "/" + fileCount, currentFile, fileCount);
				GeneratorFromFile(file);
				++currentFile;
			}
			sbar.Progress(false);
		}

		static public void GeneratorFromSolution3()
		{
			IEnumerable<string> files = Common.Instance.SolutionWatcher.Files.Select(file => file.Path);

			EnvDTE.StatusBar sbar = Common.Instance.DTE2.StatusBar;


			int fileCount = files.Count();
			int currentFile = 0;

			files.AsParallel().WithDegreeOfParallelism(128).ForAll(file => 
			{
				if ((currentFile % 5) == 0)
				{
					sbar.Progress(true, "QuickNavigation Scan solution " + currentFile + "/" + fileCount, currentFile, fileCount);
				}
				
				GeneratorFromFile(file);
				++currentFile;
			});
			sbar.Progress(false);
		}

		static public void GeneratorFromSolution()
		{
			IEnumerable<string> files = Common.Instance.SolutionWatcher.Files.Select(file => file.Path);
			
			String dataFolder = Common.Instance.DataFolder;
			string filePath = dataFolder + "\\files";
			string tagsPath = dataFolder + "\\tags.db";
			File.WriteAllLines(filePath, files.ToArray());
			string args = "";
			args += "-V "; //Verbose : need for progress counting
			//args += "-append=yes ";
			args += "--extra=+q ";
			args += "-f \"" + tagsPath + "\" "; //Output tag file
			args += "-L \"" + filePath + "\" "; //Input file list

			using (Process process = ExecCTags(args))
			{
				int fileCount = files.Count();
				int currentFile = 0;
				EnvDTE.StatusBar sbar = Common.Instance.DTE2.StatusBar;
				bool startScan = false;
				while (!process.HasExited)
				{
					if ((currentFile % 5) == 0)
					{
						sbar.Progress(true, "QuickNavigation Scan solution " + currentFile + "/" + fileCount, currentFile, fileCount);
					}
					string line = process.StandardOutput.ReadLine();
					if (startScan)
					{
						if (line.StartsWith("OPENING ") || line.StartsWith("ignoring ") || line.StartsWith("ctags: Warning: cannot open"))
						{
							++currentFile;
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
				sbar.Progress(false);
				string output = process.StandardOutput.ReadToEnd();
			}
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
		static IEnumerable<SymbolData> ParseTagFile(string filePath, SymbolData.ESymbolType types)
		{
			List<SymbolData> symbols = new List<SymbolData>();

			using (StreamReader reader = File.OpenText(filePath))
			{
				while(!reader.EndOfStream)
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

							if ((types & SymbolData.ESymbolType.Method) != 0)
							{
								if (tagInfos[3] == "f" || tagInfos[3] == "p" || tagInfos[3] == "m")
								{
									SymbolData oSymbol = new SymbolData(tagInfos[0], fileLine, SymbolData.ESymbolType.Method);

									for (int i = 4; i < tagInfos.Length; ++i)
									{
										if (tagInfos[i].StartsWith("signature:"))
										{
											oSymbol.Parameters = tagInfos[i].Substring("signature:".Length);
										}

										if (tagInfos[i].StartsWith("class:"))
										{
											oSymbol.Class = tagInfos[i].Substring("class:".Length) + "::";
										}

										if (tagInfos[i].StartsWith("file:"))
										{
											//TODO : To implement?
										}
									}

									symbols.Add(oSymbol);
								}
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
