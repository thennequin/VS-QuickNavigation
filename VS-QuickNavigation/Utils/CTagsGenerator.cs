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

				string output = process.StandardOutput.ReadToEnd();

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

							SymbolData.ESymbolType eType = SymbolData.ESymbolType.Namespace;
							bool bTypeFound = false;

							switch (kind)
							{
								case "function":
								case "method":
									eType = SymbolData.ESymbolType.Method;
									bTypeFound = true;
									break;
								case "prototype":
									eType = SymbolData.ESymbolType.MethodPrototype;
									bTypeFound = true;
									break;
								case "property":
									eType = SymbolData.ESymbolType.Property;
									bTypeFound = true;
									break;
								case "struct":
									eType = SymbolData.ESymbolType.Struct;
									bTypeFound = true;
									break;
								case "class":
									eType = SymbolData.ESymbolType.Class;
									bTypeFound = true;
									break;
								case "interface":
									eType = SymbolData.ESymbolType.Interface;
									bTypeFound = true;
									break;
								case "typedef":
									eType = SymbolData.ESymbolType.TypeDef;
									bTypeFound = true;
									break;
								case "macro":
									eType = SymbolData.ESymbolType.Macro;
									bTypeFound = true;
									break;
								case "enum":
									eType = SymbolData.ESymbolType.Enumeration;
									bTypeFound = true;
									break;
								case "enumerator":
									eType = SymbolData.ESymbolType.Enumerator;
									bTypeFound = true;
									break;
								case "member":
									eType = SymbolData.ESymbolType.Field;
									bTypeFound = true;
									break;
								case "local":
									eType = SymbolData.ESymbolType.Local;
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
