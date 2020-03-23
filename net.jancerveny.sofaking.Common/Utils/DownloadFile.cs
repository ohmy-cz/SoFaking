using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace net.jancerveny.sofaking.Common.Utils
{
	public static class Download
	{
		public static async Task GetFile(IHttpClientFactory clientFactory, string url, string targetFile)
		{
			using (var client = clientFactory.CreateClient())
			{
				using (var result = await client.GetAsync(url))
				{
					if (result.IsSuccessStatusCode)
					{
						var content = await result.Content.ReadAsByteArrayAsync();
						await File.WriteAllBytesAsync(targetFile, content);
					}
				}
			}
		}
	}
}
