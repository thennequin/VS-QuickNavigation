using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace VS_QuickNavigation
{
	public class FileList
	{
		public class FileData
		{
			public FileData(string file, string project)
			{
				Path = file;
				File = file.Substring(file.LastIndexOf('\\') + 1);
				Project = project;
			}

			public string File { get; set; }
			public string Path { get; set; }
			public string Project { get; set; }

			private string mLastSearch;

			public string Search
			{
				get
				{
					return mLastSearch;
				}
				set
				{
					GetScore(value);
				}
			}

			public int SearchScore { get; private set; }

			public int GetScore(string sSearch)
			{
				if (sSearch != mLastSearch)
				{
					mLastSearch = sSearch;
					//SearchScore = StringScore.LevenshteinDistance(File, sSearch);
					//SearchScore = (int)(DuoVia.FuzzyStrings.DiceCoefficientExtensions.DiceCoefficient(sSearch, File) * 100);
					SearchScore = (int)(DuoVia.FuzzyStrings.DiceCoefficientExtensions.DiceCoefficient(sSearch.ToLower(), File.ToLower()) * 100);
					//SearchScore = (int)(DuoVia.FuzzyStrings.StringExtensions.FuzzyMatch(sSearch, File) * 100);
				}
				return SearchScore;
			}
		}

		static public IEnumerable<FileData> files
		{
			get
			{
				//DTE2 dte2 = (EnvDTE80.DTE2)System.Runtime.InteropServices.Marshal.GetActiveObject("VisualStudio.DTE.8.0");

				List<FileData> files = new List<FileData>();
				IEnumerable<IVsProject> projects = LoadedProjects;

				foreach (IVsProject project in projects)
				{
					string projectName = "";
					project.GetMkDocument(VSConstants.VSITEMID_ROOT, out projectName);
					if (null != projectName)
					{
						projectName = projectName.Substring(projectName.LastIndexOf('\\') + 1);
						projectName = projectName.Substring(0, projectName.LastIndexOf('.'));

						files.AddRange(AllItemsInProject(project).Select(file => new FileData(file, projectName)));
					}
				}

				return files;
			}
		}

		public static IEnumerable<IVsProject> LoadedProjects
		{
			get
			{
				IVsSolution solution = ServiceProvider.GlobalProvider.GetService(typeof(SVsSolution)) as IVsSolution;
				IEnumHierarchies enumerator = null;
				Guid guid = Guid.Empty;
				solution.GetProjectEnum((uint)__VSENUMPROJFLAGS.EPF_LOADEDINSOLUTION, ref guid, out enumerator);
				IVsHierarchy[] hierarchy = new IVsHierarchy[1] { null };
				uint fetched = 0;
				for (enumerator.Reset(); enumerator.Next(1, hierarchy, out fetched) == VSConstants.S_OK && fetched == 1; /*nothing*/)
				{
					yield return (IVsProject)hierarchy[0];
				}
			}
		}

		public static IEnumerable<string> AllItemsInProject(IVsProject project)
		{
			if (project == null)
			{
				throw new ArgumentNullException("project");
			}

			string sProjectFilename;
			project.GetMkDocument(VSConstants.VSITEMID_ROOT, out sProjectFilename);

			string projectDir = Path.GetDirectoryName(sProjectFilename);
			IVsHierarchy hierarchy = project as IVsHierarchy;

			return
				ChildrenOf(hierarchy, VSConstants.VSITEMID.Root)
				.Select(
					id =>
					{
						string name = null;
						project.GetMkDocument((uint)id, out name);
						if (name != null && name.Length > 0 && !Path.IsPathRooted(name))
						{
							name = AbsolutePathFromRelative(name, projectDir);
						}
						return name;
					})
				.Where(File.Exists);
		}

		private static List<VSConstants.VSITEMID> ChildrenOf(IVsHierarchy hierarchy, VSConstants.VSITEMID rootID)
		{
			var result = new List<VSConstants.VSITEMID>();

			for (VSConstants.VSITEMID itemID = FirstChild(hierarchy, rootID); itemID != VSConstants.VSITEMID.Nil; itemID = NextSibling(hierarchy, itemID))
			{
				result.Add(itemID);
				result.AddRange(ChildrenOf(hierarchy, itemID));
			}

			return result;
		}

		private static VSConstants.VSITEMID FirstChild(IVsHierarchy hierarchy, VSConstants.VSITEMID rootID)
		{
			object childIDObj = null;
			hierarchy.GetProperty((uint)rootID, (int)__VSHPROPID.VSHPROPID_FirstChild, out childIDObj);
			if (childIDObj != null)
			{
				return (VSConstants.VSITEMID)(int)childIDObj;
			}

			return VSConstants.VSITEMID.Nil;
		}

		private static VSConstants.VSITEMID NextSibling(IVsHierarchy hierarchy, VSConstants.VSITEMID firstID)
		{
			object siblingIDObj = null;
			hierarchy.GetProperty((uint)firstID, (int)__VSHPROPID.VSHPROPID_NextSibling, out siblingIDObj);
			if (siblingIDObj != null)
			{
				return (VSConstants.VSITEMID)(int)siblingIDObj;
			}

			return VSConstants.VSITEMID.Nil;
		}

		static public string AbsolutePathFromRelative(string relativePath, string baseFolderForDerelativization)
		{
			if (relativePath == null)
			{
				throw new ArgumentNullException("relativePath");
			}
			if (baseFolderForDerelativization == null)
			{
				throw new ArgumentNullException("baseFolderForDerelativization");
			}
			if (Path.IsPathRooted(relativePath))
			{
				throw new ArgumentException("PathNotRelative", "relativePath");
			}
			if (!Path.IsPathRooted(baseFolderForDerelativization))
			{
				throw new ArgumentException("BaseFolderMustBeRooted", "baseFolderForDerelativization");
			}

			StringBuilder result = new StringBuilder(baseFolderForDerelativization);

			if (result[result.Length - 1] != Path.DirectorySeparatorChar)
			{
				result.Append(Path.DirectorySeparatorChar);
			}

			int spanStart = 0;

			while (spanStart < relativePath.Length)
			{
				int spanStop = relativePath.IndexOf(Path.DirectorySeparatorChar, spanStart);

				if (spanStop == -1)
				{
					spanStop = relativePath.Length;
				}

				string span = relativePath.Substring(spanStart, spanStop - spanStart);

				if (span == "..")
				{
					// The result string should end with a directory separator at this point.  We
					// want to search for the one previous to that, which is why we subtract 2.
					int previousSeparator;
					if (result.Length < 2 || (previousSeparator = result.ToString().LastIndexOf(Path.DirectorySeparatorChar, result.Length - 2)) == -1)
					{
						//throw new ArgumentException(Resources.BackTooFar);
						throw new ArgumentException("BackTooFar");
					}
					result.Remove(previousSeparator + 1, result.Length - previousSeparator - 1);
				}
				else if (span != ".")
				{
					// Ignore "." because it means the current direcotry
					result.Append(span);

					if (spanStop < relativePath.Length)
					{
						result.Append(Path.DirectorySeparatorChar);
					}
				}

				spanStart = spanStop + 1;
			}

			return result.ToString();
		}
	}
}