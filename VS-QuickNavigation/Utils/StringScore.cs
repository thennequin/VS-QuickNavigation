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
				if (char.IsUpper(b) || !char.IsLetter(b))
					return 4;
				else
					return 3;
			}
			else if (char.ToUpperInvariant(a) == char.ToUpperInvariant(b))
			{
				if (char.IsUpper(b) || !char.IsLetter(b))
					return 2;
				else
					return 1;
			}
			return 0;
		}

		public static int SearchOld(string query, string content, List<Match> matchIndexOut = null, int doubleScoreStart = 0)
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

		static int PatternScore(string sPattern, string sContent, int iContentBegin, int iContentEnd, int iDoubleScoreStart, out int iOutStart, out int iOutEnd, List<Match> matchIndexOut)
		{
			int iCombo = 1;
			int iGlobalScore = 0;
			int iCurrentScore = 0;
			int iStart = sContent.Length - 1;
			int iEnd = 0;

			int iPatternLen = sPattern.Length;
			int iPatternPos = 0;

			int? currentMatch = null;

			int iCharCount = 0;

			int j;
			for (j = iContentBegin; j < iContentEnd && iPatternPos < iPatternLen; ++j)
			{
				int iCharScore = CharScore(sPattern[iPatternPos], sContent[j]);
				if (iCharScore > 0)
				{
					if (!currentMatch.HasValue)
					{
						currentMatch = j;
					}
					int iMultiplicator = (j >= iDoubleScoreStart) ? 2 : 1;
					if (j >= iDoubleScoreStart)
					{
						++iCharCount;
					}
					iCurrentScore += iCharScore * iCombo * iMultiplicator;
					++iCombo;
					iStart = Math.Min(iStart, j);
					iEnd = Math.Max(iEnd, j);
					++iPatternPos;
				}
				else
				{
					iGlobalScore += iCurrentScore;
					iCurrentScore = 0;
					iCombo = 1;

					if (null != matchIndexOut && currentMatch.HasValue)
					{
						Debug.Assert((j - currentMatch.Value) > 0);
						matchIndexOut.Add(new Match(currentMatch.Value, j - currentMatch.Value));
						currentMatch = null;
					}
				}
			}

			if (null != matchIndexOut && currentMatch.HasValue)
			{
				Debug.Assert((j - currentMatch.Value) > 0);
				matchIndexOut.Add(new Match(currentMatch.Value, j - currentMatch.Value));
			}

			iGlobalScore += iCurrentScore;

			float fLengthMatchMultiplicator = 1f + (float)iCharCount / (float)(iContentEnd - iDoubleScoreStart);
			iGlobalScore = (int)Math.Round((float)iGlobalScore * fLengthMatchMultiplicator);

			iOutStart = iStart;
			iOutEnd = iEnd;
			return iGlobalScore;
		}

		static int SearchBestPatternScore(string sPattern, string sContent, int iContentBegin, int iContentEnd, int iDoubleScoreStart, out int iBestStart, out int iBestEnd, List<Match> matchIndexOut)
		{
			iBestStart = 0;
			iBestEnd = 0;

			int iBestScore = 0;
			List<Match> bestMatches = null;
			List<Match> currentMatches = null;
			if (matchIndexOut != null)
			{
				bestMatches = new List<Match>();
				currentMatches = new List<Match>();
			}

			while (iContentBegin < iContentEnd)
			{
				int iStart;
				int iEnd;
				if (matchIndexOut != null)
				{
					currentMatches.Clear();
				}
				int iScore = PatternScore(sPattern, sContent, iContentBegin, iContentEnd, iDoubleScoreStart, out iStart, out iEnd, currentMatches);
				if (iScore > iBestScore)
				{
					iBestScore = iScore;
					iBestStart = iStart;
					iBestEnd = iEnd;

					List<Match> temp = bestMatches;
					bestMatches = currentMatches;
					currentMatches = temp;
				}

				if (iScore > 0)
				{
					iContentBegin = iStart + 1;
				}
				else
				{
					break;
				}
			}

			if (matchIndexOut != null && iBestScore > 0)
			{
				matchIndexOut.AddRange(bestMatches);
			}

			return iBestScore;
		}

		public static int Search(string sQuery, string sContent, List<Match> matchIndexOut = null, int doubleScoreStart = 0)
		{
			string[] aQueryPatterns = sQuery.Split(' ');

			int iScore = 0;
			int iContentBegin = 0;
			int iContentEnd = sContent.Length;
			for (int i = aQueryPatterns.Length - 1; i >= 0; --i)
			{
				int iBestStart, iBestEnd;
				int iBestScore = SearchBestPatternScore(aQueryPatterns[i], sContent, iContentBegin, iContentEnd, doubleScoreStart, out iBestStart, out iBestEnd, matchIndexOut);
				iScore += iBestScore;
				if (iBestScore > 0)
				{
					iContentEnd = iBestStart;
				}
			}

			if (matchIndexOut != null)
			{
				matchIndexOut.Sort((t1, t2) => t1.Index.CompareTo(t2.Index));
			}

			return iScore;
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