using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace VS_QuickNavigation
{
	internal class StringScore
	{
		public struct Match
		{
			public Match(int index, int length)
			{
				Index = index;
				Length = length;
			}
			public int Index;
			public int Length;
		}

		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
		private static int CharScore(char a, char b)
		{
			if ( a == b )
			{
				return 3;
			}
			else if (char.ToUpperInvariant(a) == char.ToUpperInvariant(b))
			{
				if (char.IsUpper(b))
					return 2;
				else
					return 1;
			}
			return 0;
		}

		public static int Search(string query, string content, List<Match> matchIndexOut = null, int doubleScoreStart = 0)
		{
			if (string.IsNullOrEmpty(query) || string.IsNullOrEmpty(content))
			{
				return 0;
			}

			int queryIndex = query.Length - 1;
			int contentIndex = content.Length - 1;
			int totalScore = 0;
			int totalChar = 0;
			int combo = 1;
			int? currentMatch = null;
			
			while (contentIndex >= 0)
			{
				//int bestScore = 0;
				//int bestScoreLastCharPos = 0;

				int charScore = CharScore(query[queryIndex], content[contentIndex]);
				if (charScore > 0)
				{
					if (!currentMatch.HasValue)
					{
						currentMatch = contentIndex;
					}

					int multScore = contentIndex >= doubleScoreStart ? 2 : 1; // Double score
					totalScore += charScore * combo * multScore;
					if (contentIndex >= doubleScoreStart)
						++totalChar;

					//if (charScore == 2) //To test : ignore
					{
						--queryIndex;
						++combo;

						if (queryIndex < 0)
						{
							--contentIndex;
							break;
						}
					}
				}
				else
				{
					combo = 1;
					if (null != matchIndexOut && currentMatch.HasValue)
					{
						Debug.Assert((contentIndex - currentMatch.Value) < 0);
						matchIndexOut.Insert(0, new Match(contentIndex+1, currentMatch.Value - contentIndex));
						currentMatch = null;
					}
				}

				--contentIndex;
			}

			if (null != matchIndexOut && currentMatch.HasValue)
			{
				Debug.Assert((contentIndex - currentMatch.Value) < 0);
				matchIndexOut.Insert(0, new Match(contentIndex+1, currentMatch.Value - contentIndex));
			}

			float ratio = 1f + (float)totalChar / (float)(content.Length - doubleScoreStart);
			totalScore = (int)Math.Round((float)totalScore * ratio);

			return totalScore;
		}

		public static void FormatMatches(string sString, List<Match> matches, List<Tuple<string, bool>> formatted, int start = 0/*, int end = -1*/)
		{
			formatted.Clear();
			if (matches.Count > 0)
			{
				int previousIndex = start;
				foreach (var match in matches)
				{
					if (match.Index < start && (match.Index + match.Length) < start)
					{
						continue;
					}
					if (match.Index > start)
					{
						formatted.Add(Tuple.Create(sString.Substring(previousIndex, match.Index - previousIndex), false));
					}
					formatted.Add(Tuple.Create(sString.Substring(match.Index, match.Length), true));

					previousIndex = match.Index + match.Length;
				}

				Match lastMatch = matches[matches.Count - 1];
				if ((lastMatch.Index + lastMatch.Length) < sString.Length)
				{
					formatted.Add(Tuple.Create(sString.Substring(Math.Max(start,lastMatch.Index + lastMatch.Length)), false));
				}
			}
			else
			{
				formatted.Add(Tuple.Create(sString.Substring(start), false));
			}
		}
	}
}