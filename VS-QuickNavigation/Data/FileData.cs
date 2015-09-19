using System.Windows.Media.Imaging;

namespace VS_QuickNavigation.Data
{
	public class FileData
	{
		public FileData(string file, string project)
		{
			Path = file;
			File = file.Substring(file.LastIndexOf('\\') + 1);
			Project = project;

			Icon = FileIcon.GetIcon(file);
		}

		public BitmapSource Icon { get; private set; }
		public string File { get; private set; }
		public string Path { get; private set; }
		public string Project { get; private set; }
	}
}
