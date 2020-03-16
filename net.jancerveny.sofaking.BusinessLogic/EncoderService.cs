using FFmpeg.NET;
using FFmpeg.NET.Enums;
using FFmpeg.NET.Events;
using Microsoft.Extensions.Logging;
using net.jancerveny.sofaking.BusinessLogic.Interfaces;
using net.jancerveny.sofaking.BusinessLogic.Models;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace net.jancerveny.sofaking.BusinessLogic
{
	public class EncoderService : IEncoderService
	{
		private readonly ILogger<EncoderService> _logger;
		private readonly EncoderConfiguration _configuration;
		private static string _busyWith;
		public EncoderService(ILogger<EncoderService> logger, EncoderConfiguration configuration)
		{
			_logger = logger;
			_configuration = configuration;
		}

		public async Task<IMediaInfo> GetMediaInfo(string filePath)
		{
			var ffmpeg = new Engine(_configuration.FFMPEGBinary);
			var inputFile = new MediaFile(filePath);
			var metaData = await ffmpeg.GetMetaDataAsync(inputFile);

			return new MediaInfo { 
				VideoCodec = metaData?.VideoData?.Format,
				VideoResolution = metaData?.VideoData?.FrameSize,
				AudioCodec = metaData?.AudioData?.Format
			};
		}

		public void StartEncoding(string sourcePath, string destinationPath, EncodingTargetFlags action, Action onComplete, CancellationToken cancellationToken)
		{
			if(!string.IsNullOrWhiteSpace(_busyWith))
			{
				_logger.LogWarning("Cannot start another transcoding, encoder busy.");
				return;
			}

			_busyWith = Path.GetFileName(sourcePath);
			Task.Run(async () =>
			{
				if (action == EncodingTargetFlags.None)
				{
					throw new Exception("No transcoding action selected");
				}

				var arguments = $"-i \"{sourcePath}\" " +
					$"-c:v {(action.HasFlag(EncodingTargetFlags.Video) ? _configuration.OutputVideoCodec + $" -b:v {_configuration.OutputVideoBitrateMbits}M -s hd1080" : "copy")} " +
					//"-ss 0 -t 120 " + // Debug only - take only two minutes
					"-preset veryslow " +
					"-tune film " +
					"-crf 22 " +
					$"-c:a {(action.HasFlag(EncodingTargetFlags.Video) ? $"{_configuration.OutputAudioCodec} -b:a {_configuration.OutputVideoBitrateMbits}M" : "copy")} " +
					$"\"{destinationPath}\"";

				var ffmpeg = new Engine(_configuration.FFMPEGBinary);

				ffmpeg.Progress += (object sender, ConversionProgressEventArgs e) =>
				{
					_logger.LogInformation($"{e.Input?.FileInfo?.Name} -> {e.Output?.FileInfo?.Name}\nProcessed duration: {e.ProcessedDuration}\nBitrate: {e.Bitrate}\nSize: {e.SizeKb} kb");
				};
				
				ffmpeg.Error += (object sender, ConversionErrorEventArgs e) => {
					_logger.LogError($"Encoding error {e.Exception.ExitCode}", e.Exception.InnerException);
				};
				
				ffmpeg.Complete += (object sender, ConversionCompleteEventArgs e) =>
				{
					_logger.LogWarning($"Encoding complete! {_busyWith}");
					_busyWith = null;
					onComplete();
				};

				await ffmpeg.ExecuteAsync(arguments, cancellationToken);
			});
		}
	}
}
