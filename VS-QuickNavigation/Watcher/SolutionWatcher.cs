using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using VS_QuickNavigation.Data;
using VS_QuickNavigation.Utils;

namespace VS_QuickNavigation
{
	//IDTExtensibility2
	public class SolutionWatcher : IVsSolutionEvents, IVsSolutionLoadEvents, IDisposable
	{
		uint mSolutionCookie;
		CancellationTokenSource mToken;

		private EnvDTE.WindowEvents mWindowEvents;
		private EnvDTE.DocumentEvents mDocumentEvents;
		private EnvDTE.ProjectItemsEvents mSolutionItemsEvents;

		HistoryList<string> mFileHistory = new HistoryList<string>(50);

		Dictionary<string, FileData> mFiles = new Dictionary<string, FileData>();

		public SolutionWatcher()
		{
			if (Common.Instance.Solution != null)
			{
				Common.Instance.Solution.AdviseSolutionEvents(this, out mSolutionCookie);
			}

			mFileHistory.MaxHistory = Common.Instance.Settings.MaxFileHistory;

			mWindowEvents = Common.Instance.DTE2.Events.WindowEvents;
			mWindowEvents.WindowActivated += OnWindowActivated;
			mWindowEvents.WindowCreated += OnWindowCreated;

			mDocumentEvents = Common.Instance.DTE2.Events.DocumentEvents;
			mDocumentEvents.DocumentOpening += OnDocumentOpening;
			mDocumentEvents.DocumentOpened += OnDocumentOpened;
			mDocumentEvents.DocumentSaved += OnDocumentSaved;

			mSolutionItemsEvents = Common.Instance.DTE2.Events.SolutionItemsEvents;
			mSolutionItemsEvents.ItemAdded += OnItemAdded;
			mSolutionItemsEvents.ItemRemoved += OnItemRemoved;
			mSolutionItemsEvents.ItemRenamed += OnItemRenamed;

			RefreshOpenHistory();

			OnFilesChanged += RefreshSymbolDatabase;
		}

		public void Dispose()
		{
			if (Common.Instance.Solution != null && mSolutionCookie != 0)
			{
				Common.Instance.Solution.UnadviseSolutionEvents(mSolutionCookie);
			}
		}

		#region Window events
		void OnWindowActivated(EnvDTE.Window oWindow, EnvDTE.Window oPreviousWindow)
		{
			if (null != oWindow.Document)
			{
				mFileHistory.Push(oWindow.Document.FullName.ToLower());
				RefreshHistoryFileList();
			}
		}

		void OnWindowCreated(EnvDTE.Window oWindow)
		{
			if (null != oWindow.Document)
			{
				mFileHistory.Push(oWindow.Document.FullName.ToLower());
				RefreshHistoryFileList();
			}
		}
		#endregion

		#region Document events

		void OnDocumentOpening(string sPath, bool bReadOnly)
		{
			mFileHistory.Push(sPath.ToLower());
			RefreshHistoryFileList();
		}

		void OnDocumentOpened(EnvDTE.Document oDoc)
		{
			mFileHistory.Push(oDoc.FullName.ToLower());
			RefreshHistoryFileList();
		}

		void OnDocumentSaved(EnvDTE.Document oDoc)
		{
			FileData fileData = GetFileDataByPath(oDoc.FullName);
			if (null != fileData)
			{
				fileData.GenerateSymbols();
			}
		}
		#endregion

		#region Project Items events
		void OnItemAdded(EnvDTE.ProjectItem oProjectItem)
		{
			//mNeedRefresh = true;
			RefreshFileList();
		}

		void OnItemRemoved(EnvDTE.ProjectItem oProjectItem)
		{
			//mNeedRefresh = true;
			RefreshFileList();
		}

		void OnItemRenamed(EnvDTE.ProjectItem oProjectItem, string sOldName)
		{
			//mNeedRefresh = true;
			RefreshFileList();
		}
		#endregion

		#region IVsSolutionEvents
		public int OnAfterOpenProject(IVsHierarchy pHierarchy, int fAdded)
		{
			return VSConstants.S_OK;
		}

		public int OnQueryCloseProject(IVsHierarchy pHierarchy, int fRemoving, ref int pfCancel)
		{
			return VSConstants.S_OK;
		}

		public int OnBeforeCloseProject(IVsHierarchy pHierarchy, int fRemoved)
		{
			return VSConstants.S_OK;
		}

		public int OnAfterLoadProject(IVsHierarchy pStubHierarchy, IVsHierarchy pRealHierarchy)
		{
			return VSConstants.S_OK;
		}

