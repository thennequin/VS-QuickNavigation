using System.Collections.Generic;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace VS_QuickNavigation.Data
{
	public static class FileIcon
	{
		static Dictionary<string, BitmapSource> sExtensionsIcons = new Dictionary<string, BitmapSource>();
		static public BitmapSource GetIcon(string file)
		{
			string ext = Path.GetExtension(file);
			BitmapSource oBmpSource;
			if (sExtensionsIcons.TryGetValue(ext, out oBmpSource))
			{
				return oBmpSource;
			}
			else
			{
				BitmapSource bmpSrc = null;
				try
				{
					var sysicon = System.Drawing.Icon.ExtractAssociatedIcon(file);
					bmpSrc = System.Windows.Interop.Imaging.CreateBitmapSourceFromHIcon(
								sysicon.Handle,
								System.Windows.Int32Rect.Empty,
								System.Windows.Media.Imaging.BitmapSizeOptions.FromEmptyOptions());
					sysicon.Dispose();

				}
				catch (System.Exception) { }

				if (bmpSrc != null && bmpSrc.Width > 16)
				{
					bmpSrc = new TransformedBitmap(bmpSrc,
						new ScaleTransform(
							16.0 / bmpSrc.PixelWidth,
							16.0 / bmpSrc.PixelHeight));
				}

				sExtensionsIcons.Add(ext, bmpSrc);
				return bmpSrc;
			}
		}
	}
}
