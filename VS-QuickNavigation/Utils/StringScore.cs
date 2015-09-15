using System;
using System.Collections.Generic;

namespace VS_QuickNavigation
{
	internal class StringScore
	{
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