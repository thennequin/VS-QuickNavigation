//------------------------------------------------------------------------------
// <copyright file="QuickMethodToolWindowControl.xaml.cs" company="Company">
//     Copyright (c) Company.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

namespace VS_QuickNavigation
{
	using Microsoft.VisualStudio.Shell;
	using System.Collections.ObjectModel;
	using System.Windows.Controls;
	using System.Windows.Data;

	/// <summary>
	/// Interaction logic for QuickMethodToolWindowControl.
	/// </summary>
	public partial class QuickMethodToolWindowControl : UserControl
	{
		private class SymbolData
		{
			public string Symbol { get; set; }
			public int Line { get; set; }

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
					//SearchScore = StringScore.LevenshteinDistance(File, sSearch);
					//SearchScore = (int)(DuoVia.FuzzyStrings.DiceCoefficientExtensions.DiceCoefficient(sSearch, File) * 100);
					//SearchScore = (int)(DuoVia.FuzzyStrings.DiceCoefficientExtensions.DiceCoefficient(sSearch.ToLower(), Symbol.ToLower()) * 100);
					//SearchScore = (int)(DuoVia.FuzzyStrings.StringExtensions.FuzzyMatch(sSearch, File) * 100);
					SearchScore = StringScore.Search(sSearch, Symbol);
				}
				return SearchScore;
			}
		}

		public sealed class SymbolDataComparer : System.Collections.IComparer
		{
			public int Compare(object a, object b)
			{
				var lhs = (SymbolData)b;
				var rhs = (SymbolData)a;
				if (!string.IsNullOrEmpty(mSearchText))
				{
					return lhs.GetScore(mSearchText).CompareTo(rhs.GetScore(mSearchText));
				}

				return lhs.Symbol.CompareTo(rhs.Symbol);
			}

			public string mSearchText;
		}

		private SymbolDataComparer mComparer;

		private ObservableCollection<SymbolData> mRows;

		public QuickMethodToolWindowControl()
		{
			this.InitializeComponent();

			mRows = new ObservableCollection<SymbolData>();

			listView.Items.Clear();
			listView.ItemsSource = mRows;

			ListCollectionView view = (ListCollectionView)CollectionViewSource.GetDefaultView(listView.ItemsSource);
			mComparer = new SymbolDataComparer();
			view.CustomSort = mComparer;

			EnvDTE80.DTE2 dte2 = ServiceProvider.GlobalProvider.GetService(typeof(Microsoft.VisualStudio.Shell.Interop.SDTE)) as EnvDTE80.DTE2;
			EnvDTE.CodeElements codeElements = dte2.ActiveDocument.ProjectItem.FileCodeModel.CodeElements;

			AnalyseCodeElements(codeElements);
			
			textBox.Focus();
		}

		private void AnalyseCodeElements(EnvDTE.CodeElements codeElements)
		{
			if (null != codeElements)
			{
				foreach (EnvDTE.CodeElement objCodeElement in codeElements)
				{
					if (objCodeElement is EnvDTE.CodeFunction)
					{
						EnvDTE.CodeFunction objCodeFunction = objCodeElement as EnvDTE.CodeFunction;
						SymbolData oSymbol = new SymbolData();
						string sSymbol = objCodeElement.FullName;
						sSymbol += "(";

						if (null != objCodeFunction.Parameters && objCodeFunction.Parameters.Count > 0)
						{
							sSymbol += " ";
							bool bFirstMember = true;
							foreach (EnvDTE.CodeParameter objCodeParameter in objCodeFunction.Parameters)
							{
								if (bFirstMember)
								{
									bFirstMember = false;
								}
								else
								{
									sSymbol += ", ";
								}
								sSymbol += objCodeParameter.Type.AsString;
								sSymbol += " ";
								sSymbol += objCodeParameter.Name;
							}
							sSymbol += " ";
						}

						sSymbol += ")";
						oSymbol.Symbol = sSymbol;
						oSymbol.Line = objCodeElement.StartPoint.Line;
						mRows.Add(oSymbol);
					}
					else
					{
						AnalyseCodeElements(GetCodeElementMembers(objCodeElement));
					}
				}
			}
		}

		private EnvDTE.CodeElements GetCodeElementMembers(EnvDTE.CodeElement objCodeElement)
		{
			EnvDTE.CodeElements colCodeElements = null;

			if (objCodeElement is EnvDTE.CodeNamespace)
			{
				colCodeElements = ((EnvDTE.CodeNamespace)objCodeElement).Members;
			}
			else if (objCodeElement is EnvDTE.CodeType)
			{
				colCodeElements = ((EnvDTE.CodeType)objCodeElement).Members;
			}
			else if (objCodeElement is EnvDTE.CodeFunction)
			{
				colCodeElements = ((EnvDTE.CodeFunction)objCodeElement).Parameters;
			}
			else if (objCodeElement is EnvDTE.CodeClass)
			{
				colCodeElements = ((EnvDTE.CodeClass)objCodeElement).Members;
			}
			return colCodeElements;
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
					SymbolData symbolData = listView.Items[selectedIndex] as SymbolData;
					//dte2.ItemOperations.OpenFile(file.Path);
					((EnvDTE.TextSelection)dte2.ActiveDocument.Selection).GotoLine(symbolData.Line);

					(this.Parent as QuickMethodToolWindow).Close();
				}
				else if (e.Key == System.Windows.Input.Key.Escape)
				{
					(this.Parent as QuickMethodToolWindow).Close();
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
					SymbolData symbolData = listView.Items[selectedIndex] as SymbolData;
					//dte2.ItemOperations.OpenFile(file.Path);
					((EnvDTE.TextSelection)dte2.ActiveDocument.Selection).GotoLine(symbolData.Line);

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