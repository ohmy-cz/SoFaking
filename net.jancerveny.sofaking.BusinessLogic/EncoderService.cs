using FFmpeg.NET;
using FFmpeg.NET.Enums;
using FFmpeg.NET.Events;
using Microsoft.Extensions.Logging;
using net.jancerveny.sofaking.BusinessLogic.Exceptions;
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
		private static string _file;
		private static bool _busy;
		public int TargetVideoBitrateKbs { get { return _configuration.OutputVideoBitrateMbits * 1024; } }
		public string CurrentFile { get { return _file; } }
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

			int? bitrate = metaData.Duration.TotalSeconds == 0 ? null : (int?)Math.Ceiling((new FileInfo(filePath).Length / 1024) / metaData.Duration.TotalSeconds);
			
			return new MediaInfo { 
				VideoCodec = metaData?.VideoData?.Format,
				VideoResolution = metaData?.VideoData?.FrameSize,
				VideoBitrateKbs = bitrate,
				AudioCodec = metaData?.AudioData?.Format
			};
		}

		public void StartTranscoding(ITranscodingJob transcodingJob, Action onCompleteInternal)
		{
			if(!File.Exists(transcodingJob.SourceFile))
			{
				throw new EncoderException($"Cannot start transcoding. File {transcodingJob.SourceFile} does not exist.");
			}

			if(_busy)
			{
				_logger.LogWarning("Cannot start another transcoding, encoder busy.");
				return;
			}

			_file = transcodingJob.SourceFile;
			Task.Run(async () =>
			{
				if (transcodingJob.Action == EncodingTargetFlags.None)
				{
					throw new Exception("No transcoding action selected");
				}

				var destinationFile = Path.Combine(transcodingJob.DestinationFolder, Path.GetFileNameWithoutExtension(_file) + ".mkv");
				var ffmpeg = new Engine(_configuration.FFMPEGBinary);
				//var inputFile = new MediaFile(_file);
				//var metaData = await ffmpeg.GetMetaDataAsync(inputFile);
				var temporaryFile = Path.Combine(_configuration.TempFolder, Path.GetFileNameWithoutExtension(_file) + ".mkv");

				// Discard all streams except those in 
				//_configuration.AudioLanguages
				// Reencode the english stream only, and add it as primary stream. Copy all other desirable audio languages from the list.
				var arguments = $"-i \"{_file}\" " +
					"-c copy " +
					$"-map 0:v -c:v {(transcodingJob.Action.HasFlag(EncodingTargetFlags.NeedsNewVideo) ? _configuration.OutputVideoCodec + $" -b:v {_configuration.OutputVideoBitrateMbits}M -vf scale=1080:-2" : "copy")} " +
						"-preset veryslow -crf 18 " +
						$"-tune {(transcodingJob.Action.HasFlag(EncodingTargetFlags.VideoIsAnimation) ? "animation" : "film")} " +
					"-map -0:a " + // Drop all audio tracks, and then add EN back below.
					$"-map 0:a:m:language:eng -c:a:0 {(transcodingJob.Action.HasFlag(EncodingTargetFlags.NeedsNewAudio) ? $"{_configuration.OutputAudioCodec} -b:a:0 {_configuration.OutputVideoBitrateMbits}M -disposition:a:0 default -map 0:a:m:language:eng -c:a:1 copy" : "copy")} " +
					"-map 0:s? " + // Subtitles
					$"\"{temporaryFile}\"";

				ffmpeg.Progress += (object sender, ConversionProgressEventArgs e) =>
				{
					_logger.LogInformation($"{e.Input?.FileInfo?.Name} -> {e.Output?.FileInfo?.Name}\nProcessed duration: {e.ProcessedDuration}\nBitrate: {e.Bitrate}\nSize: {e.SizeKb} kb");
				};
				
				ffmpeg.Error += (object sender, ConversionErrorEventArgs e) => {
					_logger.LogError($"Encoding error {e.Exception.Message}", e.Exception);
					_busy = false;
					_file = null;
					transcodingJob.OnError();
				};
				
				ffmpeg.Complete += (object sender, ConversionCompleteEventArgs e) =>
				{
					File.Move(temporaryFile, destinationFile);
					_logger.LogWarning($"Encoding complete! {_file}");
					_busy = false;
					_file = null;
					onCompleteInternal();
					transcodingJob.OnComplete();
				};

				await ffmpeg.ExecuteAsync(arguments, transcodingJob.CancellationToken);
			});
		}
	}
}
