using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace VS_QuickNavigation.Data
{
	class SearchResult<T>
	{
		public SearchResult(T data, string sQuery, string sSearchIn, string sSplitLast = null, string beforeFormatted = null, string afterFormatted = null)
		{
			Data = data;
			mBeforeFormatted = beforeFormatted;
			mAfterFormatted = afterFormatted;
			Search(sQuery, sSearchIn, sSplitLast);
		}

		public T Data { get; set; }

		List<Tuple<string, bool>> mFormatted = new List<Tuple<string, bool>>();
		List<Tuple<string, bool>> mSubFormatted = new List<Tuple<string, bool>>();
		string mBeforeFormatted;
		string mAfterFormatted;

		public int SearchScore { get; private set; }
		
		virtual public void Search(string sQuery, string sSearchIn, string sSplitLast = null)
		{
			List<Tuple<int, int>> matches = new List<Tuple<int, int>>();
			int index = sSearchIn.LastIndexOf("\\");
			SearchScore = StringScore.Search(sQuery, sSearchIn, matches, index);
			if (index != -1)
			{
				StringScore.FormatMatches(sSearchIn, matches, mFormatted, index + 1);
				StringScore.FormatMatches(sSearchIn, matches, mSubFormatted);
			}
			else
			{
				StringScore.FormatMatches(sSearchIn, matches, mFormatted);
				mSubFormatted.Clear();
			}
		}

		public TextBlock SearchFormatted
		{
			get
			{
				TextBlock block = new TextBlock();
				SolidColorBrush matchBrush = new SolidColorBrush(Color.FromRgb(255, 255, 160));
				SolidColorBrush subBrush = new SolidColorBrush(Color.FromRgb(128, 128, 128));
				if (null != mBeforeFormatted)
				{
					block.Inlines.Add(new Run(mBeforeFormatted));
				}
				foreach (Tuple<string, bool> formatted in mFormatted)
				{
					Run text = new Run(formatted.Item1);
					if (formatted.Item2)
					{
						text.Background = matchBrush;
					}
					block.Inlines.Add(text);
				}
				if (mSubFormatted.Count > 0)
				{
					block.Inlines.Add(new LineBreak());
					foreach (Tuple<string, bool> formatted in mSubFormatted)
					{
						Run text = new Run(formatted.Item1);
						if (formatted.Item2)
						{
							text.Background = matchBrush;
						}
						text.Foreground = subBrush;
						text.FontSize = 12;
						block.Inlines.Add(text);
					}
				}
				if (null != mAfterFormatted)
				{
					block.Inlines.Add(new Run(mAfterFormatted));
				}
				return block;
			}
		}
	}
}