		public int OnQueryUnloadProject(IVsHierarchy pRealHierarchy, ref int pfCancel)
		{
			return VSConstants.S_OK;
		}

		public int OnBeforeUnloadProject(IVsHierarchy pRealHierarchy, IVsHierarchy pStubHierarchy)
		{
			return VSConstants.S_OK;
		}

		public int OnAfterOpenSolution(object pUnkReserved, int fNewSolution)
		{
			return VSConstants.S_OK;
		}

		public int OnQueryCloseSolution(object pUnkReserved, ref int pfCancel)
		{
			return VSConstants.S_OK;
		}

		public int OnBeforeCloseSolution(object pUnkReserved)
		{
			return VSConstants.S_OK;
		}

		public int OnAfterCloseSolution(object pUnkReserved)
		{
			return VSConstants.S_OK;
		}
		#endregion

		#region IVsSolutionLoadEvents
		public int OnBeforeOpenSolution(string pszSolutionFilename)
		{
			return VSConstants.S_OK;
		}

		public int OnBeforeBackgroundSolutionLoadBegins()
		{
			return VSConstants.S_OK;
		}

		public int OnQueryBackgroundLoadProjectBatch(out bool pfShouldDelayLoadToNextIdle)
		{
			pfShouldDelayLoadToNextIdle = false;
			return VSConstants.S_OK;
		}

		public int OnBeforeLoadProjectBatch(bool fIsBackgroundIdleBatch)
		{
			return VSConstants.S_OK;
		}

		public int OnAfterLoadProjectBatch(bool fIsBackgroundIdleBatch)
		{
			return VSConstants.S_OK;
		}

		public int OnAfterBackgroundSolutionLoadComplete()
		{
			System.Diagnostics.Debug.WriteLine("OnAfterBackgroundSolutionLoadComplete");
			RefreshOpenHistory();
			RefreshFileList();
			//TestSpeed();
			return VSConstants.S_OK;
		}
		#endregion

		//public IEnumerable<>

		public delegate void FilesChanged();
		public event FilesChanged OnFilesChanged;

		bool mNeedRefresh = true;

		public void SetNeedRefresh()
		{
			mNeedRefresh = true;
		}

		//public IEnumerable<Data.FileData> Files { get; private set; } = new List<Data.FileData>();
		public IEnumerable<Data.FileData> Files
		{
			get
			{
				if (mNeedRefresh)
				{
					RefreshFileList();
				}
				return mFiles.Values;
			}
		}

		public Data.FileData GetFileDataByPath(string path)
		{
			FileData fileData;
			mFiles.TryGetValue(path.ToLower(), out fileData);
			return fileData;
		}

		public int FilesCount
		{
			get
			{
				return Files.Count();
			}
		}

		public void RefreshFileList()
		{
			if (null != mToken)
			{
				mToken.Cancel();
			}
			mToken = new CancellationTokenSource();

			RefreshSolutionFiles(mToken.Token);

			//IEnumerable<Data.FileData> newFiles = FileList.files;
			if (!mToken.Token.IsCancellationRequested)
			{
				//System.Diagnostics.Debug.WriteLine("file count " + newFiles.Count());
				//string[] exts = Common.Instance.Settings.ListedExtensions;

				RefreshHistoryFileList();

				//mFiles = mFiles.Select( pair => pair.Value );
				//Files = GetSolutionsFiles(mToken.Token).Where(fileData => exts.Any(ext => fileData.File.EndsWith(ext)));
				/*lock (Files)
				{
					Files = FileList.files.Where(fileData => exts.Any(ext => fileData.File.EndsWith(ext)));
				}*/
				mNeedRefresh = false;
				if (null != OnFilesChanged)
				{
					OnFilesChanged();
				}
			}
		}

		public void RefreshHistoryFileList()
		{
			foreach (var pair in mFiles)
			{
				int index = mFileHistory.IndexOf(pair.Key);
				if (index != -1)
				{
					pair.Value.Status = FileStatus.Recent;
					pair.Value.RecentIndex = index;
				}
				else
				{
					pair.Value.Status = FileStatus.Solution;
				}
			}
		}

		public void RefreshOpenHistory()
		{
			foreach (EnvDTE.Document doc in Common.Instance.DTE2.Documents)
			{
				mFileHistory.Push(doc.FullName.ToLower());
			}

			RefreshHistoryFileList();

			if (null != OnFilesChanged)
			{
				OnFilesChanged();
			}
		}

