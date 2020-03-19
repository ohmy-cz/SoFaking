using System.IO;

namespace SystemExtensions
{
	public static class IO
	{
		public static void MoveAllRecursively(string sourcePath, string destinationPath)
		{
			// Source path is a file
			if (File.Exists(sourcePath))
			{
				if (!Directory.Exists(destinationPath))
				{
					Directory.CreateDirectory(destinationPath);
				}

				File.Move(sourcePath, Path.Combine(destinationPath, Path.GetFileName(sourcePath)));
				return;
			}

			if (!Directory.Exists(destinationPath))
			{
				Directory.Move(sourcePath, destinationPath);
			}
			else
			{
				foreach (var subSourcePath in Directory.GetFileSystemEntries(sourcePath))
				{
					var subDestinationPath = destinationPath;
					if (Directory.Exists(subSourcePath))
					{
						subDestinationPath = Path.GetDirectoryName(subSourcePath);
					}
					MoveAllRecursively(subSourcePath, subDestinationPath);
				}
			}
		}
	}
}