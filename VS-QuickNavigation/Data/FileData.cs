using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Media.Imaging;

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
		}

		public BitmapSource Icon { get; private set; }
		public string File { get; private set; }
		public string Path { get; private set; }
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
		//public DateTime LastOpenTime { get; set; }
		public int RecentIndex { get; set; }
		public FileStatus Status { get; set; }
		public string StatusString { get { return Status.ToString(); } }

		/*public void SetRecent()
		{
			Status = FileStatus.RecentIndex;
			//LastOpenTime = DateTime.Now;
		}*/
	}
}