		public void TestSpeed()
		{
			System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch();
			watch.Start();
			var results = GetSolutionsFiles(null);
			watch.Stop();
			System.Diagnostics.Debug.WriteLine("FileList.files");
			System.Diagnostics.Debug.WriteLine("    time : " + watch.ElapsedMilliseconds);
			System.Diagnostics.Debug.WriteLine("    count : " + results.Count());

			watch.Restart();
			var resultsOld = FileList.files;
			watch.Stop();
			System.Diagnostics.Debug.WriteLine("FileList.filesOld");
			System.Diagnostics.Debug.WriteLine("    time : " + watch.ElapsedMilliseconds);
			System.Diagnostics.Debug.WriteLine("    count : " + resultsOld.Count());
		}

		void RefreshSolutionFiles(CancellationToken? cancelToken = null)
		{
			mFiles = mFiles.Where(pair => pair.Value.Status == FileStatus.Recent) // Keep RecentIndex files
				.ToDictionary(pair => pair.Key, pair => pair.Value);
			if (cancelToken.HasValue && cancelToken.Value.IsCancellationRequested)
			{
				return;
			}

			IEnumerable<FileData> solutionFiles = GetSolutionsFiles(cancelToken);

			if (cancelToken.HasValue && cancelToken.Value.IsCancellationRequested)
			{
				return;
			}

			string[] exts = Common.Instance.Settings.ListedExtensions;
			//Files = solutionFiles.Where(fileData => exts.Any(ext => fileData.File.EndsWith(ext)));

			foreach (FileData file in solutionFiles)
			{
				if (cancelToken.HasValue && cancelToken.Value.IsCancellationRequested)
				{
					return;
				}
				if (!mFiles.ContainsKey(file.Path))
				{
					mFiles[file.Path.ToLower()] = new FileData(file.Path, null);
				}

				foreach (string project in file.Projects)
				{
					mFiles[file.Path.ToLower()].Projects.Add(project);
				}
                //FileData data = mFiles[file.Path.ToLower()];
                //data.Projects.Add(file.Projects.First());
            }
        }

		public static IEnumerable<FileData> GetSolutionsFiles(CancellationToken? cancelToken = null)
		{
			System.Diagnostics.Debug.WriteLine("GetSolutionsFiles " + Common.Instance.DTE2.Solution.FileName);
            EnvDTE.Projects projects = Common.Instance.DTE2.Solution.Projects;
			if (null != projects)
			{
				List<FileData> newFiles = new List<FileData>();

				var projectsIte = projects.GetEnumerator();
				while (projectsIte.MoveNext())
				//foreach (EnvDTE.Project project in projects)
				{
					var project = (EnvDTE.Project)projectsIte.Current;
					System.Diagnostics.Debug.WriteLine("Project " + project.FileName);
					if (cancelToken.HasValue && cancelToken.Value.IsCancellationRequested)
					{
						return null;
					}
					if (null != project.ProjectItems)
					{
						FillProjectItems(newFiles, project.ProjectItems, cancelToken);
					}
				}
				return newFiles;
			}

			return null;
		}

		static void FillProjectItems(List<FileData> list, EnvDTE.ProjectItems projectItems, CancellationToken? cancelToken = null)
		{
			if (null != projectItems)
			{
				var projectItemsIte = projectItems.GetEnumerator();
				while (projectItemsIte.MoveNext())
				//foreach (EnvDTE.ProjectItem projectItem in projectItems)
				{
					var projectItem = (EnvDTE.ProjectItem)projectItemsIte.Current;

					if (cancelToken.HasValue && cancelToken.Value.IsCancellationRequested)
					{
						return;
					}
					if (Guid.Parse(projectItem.Kind) == VSConstants.GUID_ItemType_PhysicalFile)
					{
						/*string fullPath;
						if (Utils.DteHelper.GetPropertyString(projectItem.Properties, "FullPath", out fullPath))
						{

							list.Add(new FileData(fullPath, projectItem.ContainingProject.Name));
						}
						else
						{
							System.Diagnostics.Debug.WriteLine(projectItem.Name);
						}*/
						list.Add(new FileData(projectItem.FileNames[0], projectItem.ContainingProject.Name));
					}
					FillProjectItems(list, projectItem.ProjectItems, cancelToken);

					if (null != projectItem.SubProject)
					{
						FillProjectItems(list, projectItem.SubProject.ProjectItems, cancelToken);
					}
				}
			}
		}

		String SymbolsDatabasePath
		{
			get
			{
				return Common.Instance.DataFolder + "\\" + Common.Instance.DTE2.Solution.FileName.GetHashCode().ToString() + ".db";
			}
		}

		static byte[] DatabaseHeader
		{
			get
			{
				return Encoding.ASCII.GetBytes("QNDB");
			}
		}

		static ushort DatabaseVersion
		{
			get
			{
				return 2;
			}
		}

