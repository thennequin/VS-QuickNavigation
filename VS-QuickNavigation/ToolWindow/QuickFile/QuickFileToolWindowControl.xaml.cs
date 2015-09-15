//------------------------------------------------------------------------------
// <copyright file="QuickFileToolWindowControl.xaml.cs" company="Company">
//     Copyright (c) Company.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

namespace VS_QuickNavigation
{
	using Microsoft.VisualStudio.Shell;
	using System;
	using System.Collections.Generic;
	using System.Collections.ObjectModel;
	using System.Threading.Tasks;
	using System.Windows.Controls;
	using System.Windows.Data;

	/// <summary>
	/// Interaction logic for QuickFileToolWindowControl.
	/// </summary>
	public partial class QuickFileToolWindowControl : UserControl
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="QuickFileToolWindowControl"/> class.
		/// </summary>
		///
		private IEnumerable<FileList.FileData> mFiles;

		private ObservableCollection<FileList.FileData> mRows;
		private Task<IEnumerable<string>> mTask;
		private FileDataComparer mComparer;

		public sealed class FileDataComparer : System.Collections.IComparer
		{
			public int Compare(object a, object b)
			{
				var lhs = (FileList.FileData)b;
				var rhs = (FileList.FileData)a;
				if (!string.IsNullOrEmpty(mSearchText))
				{
					return lhs.GetScore(mSearchText).CompareTo(rhs.GetScore(mSearchText));
				}

				return lhs.File.CompareTo(rhs.File);
			}

			public string mSearchText;
		}

		public QuickFileToolWindowControl()
		{
			this.InitializeComponent();

			//listView.color
			//listView.UseCustomSelectionColors = true;
			//listView.HighlightBackgroundColor = Color.Lime;
			//listView.UnfocusedHighlightBackgroundColor = Color.Lime;

			mFiles = FileList.files;

			mRows = new ObservableCollection<FileList.FileData>();

			listView.Items.Clear();
			listView.ItemsSource = mRows;

			ListCollectionView view = (ListCollectionView)CollectionViewSource.GetDefaultView(listView.ItemsSource);
			//view.Filter = SearchFilter;
			mComparer = new FileDataComparer();
			view.CustomSort = mComparer;

			textBox.Focus();

			RefreshListView();
		}

		private bool SearchFilter(object item)
		{
			if (String.IsNullOrEmpty(textBox.Text))
				return true;
			else
				//return ((item as FileList.FileData).Name.IndexOf(textBox.Text, StringComparison.OrdinalIgnoreCase) >= 0);
				return ((item as FileList.FileData).File.IndexOf(textBox.Text, StringComparison.OrdinalIgnoreCase) >= 0);
		}

		private void RefreshListView()
		{
			//Async async;
			if (null != mTask)
			{
				mTask.Dispose();
			}

			//mTask = new Task<IEnumerable<string>>(
			//	delegate()
			{
				IEnumerable<FileList.FileData> files = mFiles;
				string text = textBox.Text;
				if (!string.IsNullOrEmpty(text))
				{
					//files = files.Where(file => file.File.Contains(text));
					//

					//files = files.Where(file => StringScore.LevenshteinDistance(text, file) > 0);
				}

				mRows.Clear();
				foreach (FileList.FileData file in files)
				{
					mRows.Add(file);
				}

				//if (mTask.IsCompleted)
				/*{
					listView.Items.Clear();
					foreach (string filename in filenames)
					{
						listView.Items.Add(new FileData(filename));
					}
				}*/

				//listView.DataContext = mRows;

				//return filenames;
			}
			//	);
			//mTask.Start();

			//mTask.Wait();
			//if (mTask.IsCompleted)
			//{
			//	listBox.Items.Clear();
			//	foreach (string filename in mTask.Result)
			//	{
			//		listBox.Items.Add(filename);
			//	}
			//}
		}

		private void textBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
		{
			if (e.IsUp)
			{
				if (e.Key == System.Windows.Input.Key.Up)
				{
					listView.SelectedIndex--;
					e.Handled = true;
				}
				else if (e.Key == System.Windows.Input.Key.Down)
				{
					listView.SelectedIndex++;
					e.Handled = true;
				}
			}
			else if (e.IsDown)
			{
				if (e.Key == System.Windows.Input.Key.Return)
				{
					EnvDTE80.DTE2 dte2 = ServiceProvider.GlobalProvider.GetService(typeof(Microsoft.VisualStudio.Shell.Interop.SDTE)) as EnvDTE80.DTE2;
					int selectedIndex = listView.SelectedIndex;
					if (selectedIndex == -1) selectedIndex = 0;
					FileList.FileData file = listView.Items[selectedIndex] as FileList.FileData;
					dte2.ItemOperations.OpenFile(file.Path);
					(this.Parent as QuickFileToolWindow).Close();
				}
				else if (e.Key == System.Windows.Input.Key.Escape)
				{
					(this.Parent as QuickFileToolWindow).Close();
				}
			}
		}

		private void textBox_TextChanged(object sender, TextChangedEventArgs e)
		{
			mComparer.mSearchText = textBox.Text;
			//RefreshListView();

			CollectionViewSource.GetDefaultView(listView.ItemsSource).Refresh();
		}

		private void listView_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
		{
			if (e.IsDown)
			{
				if (e.Key == System.Windows.Input.Key.Return)
				{
					EnvDTE80.DTE2 dte2 = ServiceProvider.GlobalProvider.GetService(typeof(Microsoft.VisualStudio.Shell.Interop.SDTE)) as EnvDTE80.DTE2;
					int selectedIndex = listView.SelectedIndex;
					if (selectedIndex == -1) selectedIndex = 0;
					FileList.FileData file = listView.Items[selectedIndex] as FileList.FileData;
					dte2.ItemOperations.OpenFile(file.Path);
					(this.Parent as QuickFileToolWindow).Close();
				}
				else if (e.Key == System.Windows.Input.Key.Escape)
				{
					(this.Parent as QuickFileToolWindow).Close();
				}
			}
		}
	}
}