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

		public static int LevenshteinDistance2(string s, string t)
		{
			int m = s.Length;
			int n = t.Length;

			int[,] d = new int[m + 1, n + 1];

			for (int c = 0; c <= m; ++c)
			{
				for (int r = 0; r <= n; ++r)
				{
					d[c, r] = 0;
				}
			}

			// source prefixes can be transformed into empty string by
			// dropping all characters
			for (int i = 1; i <= m; ++i)
			{
				d[i, 0] = i;
			}

			// target prefixes can be reached from empty source prefix
			// by inserting every character
			for (int j = 1; j <= n; ++j)
			{
				d[0, j] = j;
			}

			for (int i = 1; i <= m; ++i)
			{
				for (int j = 1; j <= n; ++j)
				{
					if (s[i - 1] == t[j - 1])
					{
						d[i, j] = d[i - 1, j - 1];
					}
					else
					{
						int del = d[i - 1, j] + 1;
						int ins = d[i, j - 1] + 1;
						int sub = d[i - 1, j - 1] + 1;
						d[i, j] = Math.Min(del, Math.Min(ins, sub));
					}
				}
			}
			/*
			for j from 1 to n:
				for i from 1 to m:
					if s[i] = t[j]:
					d[i, j] := d[i - 1, j - 1]              // no operation required
					else:
					d[i, j] := minimum(d[i - 1, j] + 1,   // a deletion
										d[i, j - 1] + 1,   // an insertion
										d[i - 1, j - 1] + 1) // a substitution
			*/
			return d[m, n];
		}
	}
}