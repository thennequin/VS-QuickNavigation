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
		public class SymbolTypeWrapper : System.ComponentModel.INotifyPropertyChanged
		{
			public SymbolTypeWrapper(ESymbolType eType, QuickMethodToolWindowControl control)
			{
				Type = eType;
				mControl = control;
			}

			QuickMethodToolWindowControl mControl;

			public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;

			public ESymbolType Type { get; set; }

			public bool IsSelected
			{
				get
				{
					return (((int)mControl.mSupportedSymbolTypes) & (int)Type) != 0;
				}

				set
				{
					if (value)
					{
						mControl.mSupportedSymbolTypes |= Type;
					}
					else
					{
						mControl.mSupportedSymbolTypes &= ~Type;
					}
					mControl.RefreshResults();
				}
			}

			public string ImagePath
			{
				get
				{
					return Type.GetImagePath();
				}
			}

			public string Description
			{
				get
				{
					return Type.GetDescription();
				}
			}

			internal void UpdateValue()
			{
				if (PropertyChanged != null)
				{
					PropertyChanged.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs("IsSelected"));
				}
			}
		}

		private QuickMethodToolWindow mQuickMethodToolWindow;
		private bool mSearchInSolution;
		private ESymbolType mSupportedSymbolTypes;
		private ESymbolType mDefaultSupportedSymbolTypes;
		private object mSymbolLocker = new object();
		private IEnumerable<SymbolData> mSymbols = null;
		private CancellationTokenSource mToken;
		private Task mTask;
		private DeferredAction mDeferredRefresh;
		private List<SymbolTypeWrapper> mSymbolTypeWrappers;
		private bool mUpdatingSupportedSymbolTypeCheckboxes = false;

		const int c_RefreshDelay = 100;

		public IEnumerable<SymbolTypeWrapper> SuportedSymbolTypes
		{
			get
			{
				return mSymbolTypeWrappers;
			}
		}

		public QuickMethodToolWindowControl(QuickMethodToolWindow oParent, bool searchInSolution, ESymbolType supportedSymbolTypes)
		{
			this.InitializeComponent();

			mQuickMethodToolWindow = oParent;

			mSearchInSolution = searchInSolution;
			mSupportedSymbolTypes = supportedSymbolTypes;
			mDefaultSupportedSymbolTypes = supportedSymbolTypes;

			mSymbolTypeWrappers = new List<SymbolTypeWrapper>();
			foreach (ESymbolType eType in Enum.GetValues(typeof(ESymbolType)))
			{
				mSymbolTypeWrappers.Add(new SymbolTypeWrapper(eType, this));
			}

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

			if (symbolData.Data.AssociatedFile != null)
			{
				EnvDTE.Window window = Common.Instance.DTE2.ItemOperations.OpenFile(symbolData.Data.AssociatedFile.Path, EnvDTE.Constants.vsViewKindTextView);
				if (null != window)
				{
					window.Activate();
				}
			}
			else
			{
				Common.Instance.DTE2.ActiveDocument.Activate();
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

		private void buttonSymbolPopup_Click(object sender, System.Windows.RoutedEventArgs e)
		{
			SymbolPopup.IsOpen = true;
		}

		private void CheckBox_Checked(object sender, System.Windows.RoutedEventArgs e)
		{
			if (sender != null && sender is CheckBox && mUpdatingSupportedSymbolTypeCheckboxes == false)
			{
				mUpdatingSupportedSymbolTypeCheckboxes = true;
				if (System.Windows.Input.Keyboard.IsKeyDown(System.Windows.Input.Key.LeftCtrl) ||
					System.Windows.Input.Keyboard.IsKeyDown(System.Windows.Input.Key.RightCtrl))
				{
					CheckBox oCheckbox = sender as CheckBox;
					if (oCheckbox.DataContext != null && oCheckbox.DataContext is SymbolTypeWrapper)
					{
						SymbolTypeWrapper oSymbolTypeWrapper = oCheckbox.DataContext as SymbolTypeWrapper;
						mSupportedSymbolTypes = oSymbolTypeWrapper.Type;
						UpdateTypeCheckboxes();
					}
				}
				mUpdatingSupportedSymbolTypeCheckboxes = false;
			}
		}

		private void CheckBox_Unchecked(object sender, System.Windows.RoutedEventArgs e)
		{
			if (sender != null && sender is CheckBox && mUpdatingSupportedSymbolTypeCheckboxes == false)
			{
				mUpdatingSupportedSymbolTypeCheckboxes = true;
				if (System.Windows.Input.Keyboard.IsKeyDown(System.Windows.Input.Key.LeftCtrl) ||
					System.Windows.Input.Keyboard.IsKeyDown(System.Windows.Input.Key.RightCtrl))
				{
					CheckBox oCheckbox = sender as CheckBox;
					if (oCheckbox.DataContext != null && oCheckbox.DataContext is SymbolTypeWrapper)
					{
						SymbolTypeWrapper oSymbolTypeWrapper = oCheckbox.DataContext as SymbolTypeWrapper;
						mSupportedSymbolTypes = 0;

						foreach (ESymbolType eType in Enum.GetValues(typeof(ESymbolType)))
						{
							if (eType != oSymbolTypeWrapper.Type)
							{
								mSupportedSymbolTypes |= eType;
							}
						}
						UpdateTypeCheckboxes();
					}
				}
				mUpdatingSupportedSymbolTypeCheckboxes = false;
			}
		}

		private void UpdateTypeCheckboxes()
		{
			foreach (SymbolTypeWrapper oSymbolTypeWrapper in mSymbolTypeWrappers)
			{
				oSymbolTypeWrapper.UpdateValue();
			}
		}

		private void Reset_Click(object sender, System.Windows.RoutedEventArgs e)
		{
			mSupportedSymbolTypes = mDefaultSupportedSymbolTypes;
			UpdateTypeCheckboxes();
		}
	}
}
