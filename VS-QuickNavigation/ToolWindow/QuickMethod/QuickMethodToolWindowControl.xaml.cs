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
		private bool mSearchInSolution;
		private SymbolData.ESymbolType mSupportedSymbolTypes;
		private object mSymbolLocker = new object();
		private IEnumerable<SymbolData> mSymbols = null;
		private CancellationTokenSource mToken;
		private DeferredAction mDeferredRefresh;
		
		const int c_RefreshDelay = 100;

		public QuickMethodToolWindowControl(QuickMethodToolWindow oParent, bool searchInSolution, SymbolData.ESymbolType supportedSymbolTypes)
		{
			this.InitializeComponent();

			mQuickMethodToolWindow = oParent;

			mSearchInSolution = searchInSolution;
			mSupportedSymbolTypes = supportedSymbolTypes;

			mDeferredRefresh = DeferredAction.Create(RefreshResults);

			DataContext = this;

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

		public void RefreshContent()
		{
			mSymbols = null;
			mDeferredRefresh.Defer(0);
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
					mQuickMethodToolWindow.Close();
					OpenCurrentSelection();
				}
				else if (e.Key == System.Windows.Input.Key.Escape)
				{
					mQuickMethodToolWindow.Close();
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
						if (!mSearchInSolution && mSymbols == null)
						{
							mSymbols = CTagsGenerator.GeneratorFromDocument(Common.Instance.DTE2.ActiveDocument);
						}

						try
						{
							//System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
							//sw.Start();

							ParallelQuery<SymbolData> source = null;
							if (mSearchInSolution)
							{
								source = Common.Instance.SolutionWatcher.Files
									.AsParallel()
									.WithCancellation(mToken.Token)
									.Where(file => file != null && file.Symbols != null)
									.SelectMany(file => file.Symbols)
									;
							}
							else
							{
								source = mSymbols
									.AsParallel()
									.WithCancellation(mToken.Token)
									;
							}

							ParallelQuery<SearchResultData<SymbolData>> results = source
								.Where(symbol => (symbol.Type & mSupportedSymbolTypes) != 0)
								.Select(symbolData => new SearchResultData<SymbolData>(symbolData, sSearch, symbolData.Symbol, null, symbolData.Class, symbolData.Parameters));

							int total = results.Count();

							if (!string.IsNullOrWhiteSpace(sSearch))
							{
								int searchStringLen = sSearch.Length;
								results = results.Where(resultData => resultData.SearchScore > searchStringLen);
							}

							results = results
								.OrderByDescending(resultData => resultData.SearchScore)
								;

							int count = results.Count();

							//EnvDTE.FontsAndColorsItems fontsAndColor = Common.Instance.DTE2.Properties.Item("FontsAndColorsItems") as EnvDTE.FontsAndColorsItems;
							//fontsAndColor.Item("Line Number").Foreground
							//fontsAndColor.Item("Keywords").Foreground

							/*Action<IEnumerable> refreshMethod = (res) =>
							{
								results.ForAll(result => result.RefreshSearchFormatted());
							};
							Dispatcher.BeginInvoke(refreshMethod, results);*/

							Action<IEnumerable> setMethod = (res) =>
							{
								listView.ItemsSource = res;

								string title = mQuickMethodToolWindow.Title;
								int pos = title.IndexOf(" [");
								if (pos != -1)
								{
									title = title.Substring(0, pos);
								}
								mQuickMethodToolWindow.Title = title + " [" + count + "/" + total + "]";
							};
							Dispatcher.Invoke(setMethod, results.ToList());


							//sw.Stop();
							//System.Diagnostics.Debug.WriteLine("PLINQ time " + sw.Elapsed.TotalMilliseconds);
						}
						catch (Exception) { }
					}
				}
			});
			
		}

		private void OpenCurrentSelection()
		{
			int selectedIndex = listView.SelectedIndex;

			if (selectedIndex == -1)
				selectedIndex = 0;

			if (listView.Items[selectedIndex] == null)
				return;

			SearchResultData<SymbolData> symbolData = listView.Items[selectedIndex] as SearchResultData<SymbolData>;

			Common.Instance.DTE2.ItemOperations.OpenFile(symbolData.Data.AssociatedFile.Path, EnvDTE.Constants.vsViewKindTextView);
			((EnvDTE.TextSelection)Common.Instance.DTE2.ActiveDocument.Selection).GotoLine(symbolData.Data.StartLine);
		}

		private void listView_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
		{
			mQuickMethodToolWindow.Close();
			OpenCurrentSelection();
		}

		private void listView_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (!textBox.IsFocused)
				textBox.Focus();
		}
	}
}