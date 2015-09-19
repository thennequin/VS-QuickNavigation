
namespace VS_QuickNavigation
{
	using Microsoft.VisualStudio.Shell;
	using System;
	using System.Collections.Generic;
	using System.Collections.ObjectModel;
	using System.Threading.Tasks;
	using System.Windows.Controls;
	using System.Windows.Data;
	using System.Windows.Documents;
	using System.Threading;
	using System.Windows.Media;
	using System.ComponentModel;
	using System.Linq;
	using VS_QuickNavigation.Data;

	public partial class QuickFileToolWindowControl : UserControl, INotifyPropertyChanged
	{
		private QuickFileToolWindow mQuickFileToolWindow;

		private CancellationTokenSource mToken;

		public event PropertyChangedEventHandler PropertyChanged;

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

		public QuickFileToolWindowControl(QuickFileToolWindow oQuickFileToolWindow)
		{
			this.InitializeComponent();

			mQuickFileToolWindow = oQuickFileToolWindow;

			mQuickFileToolWindow.Closing += OnClosing;

			Common.Instance.SolutionWatcher.RefreshFileList();
			Common.Instance.SolutionWatcher.OnFilesChanged += OnFilesChanged;

			DataContext = this;

			RefreshList();

			textBox.Focus();
			
		}

		private void OnClosing(object sender, CancelEventArgs e)
		{
			Common.Instance.SolutionWatcher.OnFilesChanged -= OnFilesChanged;
		}

		private void OnFilesChanged()
		{
			OnPropertyChanged("FileHeader");
			RefreshList();
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
					(this.Parent as QuickFileToolWindow).Close();
				}
			}
		}

		private void textBox_TextChanged(object sender, TextChangedEventArgs e)
		{
			RefreshList();
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

		private async void RefreshList()
		{
			if ( null != mToken)
			{
				mToken.Cancel();
			}
			mToken = new CancellationTokenSource();

			//try
			{
				string sSearch = textBox.Text;
				IEnumerable<SearchResult<FileData>> results = Common.Instance.SolutionWatcher.Files
					.AsParallel()
					.WithCancellation(mToken.Token)
					.Select( fileData => new SearchResult<FileData>(fileData, sSearch, fileData.Path, "\\") )
					.Where(fileData => fileData.SearchScore > 0)
					.OrderByDescending(fileData => fileData.SearchScore)
					.Take(250)
					;

				
				if (!mToken.Token.IsCancellationRequested)
				{
					listView.ItemsSource = results;
				}
			}
			//catch (Exception)
			//{

			//}
		}

		private void listView_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
		{
			OpenCurrentSelection();
		}

		private void OpenCurrentSelection()
		{
			EnvDTE80.DTE2 dte2 = ServiceProvider.GlobalProvider.GetService(typeof(Microsoft.VisualStudio.Shell.Interop.SDTE)) as EnvDTE80.DTE2;
			int selectedIndex = listView.SelectedIndex;
			if (selectedIndex == -1) selectedIndex = 0;
			SearchResult<FileData> results = listView.Items[selectedIndex] as SearchResult<FileData>;
			dte2.ItemOperations.OpenFile(results.Data.Path);
			mQuickFileToolWindow.Close();
		}
	}
}