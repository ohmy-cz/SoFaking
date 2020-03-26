using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace net.jancerveny.sofaking.Common.Utils
{
	public static class WindowsFolder
	{
		/// <summary>
		/// Creates a Desktop.ini file, which sets up the folder picture for the parent folder of the picture
		/// </summary>
		/// <param name="folderPath"></param>
		/// <param name="imageFileName"></param>
		public static async Task SetFolderPictureAsync(string imageFileName)
		{
			if (!File.Exists(imageFileName)) throw new ArgumentException("Image does not exist." + nameof(imageFileName));

			var desktopIniFile = Path.Combine(Path.GetFullPath(imageFileName), "desktop.ini");

			var c = new StringBuilder();
			c.AppendLine("[ViewState]");
			c.AppendLine("Mode=");
			c.AppendLine("Vid=");
			c.AppendLine("FolderType=Videos");
			c.AppendLine($"Logo={imageFileName}");

			await File.WriteAllTextAsync(desktopIniFile, c.ToString());
			File.SetAttributes(desktopIniFile, File.GetAttributes(desktopIniFile) | FileAttributes.Hidden | FileAttributes.System);
		}
	}
}
