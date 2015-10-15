using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using VS_QuickNavigation.Data;

namespace VS_QuickNavigation
{
	//IDTExtensibility2
	public class SolutionWatcher : IVsSolutionEvents, IVsSolutionLoadEvents, IDisposable
	{
		uint mSolutionCookie;
		CancellationTokenSource mToken;

		private EnvDTE.WindowEvents mWindowEvents;
		private EnvDTE.DocumentEvents mDocumentEvents;

		HistoryList<string> mFileHistory = new HistoryList<string>();
		//Dictionary<string, DateTime> mFileHistory = new Dictionary<string, DateTime>();

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
			mDocumentEvents.DocumentOpened += OnDocumentOpened;

			//Common.Instance.DTE2.Events.SolutionItemsEvents.ItemAdded
			//Common.Instance.DTE2.Events.SolutionItemsEvents.ItemRemoved
			//Common.Instance.DTE2.Events.SolutionItemsEvents.ItemRenamed

			RefreshOpenHistory();
		}

		public void Dispose()
		{
			if (Common.Instance.Solution != null && mSolutionCookie != 0)
			{
				Common.Instance.Solution.UnadviseSolutionEvents(mSolutionCookie);
			}
		}

		#region Window events
		void OnWindowActivated(EnvDTE.Window window, EnvDTE.Window previousWindow)
		{
			if (null != window.Document)
			{
				mFileHistory.Push(window.Document.FullName);
				RefreshHistoryFileList();
			}
		}

		void OnWindowCreated(EnvDTE.Window window)
		{
			if (null != window.Document)
			{
				mFileHistory.Push(window.Document.FullName);
				RefreshHistoryFileList();
			}
		}
		#endregion

		#region Document events
		void OnDocumentOpened(EnvDTE.Document doc)
		{
			mFileHistory.Push(doc.FullName);
			RefreshHistoryFileList();
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
		public IEnumerable<Data.FileData> Files {
			get
			{
				if (mNeedRefresh)
				{
					RefreshFileList();
				}
				return mFiles.Values;
			}
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
				mFileHistory.Push(doc.FullName);
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
					mFiles[file.Path] = new FileData(file.Path, null);
				}

				foreach (string project in file.Projects)
				{
					mFiles[file.Path].Projects.Add(project);
				}
				//FileData data = mFiles[file.Path];
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
	}
}