		void WriteSymbolDatabase()
		{
			using (BinaryWriter writer = new BinaryWriter(File.Open(SymbolsDatabasePath, FileMode.Create)))
			{
				writer.Write(DatabaseHeader);
				writer.Write(DatabaseVersion);
				writer.Write(mFiles.Count);

				foreach (FileData fileData in mFiles.Values)
				{
					writer.Write(fileData.Path);
					writer.Write(fileData.LastSymbolsGeneration.ToBinary());
					if (null != fileData.Symbols)
					{
						writer.Write(fileData.Symbols.Count());
						//TODO write symbols
						foreach (SymbolData symbol in fileData.Symbols)
						{
							writer.Write((byte)symbol.Type);
							writer.Write(symbol.StartLine);
							writer.Write(symbol.Symbol);
							writer.Write(symbol.Scope != null ? symbol.Scope : "");
							//TODO 
							writer.Write(symbol.Parameters != null ? symbol.Parameters : "");
						}
					}
					else
					{
						writer.Write((int)0);
					}
				}
			}
		}

		void ReadSymbolDatabase()
		{
			string dbPath = Common.Instance.DataFolder + "\\" + Common.Instance.DTE2.Solution.FileName.GetHashCode().ToString() + ".db";
			if (System.IO.File.Exists(dbPath))
			{
				using (BinaryReader reader = new BinaryReader(File.Open(SymbolsDatabasePath, FileMode.Open)))
				{
					if (reader.ReadBytes(4).SequenceEqual(DatabaseHeader) == false)
						return;

					if (reader.ReadUInt16() != DatabaseVersion)
						return;

					int iFileCount = reader.ReadInt32();

					for (int iFileIndex = 0; iFileIndex < iFileCount; ++iFileIndex)
					{
						string sFilePath = reader.ReadString();
						FileData fileData = GetFileDataByPath(sFilePath);
						DateTime oLastSymbolsGeneration = DateTime.FromBinary(reader.ReadInt64());
						int iSymbolCount = reader.ReadInt32();
						List<SymbolData> symbols = new List<SymbolData>();
						for (int i = 0; i < iSymbolCount; ++i)
						{
							SymbolData.ESymbolType eType = (SymbolData.ESymbolType)reader.ReadByte();
							int iStartLine = reader.ReadInt32();
							string sSymbol = reader.ReadString();
							string sScope = reader.ReadString();
							sScope = string.IsNullOrEmpty(sScope) ? null : sScope;
							string sParameters = reader.ReadString();
							sParameters = string.IsNullOrEmpty(sParameters) ? null : sParameters;
							if (null != fileData) // ignore invalid file
							{
								SymbolData newSymbol = new SymbolData(sSymbol, iStartLine, eType);
								newSymbol.Scope = sScope;
								newSymbol.Parameters = sParameters;
								symbols.Add(newSymbol);
							}
							//newSymbol.AssociatedFile;
						}
						if (null != fileData) // ignore invalid file
						{
							fileData.SetSymbols(symbols, oLastSymbolsGeneration);
						}
					}
				}
			}
		}

		void RefreshSymbolDatabase()
		{
			if (null == mFiles || !mFiles.Any())
			{
				return;
			}

			ReadSymbolDatabase();

			List<string> lToGenerate = new List<string>();
			foreach (FileData fileData in mFiles.Values)
			{
				if (	fileData.LastSymbolsGeneration == DateTime.MinValue 
					||	fileData.LastSymbolsGeneration < System.IO.File.GetLastWriteTime(fileData.Path)
					)
				{
					lToGenerate.Add(fileData.Path);
				}
			}

			/*EnvDTE.StatusBar sbar = Common.Instance.DTE2.StatusBar;
			Action<int, int> progressAction = (current, total) =>
			{
				if ((current % 5) == 0)
				{
					sbar.Progress(true, "QuickNavigation Scan solution " + current + "/" + total, current, total);
				}
			};
			IEnumerable<SymbolData> symbols = CTagsGenerator.GeneratorFromFiles(lToGenerate, progressAction);

			sbar.Progress(false, "QuickNavigation Analysing solution ...", 0, 0);*/

			IEnumerable<SymbolData> symbols = CTagsGenerator.GeneratorFromFilesWithProgress(lToGenerate);

			EnvDTE.StatusBar sbar = Common.Instance.DTE2.StatusBar;
			sbar.Progress(true, "Sorting symbols", 1,1);

			//Associate symbols to DileData
			symbols
				.AsParallel()
				.GroupBy(symbol => symbol.AssociatedFile)
				.ForAll( pair => pair.Key.SetSymbols(pair) );

			sbar.Progress(false);

			WriteSymbolDatabase();
		}
	}
}
