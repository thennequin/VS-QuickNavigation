using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Data;
using VS_QuickNavigation.Data;
using VS_QuickNavigation.Utils;

namespace VS_QuickNavigation
{
    public partial class QuickFileToolWindowControl : UserControl, INotifyPropertyChanged
	{
		private QuickFileToolWindow mQuickFileToolWindow;

		private bool mHistoryOnly;

		private CancellationTokenSource mToken;

		public event PropertyChangedEventHandler PropertyChanged;

		private DeferredAction mDeferredRefresh;
		const int c_RefreshDelay = 100;

		public string FileHeader
		{
			get
			{
				return "Files (" + Common.Instance.SolutionWatcher.FilesCount + ")";
			}
		}

		protected virtual void OnPropertyChanged(string propertyName)
		{
			PropertyChangedEventHandler handler = this.PropertyChanged;
			if (handler != null)
			{
				var e = new PropertyChangedEventArgs(propertyName);
				handler(this, e);
			}
		}

		public QuickFileToolWindowControl(QuickFileToolWindow oQuickFileToolWindow, bool bHistoryOnly)
		{
			this.InitializeComponent();

			mQuickFileToolWindow = oQuickFileToolWindow;

			mQuickFileToolWindow.Closing += OnClosing;

			mHistoryOnly = bHistoryOnly;

			Common.Instance.SolutionWatcher.OnFilesChanged += OnFilesChanged;

			mDeferredRefresh = DeferredAction.Create(RefreshResults);

			DataContext = this;

			textBox.Focus();

			listView.SelectedIndex = 0;
		}

		public void RefreshContent()
		{
			mDeferredRefresh.Defer(0);
		}

		private void OnClosing(object sender, CancelEventArgs e)
		{
			Common.Instance.SolutionWatcher.OnFilesChanged -= OnFilesChanged;
		}

		private void OnFilesChanged()
		{
			if (IsVisible)
			{
				OnPropertyChanged("FileHeader");
				mDeferredRefresh.Defer(0);
			}
		}

		private void textBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
		{
			if (e.IsUp)
			{
				if (e.Key == System.Windows.Input.Key.Up)
				{
					if (listView.SelectedIndex > 0)
					{
						listView.SelectedIndex--;
						listView.ScrollIntoView(listView.SelectedItem);
					}
					e.Handled = true;
				}
				else if (e.Key == System.Windows.Input.Key.Down)
				{
					if (listView.SelectedIndex == -1)
					{
						listView.SelectedIndex = 1;
					}
					else
					{
						listView.SelectedIndex++;
					}
					listView.ScrollIntoView(listView.SelectedItem);
					e.Handled = true;
				}
			}
			else if (e.IsDown)
			{
				if (e.Key == System.Windows.Input.Key.Return)
				{
					OpenCurrentSelection();
				}
				else if (e.Key == System.Windows.Input.Key.Escape)
				{
					mQuickFileToolWindow.Close();
				}
			}
		}

		private void textBox_TextChanged(object sender, TextChangedEventArgs e)
		{
			mDeferredRefresh.Defer(c_RefreshDelay);
		}

		private void listView_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
		{
			if (e.IsDown)
			{
				if (e.Key == System.Windows.Input.Key.Return)
				{
					OpenCurrentSelection();
				}
				else if (e.Key == System.Windows.Input.Key.Escape)
				{
					mQuickFileToolWindow.Close();
				}
			}
		}

		private void RefreshResults()
		{
			if (null != mToken)
			{
				mToken.Cancel();
			}
			mToken = new CancellationTokenSource();
			string sSearch = textBox.Text;
			Task.Run(() =>
			{
				try
				{
					//Common.Instance.SolutionWatcher.SetNeedRefresh();
					ParallelQuery<FileData> source = null;
					if (mHistoryOnly || string.IsNullOrWhiteSpace(sSearch))
					{
						source = Common.Instance.SolutionWatcher.Files
							.AsParallel()
							.WithCancellation(mToken.Token)
							.Where(fileData => fileData.Status == FileStatus.Recent)
							;
					}
					else
					{
						string[] exts = Common.Instance.Settings.ListedExtensions;

						source = Common.Instance.SolutionWatcher.Files
							.AsParallel()
							.WithCancellation(mToken.Token)
							.Where(fileData => exts.Any(ext => fileData.File.EndsWith(ext)))
							;
					}

					int total = source.Count();

					IEnumerable<SearchResultData<FileData>> results = source
						.Select(fileData => new SearchResultData<FileData>(fileData, sSearch, fileData.Path, CommonUtils.ToArray<string>("\\","/")))
						;

					if (!string.IsNullOrWhiteSpace(sSearch))
					{
						int searchStringLen = sSearch.Length;
						results = results.Where(resultData => resultData.SearchScore > searchStringLen);
					}

					results = results.OrderByDescending(fileData => fileData.SearchScore) // Sort by score
						.ThenByDescending(fileData => fileData.Data.RecentIndex) // Sort by last access
						;

					int count = results.Count();

					Action<IEnumerable> setMethod = (res) =>
					{
						listView.ItemsSource = res;
						CollectionView view = (CollectionView)CollectionViewSource.GetDefaultView(listView.ItemsSource);
						view.GroupDescriptions.Clear();

						if (string.IsNullOrWhiteSpace(sSearch))
						{
							PropertyGroupDescription groupDescription = new PropertyGroupDescription("Data.StatusString");

							view.GroupDescriptions.Add(groupDescription);
						}

						string title = mQuickFileToolWindow.Title;
						int pos = title.IndexOf(" [");
						if (pos != -1)
						{
							title = title.Substring(0, pos);
						}
						mQuickFileToolWindow.Title = title + " [" + count + "/" + total + "]";
					};
					
					Dispatcher.Invoke(setMethod, results.ToList());
				}
				catch (Exception) { }
				
			});
		}

		private void listView_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
		{
			OpenCurrentSelection();
		}

		private void listView_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (!textBox.IsFocused)
				textBox.Focus();
		}

		private void OpenCurrentSelection()
		{
			mQuickFileToolWindow.Close();
			int selectedIndex = listView.SelectedIndex;

			if (selectedIndex == -1)
				selectedIndex = 0;

			if (listView.Items[selectedIndex] == null)
				return;

			SearchResultData<FileData> results = listView.Items[selectedIndex] as SearchResultData<FileData>;
			if( System.IO.File.Exists(results.Data.Path) )
			{
				EnvDTE.Window oWindow = Common.Instance.DTE2.ItemOperations.OpenFile(results.Data.Path);
				if (null != oWindow)
				{
					oWindow.Activate();
				}
			}
		}
	}
}