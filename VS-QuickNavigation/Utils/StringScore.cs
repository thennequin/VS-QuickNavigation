using System;
using System.Collections.Generic;

namespace VS_QuickNavigation
{
	internal class StringScore
	{
		private static int CharScore(char a, char b)
		{
			if ( a == b )
			{
				return 2;
			}else if (char.ToUpperInvariant(a) == char.ToUpperInvariant(b))
			{
				return 1;
			}
			return 0;
		}

		
		public static int Search(string query, string inText, List<Tuple<int,int>> matchIndexOut = null)
		{
			int tokenIndex = 0;
			int stringIndex = 0;
			int totalScore = 0;
			int combo = 1;
			int? currentMatch = null;
			
			while (stringIndex < inText.Length)
			{
				int charScore = CharScore(inText[stringIndex], query[tokenIndex]);
				if (charScore > 0)
				{
					if (!currentMatch.HasValue)
					{
						currentMatch = stringIndex;
					}
					
					totalScore += charScore * combo;

					//if (charScore == 2) //To test : ignore
					{
						++tokenIndex;
						++combo;

						if (tokenIndex >= query.Length)
						{
							break;
						}
					}
				}
				else
				{
					combo = 1;
					if (null != matchIndexOut && currentMatch.HasValue)
					{
						matchIndexOut.Add(new Tuple<int, int>(currentMatch.Value, stringIndex - currentMatch.Value));
						currentMatch = null;
					}
				}

				++stringIndex;
			}

			if (null != matchIndexOut && currentMatch.HasValue)
			{
				matchIndexOut.Add(new Tuple<int,int>(currentMatch.Value, stringIndex - currentMatch.Value));
			}

			return totalScore;
		}

		public static List<string> Search(
			string search,
			string words,
			double fuzzyness)
		{
			IEnumerable<string> wordList = words.Split(
				'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J',
				'K', 'L', 'M', 'N', 'O', 'P', 'Q', 'R', 'S', 'T',
				'U', 'V', 'W', 'X', 'Y', 'Z', ' ', '-', '_');
			return Search(search, wordList, fuzzyness);
		}

		public static List<string> Search(
			string word,
			IEnumerable<string> wordList,
			double fuzzyness)
		{
			List<string> foundWords = new List<string>();

			foreach (string s in wordList)
			{
				// Calculate the Levenshtein-distance:
				int levenshteinDistance =
					LevenshteinDistance(word, s);

				// Length of the longer string:
				int length = Math.Max(word.Length, s.Length);

				// Calculate the score:
				double score = 1.0 - (double)levenshteinDistance / length;

				// Match?
				if (score > fuzzyness)
					foundWords.Add(s);
			}
			return foundWords;
		}

		public static int LevenshteinDistance(string a, string b)
		{
			if (String.IsNullOrEmpty(a) || String.IsNullOrEmpty(b)) return 0;

			int lengthA = a.Length;
			int lengthB = b.Length;
			var distances = new int[lengthA + 1, lengthB + 1];
			for (int i = 0; i <= lengthA; distances[i, 0] = i++) ;
			for (int j = 0; j <= lengthB; distances[0, j] = j++) ;

			for (int i = 1; i <= lengthA; i++)
				for (int j = 1; j <= lengthB; j++)
				{
					int cost = b[j - 1] == a[i - 1] ? 0 : 1;
					distances[i, j] = Math.Min
						(
						Math.Min(distances[i - 1, j] + 1, distances[i, j - 1] + 1),
						distances[i - 1, j - 1] + cost
						);
				}
			return distances[lengthA, lengthB];
		}
	}
}