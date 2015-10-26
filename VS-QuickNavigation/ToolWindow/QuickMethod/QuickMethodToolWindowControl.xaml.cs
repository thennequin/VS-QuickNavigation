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
	using System.Threading;
	using System.Windows.Controls;
	using System.Windows.Data;
	using System.Windows.Documents;
	using Data;
	using System.Linq;
	using Utils;
	using System.Collections;

	/// <summary>
	/// Interaction logic for QuickMethodToolWindowControl.
	/// </summary>
	public partial class QuickMethodToolWindowControl : UserControl
	{
		private QuickMethodToolWindow mQuickMethodToolWindow;
		private object mSymbolLocker = new object();
		private List<SymbolData> mSymbols = null;
		private CancellationTokenSource mToken;

		public QuickMethodToolWindowControl(QuickMethodToolWindow oParent)
		{
			this.InitializeComponent();

			mQuickMethodToolWindow = oParent;

			//For test
			/*System.Threading.Tasks.Task.Run(() =>
			{
				System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();
				stopwatch.Start();
				CTagsGenerator.GeneratorFromSolution();
				stopwatch.Stop();
				System.Diagnostics.Debug.WriteLine("GeneratorFromSolution " + stopwatch.ElapsedMilliseconds);
				stopwatch.Restart();
				CTagsGenerator.GeneratorFromSolution3();
				stopwatch.Stop();
				System.Diagnostics.Debug.WriteLine("GeneratorFromSolution3 " + stopwatch.ElapsedMilliseconds);
			});*/
			

			DataContext = this;

			RefreshResults();

			textBox.Focus();


			// An aggregate catalog that combines multiple catalogs
			/*
			var catalog = new AggregateCatalog();

			// Adds all the parts found in the necessary assemblies
			catalog.Catalogs.Add(new AssemblyCatalog(typeof(IGlyphService).Assembly));
			catalog.Catalogs.Add(new AssemblyCatalog(typeof(SmartTagSurface).Assembly));

			// Create the CompositionContainer with the parts in the catalog
			CompositionContainer mefContainer = new CompositionContainer(catalog);

			// Fill the imports of this object
			mefContainer.ComposeParts(this);

			[Import]
			public IGlyphService GlyphService { get; set; }
			*/
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

						mSymbols.Add(new SymbolData(sSymbol, objCodeElement.StartPoint.Line, SymbolData.ESymbolType.Method));
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
					mQuickMethodToolWindow.Close();
				}
				else if (e.Key == System.Windows.Input.Key.Escape)
				{
					mQuickMethodToolWindow.Close();
				}
			}
		}

		private void textBox_TextChanged(object sender, TextChangedEventArgs e)
		{
			RefreshResults();
		}

		private void listView_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
		{
			if (e.IsDown)
			{
				if (e.Key == System.Windows.Input.Key.Return)
				{
					OpenCurrentSelection();
					mQuickMethodToolWindow.Close();
				}
				else if (e.Key == System.Windows.Input.Key.Escape)
				{
					mQuickMethodToolWindow.Close();
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

			System.Threading.Tasks.Task.Run(() =>
			{
				lock(mSymbolLocker)
				{
					if (!mToken.IsCancellationRequested)
					{
						if (mSymbols == null)
						{
							mSymbols = new List<SymbolData>();
							/*if (Common.Instance.DTE2.ActiveDocument != null && Common.Instance.DTE2.ActiveDocument.ProjectItem != null)
							{
								EnvDTE.CodeElements codeElements = Common.Instance.DTE2.ActiveDocument.ProjectItem.FileCodeModel.CodeElements;

								AnalyseCodeElements(codeElements);
							}*/

							mSymbols.AddRange(CTagsGenerator.GeneratorFromDocument(Common.Instance.DTE2.ActiveDocument));
						}

						try
						{
							IEnumerable<SearchResult<SymbolData>> results = mSymbols
							//#if !DEBUG
							.AsParallel()
							.WithCancellation(mToken.Token)
							//#endif
							.Select(symbolData => new SearchResult<SymbolData>(symbolData, sSearch, symbolData.Symbol, null, symbolData.Class, symbolData.Parameters))
								.Where(fileData => fileData.SearchScore >= 0)
								.OrderByDescending(fileData => fileData.SearchScore)
								//.Take(250)
								;


							//EnvDTE.FontsAndColorsItems fontsAndColor = Common.Instance.DTE2.Properties.Item("FontsAndColorsItems") as EnvDTE.FontsAndColorsItems;
							//fontsAndColor.Item("Line Number").Foreground
							//fontsAndColor.Item("Keywords").Foreground


							Action<IEnumerable> setMethod = (res) =>
							{
								listView.ItemsSource = res;
							};
							Dispatcher.BeginInvoke(setMethod, results);
						}
						catch (Exception e) { }
					}
					
				}
			});
			
		}

		private void OpenCurrentSelection()
		{
			int selectedIndex = listView.SelectedIndex;
			if (selectedIndex == -1) selectedIndex = 0;
			SearchResult<SymbolData> symbolData = listView.Items[selectedIndex] as SearchResult<SymbolData>;
			((EnvDTE.TextSelection)Common.Instance.DTE2.ActiveDocument.Selection).GotoLine(symbolData.Data.StartLine);
		}

		private void listView_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
		{
			OpenCurrentSelection();
			mQuickMethodToolWindow.Close();
		}
	}
}