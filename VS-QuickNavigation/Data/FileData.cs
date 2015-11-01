using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media.Imaging;
using VS_QuickNavigation.Utils;

namespace VS_QuickNavigation.Data
{
	public enum FileStatus
	{
		Recent,
		Solution,
	}
	public class FileData
	{
		public FileData(string file, string project = null)
		{
			Path = file;
			File = file.Substring(file.LastIndexOf('\\') + 1);
			if (null != project)
			{
				Projects.Add(project);
			}
			
			Icon = FileIcon.GetIcon(file);
			
			Status = FileStatus.Solution;
			LastSymbolsGeneration = DateTime.MinValue;
		}

		public BitmapSource Icon { get; private set; }
		public string File { get; private set; }
		public string Path { get; private set; }

		//Projects
		public HashSet<string> Projects { get; private set; } = new HashSet<string>();
		public string ProjectsString
		{
			get
			{
				StringBuilder sb = new StringBuilder();
				foreach (string project in Projects)
				{
					if (sb.Length > 0)
					{
						sb.Append(" | ");
					}
					sb.Append(project);
				}
				return sb.ToString();
			}
		}

		// History
		public int RecentIndex { get; set; }
		public FileStatus Status { get; set; }
		public string StatusString { get { return Status.ToString(); } }


		// Symbols
		public IEnumerable<SymbolData> Symbols { get; private set; }
		public DateTime LastSymbolsGeneration { get; private set; }

		public void GenerateSymbols()
		{
			SetSymbols(CTagsGenerator.GeneratorFromFile(Path));
		}

		public void SetSymbols(IEnumerable<SymbolData> newSymbols, DateTime? symbolsGenerationTime = null)
		{
			Symbols = newSymbols;
			LastSymbolsGeneration = symbolsGenerationTime.HasValue ? symbolsGenerationTime.Value : System.IO.File.GetLastWriteTime(Path);
			if (null != Symbols)
			{
				foreach (SymbolData symbol in Symbols)
				{
					symbol.AssociatedFile = this;
				}
			}
		}
	}
}
