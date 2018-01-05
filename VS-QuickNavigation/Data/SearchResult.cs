using System;
using System.Collections.Generic;
using System.Windows.Controls;
using System.Windows.Documents;

namespace VS_QuickNavigation.Data
{
	class SearchResult
	{
		public SearchResult(string sQuery, string sSearchIn, string[] sSplitLast = null, string beforeFormatted = null, string afterFormatted = null)
		{
			mBeforeFormatted = beforeFormatted;
			mAfterFormatted = afterFormatted;

			int splitIndex = -1;
			if (sSplitLast != null)
			{
				foreach (string sSplit in sSplitLast)
				{
					splitIndex = Math.Max(splitIndex, sSearchIn.LastIndexOf(sSplit));
				}
			}

			Search(sQuery, sSearchIn, splitIndex, splitIndex);
			mSearchFormatted = null;
		}

		public SearchResult(string sQuery, string sSearchIn, int doubleScoreIndex, string beforeFormatted = null, string afterFormatted = null)
		{
			mBeforeFormatted = beforeFormatted;
			mAfterFormatted = afterFormatted;

			Search(sQuery, sSearchIn, -1, doubleScoreIndex);
			mSearchFormatted = null;
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

		void Search(string sQuery, string sSearchIn, int splitIndex = -1, int doubleScoreIndex = -1)
		{
			List<StringScore.Match> matches = new List<StringScore.Match>();

			SearchScore = StringScore.Search(sQuery, sSearchIn, matches, doubleScoreIndex);
			if (splitIndex != -1)
			{
				StringScore.FormatMatches(sSearchIn, matches, mFormatted, splitIndex + 1);
				StringScore.FormatMatches(sSearchIn, matches, mSubFormatted);
			}
			else
			{
				StringScore.FormatMatches(sSearchIn, matches, mFormatted);
				mSubFormatted.Clear();
			}
		}

		protected TextBlock mSearchFormatted;
		public TextBlock SearchFormatted
		{
			get
			{
				if(mSearchFormatted == null)
				{
					mSearchFormatted = new TextBlock();

					if (null != mBeforeFormatted)
					{
						Run text = new Run(mBeforeFormatted);
						text.Tag = "N";
						mSearchFormatted.Inlines.Add(text);
					}
					foreach (Tuple<string, bool> formatted in mFormatted)
					{
						Run text = new Run(formatted.Item1);
						text.Tag = formatted.Item2 ? "NHL" : "N";
						mSearchFormatted.Inlines.Add(text);
					}
					if (mSubFormatted.Count > 0)
					{
						mSearchFormatted.Inlines.Add(new LineBreak());
						foreach (Tuple<string, bool> formatted in mSubFormatted)
						{
							Run text = new Run(formatted.Item1);
							text.Tag = formatted.Item2 ? "SHL" : "S";
							mSearchFormatted.Inlines.Add(text);
						}
					}
					if (null != mAfterFormatted)
					{
						Run text = new Run(mAfterFormatted);
						text.Tag = "N";
						mSearchFormatted.Inlines.Add(text);
					}
				}
				
				return mSearchFormatted;
			}
		}
	}

	class SearchResultData<T> : SearchResult
	{
		public SearchResultData(T data, string sQuery, string sSearchIn, string[] sSplitLast = null, string beforeFormatted = null, string afterFormatted = null)
			: base(sQuery, sSearchIn, sSplitLast, beforeFormatted, afterFormatted)
		{
			Data = data;
		}

		public SearchResultData(T data, string sQuery, string sSearchIn, int splitIndex, string beforeFormatted = null, string afterFormatted = null)
			: base(sQuery, sSearchIn, splitIndex, beforeFormatted, afterFormatted)
		{
			Data = data;
		}

		public T Data { get; set; }
	}
}
