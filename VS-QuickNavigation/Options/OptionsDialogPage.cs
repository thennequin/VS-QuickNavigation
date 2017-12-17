using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell;
using System.ComponentModel.Design;

namespace VS_QuickNavigation.Options
{
	[ClassInterface(ClassInterfaceType.AutoDual)]
	[ComVisible(true)]
	[Guid("8D007562-FE0B-40BF-B305-4433FEA8F773")]
	class OptionsDialogPage : DialogPage
	{
		public const String STRING_COLLECTION_EDITOR = "System.Windows.Forms.Design.StringCollectionEditor, System.Design, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";

		public override void SaveSettingsToStorage()
		{
			//base.SaveSettingsToStorage();

			Common.Instance.Settings.ListedExtensionsString = ListedExtensions;
			Common.Instance.Settings.SaveSettingsToStorage();
		}

		public override void LoadSettingsFromStorage()
		{
			//base.LoadSettingsFromStorage();

			Common.Instance.Settings.LoadSettingsFromStorage();
			ListedExtensions = Common.Instance.Settings.ListedExtensionsString;
		}

		// Text Editor Extensions
		[LocDisplayName("Listed extensions")]
		[Description("List of file extensions to display in QuickFile\nOne per line")]
		[Category("QuickFile")]
		[EditorAttribute(typeof(MultilineStringEditor), typeof(System.Drawing.Design.UITypeEditor))]
		public string ListedExtensions { get; set; }
	}
}
