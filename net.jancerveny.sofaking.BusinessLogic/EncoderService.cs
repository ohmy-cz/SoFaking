using FFmpeg.NET;
using FFmpeg.NET.Enums;
using FFmpeg.NET.Events;
using Microsoft.Extensions.Logging;
using net.jancerveny.sofaking.BusinessLogic.Exceptions;
using net.jancerveny.sofaking.BusinessLogic.Interfaces;
using net.jancerveny.sofaking.BusinessLogic.Models;
using net.jancerveny.sofaking.Common.Models;
using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace net.jancerveny.sofaking.BusinessLogic
{
	public class EncoderService : IEncoderService
	{
		private readonly ILogger<EncoderService> _logger;
		private readonly EncoderConfiguration _configuration;
		private readonly SoFakingConfiguration _sofakingConfiguration;
		private static string _file;
		private static bool _busy;
		public int TargetVideoBitrateKbs { get { return _configuration.OutputVideoBitrateMbits * 1024; } }
		public string CurrentFile { get { return _file; } }

		private const char optionalFlag = '?';
		public EncoderService(ILogger<EncoderService> logger, EncoderConfiguration configuration, SoFakingConfiguration sofakingConfiguration)
		{
			_logger = logger;
			_configuration = configuration;
			_sofakingConfiguration = sofakingConfiguration;
		}

		public async Task<IMediaInfo> GetMediaInfo(string filePath)
		{
			var ffmpeg = new Engine(_configuration.FFMPEGBinary);
			var inputFile = new MediaFile(filePath);
			var metaData = await ffmpeg.GetMetaDataAsync(inputFile);

			int? bitrate = metaData.Duration.TotalSeconds == 0 ? null : (int?)(Math.Ceiling(metaData.FileInfo.Length / 1024d) / metaData.Duration.TotalSeconds);
			
			return new MediaInfo { 
				VideoCodec = metaData?.VideoData?.Format,
				VideoResolution = metaData?.VideoData?.FrameSize,
				BitrateKbs = bitrate,
				FileInfo = metaData?.FileInfo,
				AudioCodec = metaData?.AudioData?.Format // I assume this takes the default audio stream...
			};
		}

		public void StartTranscoding(ITranscodingJob transcodingJob, Action onDoneInternal, Action onSuccessInternal)
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
				var a = new StringBuilder();
				a.Append($"-i \"{_file}\" ");
				a.Append("-c copy ");

				// Video
				a.Append($"-map 0:v -c:v {(transcodingJob.Action.HasFlag(EncodingTargetFlags.NeedsNewVideo) ? _configuration.OutputVideoCodec + $" -b:v {_configuration.OutputVideoBitrateMbits}M -vf scale=1080:-2" : "copy")} ");
				a.Append("-preset veryslow -crf 18 ");
				a.Append($"-tune {(transcodingJob.Action.HasFlag(EncodingTargetFlags.VideoIsAnimation) ? "animation" : "film")} ");

				// Audio
				a.Append("-map -0:a "); // Drop all audio tracks, and then add only those we want below.
				var audioTrackCounter = 0;
				foreach(var lang in _sofakingConfiguration.AudioLanguages)
				{
					a.Append($"-map 0:a:m:language:{lang}{optionalFlag} -c:a:{audioTrackCounter} {(transcodingJob.Action.HasFlag(EncodingTargetFlags.NeedsNewAudio) ? $"{_configuration.OutputAudioCodec} -b:a:{audioTrackCounter} {_configuration.OutputAudioBitrateMbits}M" : "copy")} ");
					if(audioTrackCounter == 0)
					{
						if(transcodingJob.Action.HasFlag(EncodingTargetFlags.NeedsNewAudio))
						{
							a.Append($"-metadata:s:a:{audioTrackCounter} title=\"PS4 Compatible\" ");
						}

						a.Append($"-disposition:a:{audioTrackCounter} default ");
					}
					audioTrackCounter++;

					// The English default track  is usually in a very high quality that we can't play on a PS4, but we don't want to lose
					// So we copy it as the second track.
					if (lang == "eng" && transcodingJob.Action.HasFlag(EncodingTargetFlags.NeedsNewAudio))
					{
						a.Append($"-map 0:a:m:language:{lang}{optionalFlag} -c:a:{audioTrackCounter} copy ");
						audioTrackCounter++;
					}
				}
				
				// Subtitles
				a.Append($"-map 0:s{optionalFlag} ");
#if DEBUG
				a.Append("-t 00:01:00.0 "); // only for debug. This makes ffmpeg never return though
#endif
				a.Append($"\"{temporaryFile}\"");

				ffmpeg.Progress += (object sender, ConversionProgressEventArgs e) =>
				{
					_logger.LogInformation($"{e.Input?.FileInfo?.Name} -> {e.Output?.FileInfo?.Name}\nProcessed duration: {e.ProcessedDuration}\nBitrate: {e.Bitrate}\nSize: {e.SizeKb} kb");
				};
				
				ffmpeg.Error += (object sender, ConversionErrorEventArgs e) => {
					_logger.LogError($"Encoding error {e.Exception.Message}", e.Exception);
					onDoneInternal();
					_busy = false;
					_file = null;
					transcodingJob.OnError();
				};
				
				ffmpeg.Complete += (object sender, ConversionCompleteEventArgs e) =>
				{
					if(!Directory.Exists(transcodingJob.DestinationFolder))
					{
						Directory.CreateDirectory(transcodingJob.DestinationFolder);
					}

					File.Move(temporaryFile, destinationFile);
					_logger.LogWarning($"Encoding complete! {_file}");
					onDoneInternal();
					_busy = false;
					_file = null;
					transcodingJob.OnComplete();
					onSuccessInternal();
				};

				await ffmpeg.ExecuteAsync(a.ToString(), transcodingJob.CancellationToken);
			});
		}
	}
}
