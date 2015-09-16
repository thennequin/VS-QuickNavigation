
namespace VS_QuickNavigation
{
	using Microsoft.VisualStudio.Shell;
	using System;
	using System.Collections.Generic;
	using System.Collections.ObjectModel;
	using System.Threading.Tasks;
	using System.Windows.Controls;
	using System.Windows.Data;
	using System.Linq;
	using System.Windows.Documents;

	public partial class QuickFileToolWindowControl : UserControl
	{
		public class FileData
		{
			static System.Windows.Media.Brush sBackgroundBrush;
			static FileData()
			{
				System.Windows.Media.BrushConverter converter = new System.Windows.Media.BrushConverter();
				sBackgroundBrush = (System.Windows.Media.Brush)(converter.ConvertFromString("#FFFFA0"));
			}


			public FileData(FileList.FileData data)
			{
				Data = data;
				GetScore("");
			}

			public InlineCollection FileFormatted { get; set; }

			public FileList.FileData Data { get; set; }

			private string mLastSearch;

			public string Search
			{
				get
				{
					return mLastSearch;
				}
				set
				{
					GetScore(value);
				}
			}

			public int SearchScore { get; private set; }

			public int GetScore(string sSearch)
			{
				if (sSearch != mLastSearch)
				{
					mLastSearch = sSearch;

					Bold block = new Bold();

					if (!string.IsNullOrEmpty(mLastSearch))
					{
						//SearchScore = StringScore.LevenshteinDistance(File, sSearch);
						//SearchScore = (int)(DuoVia.FuzzyStrings.DiceCoefficientExtensions.DiceCoefficient(sSearch, File) * 100);
						//SearchScore = (int)(DuoVia.FuzzyStrings.DiceCoefficientExtensions.DiceCoefficient(sSearch.ToLower(), Data.File.ToLower()) * 100);
						//SearchScore = (int)(DuoVia.FuzzyStrings.StringExtensions.FuzzyMatch(sSearch, File) * 100);
						List<Tuple<int, int>> matches = new List<Tuple<int, int>>();
						SearchScore = StringScore.Search(sSearch, Data.File, matches);

						if (matches.Count > 0)
						{
							string sFile = Data.File;

							int previousIndex = 0;
							foreach (var match in matches)
							{
								if (match.Item1 > 0)
								{
									block.Inlines.Add(new Run(sFile.Substring(previousIndex, match.Item1 - previousIndex)));
								}
								//block.Inlines.Add(new Bold(new Run(sFile.Substring(match.Item1, match.Item2))));
								Run text = new Run(sFile.Substring(match.Item1, match.Item2));
								text.Background = sBackgroundBrush;
								block.Inlines.Add(text);

								previousIndex = match.Item1 + match.Item2;
							}

							Tuple<int, int> lastMatch = matches[matches.Count - 1];
							if ((lastMatch.Item1 + lastMatch.Item2) < sFile.Length)
							{
								block.Inlines.Add(new Run(sFile.Substring(lastMatch.Item1 + lastMatch.Item2)));
							}
						}
						else
						{
							block.Inlines.Add(new Run(Data.File));
						}
					}
					else
					{
						block.Inlines.Add(new Run(Data.File));
					}

					FileFormatted = block.Inlines;
				}
				
				return SearchScore;
			}
		}

		public sealed class FileDataComparer : System.Collections.IComparer
		{
			public int Compare(object a, object b)
			{
				var lhs = (FileData)b;
				var rhs = (FileData)a;
				int lScore = lhs.GetScore(mSearchText);
				int rScore = rhs.GetScore(mSearchText);
				if (!string.IsNullOrEmpty(mSearchText))
				{
					return lScore.CompareTo(rScore);
				}

				return lhs.Data.File.CompareTo(rhs.Data.File);
			}

			public string mSearchText;
		}

		private FileDataComparer mComparer;
		private ObservableCollection<FileData> mRows;

		public QuickFileToolWindowControl()
		{
			this.InitializeComponent();

			mRows = new ObservableCollection<FileData>();

			listView.Items.Clear();
			listView.ItemsSource = mRows;
		
			ListCollectionView view = (ListCollectionView)CollectionViewSource.GetDefaultView(listView.ItemsSource);
			mComparer = new FileDataComparer();
			view.CustomSort = mComparer;

			foreach (FileList.FileData fileData in FileList.files)
			{
				mRows.Add(new FileData(fileData));
			}

			textBox.Focus();
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
					FileData file = listView.Items[selectedIndex] as FileData;
					dte2.ItemOperations.OpenFile(file.Data.Path);
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
					FileData file = listView.Items[selectedIndex] as FileData;
					dte2.ItemOperations.OpenFile(file.Data.Path);
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