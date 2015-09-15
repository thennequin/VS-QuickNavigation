//------------------------------------------------------------------------------
// <copyright file="QuickMethodToolWindowControl.xaml.cs" company="Company">
//     Copyright (c) Company.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

namespace VS_QuickNavigation
{
	using Microsoft.VisualStudio.Shell;
	using System;
	using System.Collections.Generic;
	using System.Collections.ObjectModel;
	using System.Windows.Controls;
	using System.Windows.Data;
	using System.Windows.Documents;

	/// <summary>
	/// Interaction logic for QuickMethodToolWindowControl.
	/// </summary>
	public partial class QuickMethodToolWindowControl : UserControl
	{
		private class SymbolData
		{
			static System.Windows.Media.Brush sBackgroundBrush;
			static SymbolData()
			{
				System.Windows.Media.BrushConverter converter = new System.Windows.Media.BrushConverter();
				sBackgroundBrush = (System.Windows.Media.Brush)(converter.ConvertFromString("#FFFFA0"));
			}
			public SymbolData(string symbol, int line)
			{
				Symbol = symbol;
				Line = line;
				GetScore("");
			}

			public string Symbol { get; set; }
			public int Line { get; set; }
			public InlineCollection FileFormatted { get; set; }

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
						//SearchScore = (int)(DuoVia.FuzzyStrings.DiceCoefficientExtensions.DiceCoefficient(sSearch.ToLower(), Symbol.ToLower()) * 100);
						//SearchScore = (int)(DuoVia.FuzzyStrings.StringExtensions.FuzzyMatch(sSearch, File) * 100);
						List<Tuple<int, int>> matches = new List<Tuple<int, int>>();
						SearchScore = StringScore.Search(sSearch, Symbol, matches);

						if (matches.Count > 0)
						{
							string sSymbol = Symbol;

							int previousIndex = 0;
							foreach (var match in matches)
							{
								if (match.Item1 > 0)
								{
									block.Inlines.Add(new Run(sSymbol.Substring(previousIndex, match.Item1 - previousIndex)));
								}
								//block.Inlines.Add(new Bold(new Run(sFile.Substring(match.Item1, match.Item2))));
								Run text = new Run(sSymbol.Substring(match.Item1, match.Item2));
								text.Background = sBackgroundBrush;
								block.Inlines.Add(text);

								previousIndex = match.Item1 + match.Item2;
							}

							Tuple<int, int> lastMatch = matches[matches.Count - 1];
							if ((lastMatch.Item1 + lastMatch.Item2) < sSymbol.Length)
							{
								block.Inlines.Add(new Run(sSymbol.Substring(lastMatch.Item1 + lastMatch.Item2)));
							}
						}
						else
						{
							block.Inlines.Add(new Run(Symbol));
						}
					}
					else
					{
						block.Inlines.Add(new Run(Symbol));
					}

					FileFormatted = block.Inlines;
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
				int lScore = lhs.GetScore(mSearchText);
				int rScore = rhs.GetScore(mSearchText);
				if (!string.IsNullOrEmpty(mSearchText))
				{
					return lScore.CompareTo(rScore);
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
						
						mRows.Add(new SymbolData(sSymbol, objCodeElement.StartPoint.Line));
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