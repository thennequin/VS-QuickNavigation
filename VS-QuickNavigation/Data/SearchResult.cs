using System;
using System.Collections.Generic;
using System.Windows.Controls;
using System.Windows.Documents;

namespace VS_QuickNavigation.Data
{
	class SearchResult
	{
		public SearchResult(string sQuery, string sSearchIn, string sSplitLast = null, string beforeFormatted = null, string afterFormatted = null)
		{
			mBeforeFormatted = beforeFormatted;
			mAfterFormatted = afterFormatted;
			Search(sQuery, sSearchIn, sSplitLast);
		}

		List<Tuple<string, bool>> mFormatted = new List<Tuple<string, bool>>();
		List<Tuple<string, bool>> mSubFormatted = new List<Tuple<string, bool>>();
		string mBeforeFormatted;
		string mAfterFormatted;

		public int SearchScore { get; private set; }

		public String SearchScoreString
		{
			get
			{
				if (SearchScore > 0)
				{
					return SearchScore.ToString();
				}
				return "";
			}
		}

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

				if (null != mBeforeFormatted)
				{
					Run text = new Run(mBeforeFormatted);
					text.Tag = "N";
					block.Inlines.Add(text);
				}
				foreach (Tuple<string, bool> formatted in mFormatted)
				{
					Run text = new Run(formatted.Item1);
					text.Tag = formatted.Item2 ? "NHL" : "N";
					block.Inlines.Add(text);
				}
				if (mSubFormatted.Count > 0)
				{
					block.Inlines.Add(new LineBreak());
					foreach (Tuple<string, bool> formatted in mSubFormatted)
					{
						Run text = new Run(formatted.Item1);
						text.Tag = formatted.Item2 ? "SHL" : "S";
						block.Inlines.Add(text);
					}
				}
				if (null != mAfterFormatted)
				{
					Run text = new Run(mAfterFormatted);
					text.Tag = "N";
					block.Inlines.Add(text);
				}
				return block;
			}
		}
	}

	class SearchResultData<T> : SearchResult
	{
		public SearchResultData(T data, string sQuery, string sSearchIn, string sSplitLast = null, string beforeFormatted = null, string afterFormatted = null)
			: base(sQuery, sSearchIn, sSplitLast, beforeFormatted, afterFormatted)
		{
			Data = data;
		}

		public T Data { get; set; }
	}
}
