using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace VS_QuickNavigation
{
	public class SolutionWatcher : IVsSolutionEvents, IVsSolutionLoadEvents, IDisposable
	{
		uint mSolutionCookie;
		CancellationTokenSource mToken;

		public SolutionWatcher()
		{
			if (Common.Instance.Solution != null)
			{
				Common.Instance.Solution.AdviseSolutionEvents(this, out mSolutionCookie);
			}
		}

		public void Dispose()
		{
			if (Common.Instance.Solution != null && mSolutionCookie != 0)
			{
				Common.Instance.Solution.UnadviseSolutionEvents(mSolutionCookie);
			}
		}

		public int OnAfterCloseSolution(object pUnkReserved)
		{
			RefreshFileList();
			return VSConstants.S_OK;
		}

		public int OnAfterLoadProject(IVsHierarchy pStubHierarchy, IVsHierarchy pRealHierarchy)
		{
			RefreshFileList();
			return VSConstants.S_OK;
		}

		public int OnAfterOpenProject(IVsHierarchy pHierarchy, int iAdded)
		{
			RefreshFileList();
			return VSConstants.S_OK;
		}

		public int OnAfterOpenSolution(object pUnkReserved, int iNewSolution)
		{
			RefreshFileList();
			return VSConstants.S_OK;
		}

		public int OnBeforeCloseProject(IVsHierarchy pHierarchy, int iRemoved)
		{
			return VSConstants.S_OK;
		}

		public int OnBeforeCloseSolution(object pUnkReserved)
		{
			return VSConstants.S_OK;
		}

		public int OnBeforeUnloadProject(IVsHierarchy pRealHierarchy, IVsHierarchy pStubHierarchy)
		{
			return VSConstants.S_OK;
		}

		public int OnQueryCloseProject(IVsHierarchy pHierarchy, int iRemoving, ref int piCancel)
		{
			return VSConstants.S_OK;
		}

		public int OnQueryCloseSolution(object pUnkReserved, ref int piCancel)
		{
			return VSConstants.S_OK;
		}

		public int OnQueryUnloadProject(IVsHierarchy pRealHierarchy, ref int piCancel)
		{
			return VSConstants.S_OK;
		}

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
			RefreshFileList();
			return VSConstants.S_OK;
		}

		public int OnAfterBackgroundSolutionLoadComplete()
		{
			RefreshFileList();
			return VSConstants.S_OK;
		}

		//public IEnumerable<>

		public delegate void FilesChanged();
		public event FilesChanged OnFilesChanged;

		public IEnumerable<Data.FileData> Files { get; private set; } = new List<Data.FileData>();
		public int FilesCount
		{
			get
			{
				return Files.Count();
			}
		}

		public int GetCount()
		{
			return 0;
		}

		public async void RefreshFileList()
		{
			if (null != mToken)
			{
				mToken.Cancel();
			}
			mToken = new CancellationTokenSource();

			IEnumerable<Data.FileData> newFiles = FileList.files;
            if( !mToken.Token.IsCancellationRequested )
			{
				System.Diagnostics.Debug.WriteLine("file count " + newFiles.Count());
				Files = newFiles;
				if (null != OnFilesChanged)
				{
					OnFilesChanged();
				}
			}
		}
	}
}
