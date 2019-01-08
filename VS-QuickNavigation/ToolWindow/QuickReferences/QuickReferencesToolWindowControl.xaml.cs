using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;
using VS_QuickNavigation.Utils;

namespace VS_QuickNavigation
{
	class Reference
	{
		public ReferenceScope Scope { get; set; }
		public WordRef Ref { get; set; }

		public Data.FileData File
		{
			get
			{
				return Scope.File;
			}
		}
	}

	class ReferenceScope
	{
		public bool IsExpanded { get; set; } = true;

		public Data.FileData File { get; set; }
		public Data.SymbolData Symbol { get; set; }
		public IEnumerable<Reference> Refs { get; set; }

		public string Name
		{
			get
			{
				return Symbol.ScopePretty + Symbol.Symbol;
			}
		}

		public int RefCount
		{
			get
			{
				return Refs.Count();
			}
		}
	}

	class ReferenceList
	{
		public bool IsExpanded { get; set; } = true;

		public Data.FileData File { get; set; }
		public IEnumerable<ReferenceScope> Scopes { get; set; }

		public int ScopeCount
		{
			get
			{
				return Scopes.Count();
			}
		}

		public int RefCount
		{
			get
			{
				return Scopes.Sum(s => s.RefCount);
			}
		}
	}

	public partial class QuickReferencesToolWindowControl : UserControl
	{
		private CancellationTokenSource mToken;
		private Task mTask;
		public QuickReferencesToolWindowControl()
		{
			InitializeComponent();
		}

		static readonly string s_oTextFormat = "Finding references for '{0}' : {1} reference(s) in {2} files";

		public void SearchReferences(string sSymbol)
		{
			if (sSymbol != null && string.IsNullOrWhiteSpace(sSymbol) == false)
			{
				if (null != mToken)
				{
					mToken.Cancel();
				}
				mToken = new CancellationTokenSource();
				CancellationToken localToken = mToken.Token;
				Task previousTask = mTask;

				ThreadSafeObservableCollection<ReferenceList> oRefLists = new ThreadSafeObservableCollection<ReferenceList>();

				mTask = Task.Run(() =>
				{
					if (null != previousTask && !previousTask.IsCompleted)
					{
						previousTask.Wait();
					}

					Dispatcher.BeginInvoke(new Action(() =>
					{
						treeView.ItemsSource = null;
					}));

					if (!localToken.IsCancellationRequested)
					{
						Dispatcher.BeginInvoke(new Action(() =>
						{
							treeView.ItemsSource = oRefLists;
							status.Content = string.Format(s_oTextFormat, sSymbol, 0, 0);
							buttonStop.IsEnabled = true;
							progressBar.Minimum = 0;
							progressBar.Maximum = 1;
							progressBar.Value = 0;
						}));

						string[] exts = Common.Instance.Settings.ListedExtensions;

						var files = Common.Instance.SolutionWatcher.Files
							.AsParallel()
							.WithCancellation(localToken)
							.Where(fileData => exts.Any(ext => fileData.File.EndsWith(ext, StringComparison.InvariantCultureIgnoreCase)))
							.OrderBy(fileData => fileData.Path);

						if (localToken.IsCancellationRequested)
							return;

						int iFile = 0;
						int iFileCount = files.Count();

						foreach (Data.FileData fileData in files)
						{
							if (!System.IO.File.Exists(fileData.Path))
								continue;

							string[] sLines = System.IO.File.ReadAllLines(fileData.Path);
							IEnumerable<WordRef> oFileRefs = CommonUtils.FindLastWord(sLines, sSymbol);
							if (oFileRefs.Any())
							{
								ReferenceList oRefList = new ReferenceList {
									File = fileData,
									Scopes = oFileRefs.GroupBy(
										r => fileData.Symbols.Where(s => s.StartLine <= r.Line && s.EndLine >= r.Line).OrderByDescending(s => s.StartLine).FirstOrDefault()
										, (key, g) => {
											ReferenceScope oScope = new ReferenceScope { Symbol = key, File = fileData };
											oScope.Refs = g.Select(r => new Reference { Scope = oScope, Ref = r });
											return oScope;
										})
								};
								oRefLists.Add(oRefList);
							}

							++iFile;

							if (localToken.IsCancellationRequested)
							{
								break;
							}

							if (iFile % 10 == 0)
							{
								string sContent = string.Format(s_oTextFormat, sSymbol, oRefLists.Sum(r => r.RefCount), oRefLists.Count);
								Dispatcher.BeginInvoke(new Action(() =>
								{
									status.Content = sContent;
									progressBar.Value = iFile;
									progressBar.Maximum = iFileCount;
								}));
							}
						}

						{
							string sContent = string.Format(s_oTextFormat, sSymbol, oRefLists.Sum(r => r.RefCount), oRefLists.Count);
							Dispatcher.BeginInvoke(new Action(()=>
							{
								status.Content = sContent;
								progressBar.Value = iFile;
								progressBar.Maximum = iFileCount;
								buttonStop.IsEnabled = false;
							}));
						}
					}
				});
			}
		}

		private void TreeView_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
		{
			if (treeView.SelectedItem is Reference)
			{
				Reference oRef = treeView.SelectedItem as Reference;
				Utils.CommonUtils.GotoLine(oRef.File.Path, oRef.Ref.Line);
			}
		}

		private void TreeView_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
		{
			if (e.Key == System.Windows.Input.Key.Enter)
			{
				if (treeView.SelectedItem is Reference)
				{
					Reference oRef = treeView.SelectedItem as Reference;
					Utils.CommonUtils.GotoLine(oRef.File.Path, oRef.Ref.Line);
				}
			}
		}

		private void ButtonStop_Click(object sender, System.Windows.RoutedEventArgs e)
		{
			if (null != mToken)
			{
				mToken.Cancel();
			}
		}
	}
}
