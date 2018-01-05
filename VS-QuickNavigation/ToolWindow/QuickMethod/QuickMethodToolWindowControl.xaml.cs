using System;
using System.Collections.Generic;
using System.Threading;
using System.Windows.Controls;
using VS_QuickNavigation.Data;
using System.Linq;
using VS_QuickNavigation.Utils;
using System.Collections;
using System.Threading.Tasks;

namespace VS_QuickNavigation
{
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
		private Task mTask;
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
		}

		public void RefreshContent()
		{
			lock (mSymbolLocker)
			{
				mSymbols = null;
			}
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
			CancellationTokenSource localToken = mToken;
			Task previousTask = mTask;

			string sSearch = textBox.Text;

			mTask = Task.Run(() =>
			{
				if (previousTask != null && !previousTask.IsCompleted)
				{
					previousTask.Wait();
				}

				lock (mSymbolLocker)
				{
					if (localToken.IsCancellationRequested)
						return;

					if (!mSearchInSolution && mSymbols == null)
					{
						mSymbols = CTagsGenerator.GeneratorFromDocument(Common.Instance.DTE2.ActiveDocument);
					}

					if (localToken.IsCancellationRequested)
						return;

					try
					{
						//System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
						//sw.Start();

						ParallelQuery<SymbolData> source = null;
						if (mSearchInSolution)
						{
							source = Common.Instance.SolutionWatcher.Files
								.AsParallel()
								.WithCancellation(localToken.Token)
								.Where(file => file != null && file.Symbols != null)
								.SelectMany(file => file.Symbols)
								;
						}
						else
						{
							source = mSymbols
								.AsParallel()
								.WithCancellation(localToken.Token)
								;
						}

						ParallelQuery<SearchResultData<SymbolData>> results = source
							.Where(symbol => (symbol.Type & mSupportedSymbolTypes) != 0)
							.Select(symbolData => new SearchResultData<SymbolData>(symbolData, sSearch, symbolData.ScopePretty + symbolData.Symbol, symbolData.ScopePretty.Length, symbolData.TypeRef + " ", symbolData.Parameters));

						if (localToken.IsCancellationRequested)
							return;

						int total = results.Count();

						if (!string.IsNullOrWhiteSpace(sSearch))
						{
							int searchStringLen = sSearch.Length;
							results = results.Where(resultData => resultData.SearchScore > searchStringLen);
						}

						results = results
							.OrderByDescending(resultData => resultData.SearchScore)
							.ThenByDescending(resultData => resultData.Data.Symbol)
							;

						if (localToken.IsCancellationRequested)
							return;

						int count = results.Count();

						Action<IEnumerable> setMethod = (res) =>
						{
							listView.ItemsSource = res;

							mQuickMethodToolWindow.Title = string.Format("{0} [{1}/{2}]", mQuickMethodToolWindow.mTitle, count, total);
						};
						Dispatcher.Invoke(setMethod, results.ToList());
					}
					catch (Exception) { }
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

			EnvDTE.Window window = Common.Instance.DTE2.ItemOperations.OpenFile(symbolData.Data.AssociatedFile.Path, EnvDTE.Constants.vsViewKindTextView);
			if (null != window)
			{
				window.Activate();
			}
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