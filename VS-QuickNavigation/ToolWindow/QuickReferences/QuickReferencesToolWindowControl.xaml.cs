using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Windows.Controls;
using VS_QuickNavigation.Utils;

namespace VS_QuickNavigation
{
	class Reference
	{
		public Data.FileData File { get; set; }
		public WordRef Ref { get; set; }
	}

	class ReferenceList
	{
		public Data.FileData File { get; set; }
		public IEnumerable<Reference> Refs { get; set; }

		public int RefCount
		{
			get
			{
				return Refs.Count();
			}
		}
	}

	public partial class QuickReferencesToolWindowControl : UserControl
	{
		private CancellationTokenSource mToken;
		private System.Threading.Tasks.Task mTask;
		public QuickReferencesToolWindowControl()
		{
			InitializeComponent();
		}

		static readonly string s_oTextFormat = "Finding references for '{0}' : {1} reference(s) in {2} files";

		public void SearchReferences(string sSymbol)
		{
			if (sSymbol != null)
			{
				treeView.ItemsSource = null;

				if (null != mToken)
				{
					mToken.Cancel();
				}
				mToken = new CancellationTokenSource();

				if (null != mTask && !mTask.IsCompleted)
				{
					mTask.Wait();
				}

				ThreadSafeObservableCollection<ReferenceList> oRefLists = new ThreadSafeObservableCollection<ReferenceList>();

				mTask = System.Threading.Tasks.Task.Run(() =>
				{
					if (!mToken.IsCancellationRequested)
					{
						Dispatcher.Invoke(delegate () {
							treeView.ItemsSource = oRefLists;
							status.Content = string.Format(s_oTextFormat, sSymbol, 0, 0);
							buttonStop.IsEnabled = true;
							progressBar.Minimum = 0;
							progressBar.Maximum = 1;
							progressBar.Value = 0;
						});

						//Search in all files with symbols for avoid binary files
						var files = Common.Instance.SolutionWatcher.Files.Where(f => f.Symbols != null && f.Symbols.Any()).ToList();

						int iFile = 0;
						int iFileCount = files.Count();

						foreach (Data.FileData fileData in files)
						{
							string[] sLines = System.IO.File.ReadAllLines(fileData.Path);
							IEnumerable<WordRef> oFileRefs = CommonUtils.FindLastWord(sLines, sSymbol);
							if (oFileRefs.Any())
							{
								ReferenceList oRefList = new ReferenceList { File = fileData, Refs = oFileRefs.Select(r => new Reference { File = fileData, Ref = r }) };
								oRefLists.Add(oRefList);
							}

							++iFile;

							if (mToken.IsCancellationRequested)
							{
								break;
							}

							if (iFile % 10 == 0)
							{
								string sContent = string.Format(s_oTextFormat, sSymbol, oRefLists.Sum(r => r.RefCount), oRefLists.Count);
								Dispatcher.Invoke(delegate () {
									status.Content = sContent;
									progressBar.Value = iFile;
									progressBar.Maximum = iFileCount;
								});
							}
						}

						{
							string sContent = string.Format(s_oTextFormat, sSymbol, oRefLists.Sum(r => r.RefCount), oRefLists.Count);
							Dispatcher.Invoke(delegate ()
							{
								status.Content = sContent;
								progressBar.Value = iFile;
								progressBar.Maximum = iFileCount;
								buttonStop.IsEnabled = false;
							});
						}
					}
				});
			}
		}

		private void TreeView_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
		{
			if (treeView.SelectedItem is Reference)
			{
				Reference oRef = treeView.SelectedItem as Reference;
				Utils.CommonUtils.GotoLine(oRef.File.Path, oRef.Ref.Line);
			}
		}

		private void TreeView_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
		{
			if (e.Key == System.Windows.Input.Key.Enter)
			{
				if (treeView.SelectedItem is Reference)
				{
					Reference oRef = treeView.SelectedItem as Reference;
					Utils.CommonUtils.GotoLine(oRef.File.Path, oRef.Ref.Line);
				}
			}
		}

		private void ButtonStop_Click(object sender, System.Windows.RoutedEventArgs e)
		{
			if (null != mToken)
			{
				mToken.Cancel();
			}
		}
	}
}
