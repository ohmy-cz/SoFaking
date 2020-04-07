using FFmpeg.NET;
using FFmpeg.NET.Events;
using Microsoft.Extensions.Logging;
using net.jancerveny.sofaking.BusinessLogic.Exceptions;
using net.jancerveny.sofaking.BusinessLogic.Interfaces;
using net.jancerveny.sofaking.BusinessLogic.Models;
using net.jancerveny.sofaking.Common.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace net.jancerveny.sofaking.BusinessLogic
{
	public class FFMPEGEncoderService : IEncoderService
	{
		public string CurrentFile { get { return _transcodingJob.SourceFile; } }
		public bool Busy { get { return _busy; } }
		public DateTime? TranscodingStarted { get; private set; }
		public event EventHandler<EventArgs> OnStart;
		public event EventHandler<EventArgs> OnCancelled;
		public event EventHandler<EncodingProgressEventArgs> OnProgress;
		public event EventHandler<EncodingSuccessEventArgs> OnSuccess;
		public event EventHandler<EncodingErrorEventArgs> OnError;
		public string _stalledFileCandidate;
		private readonly ILogger<FFMPEGEncoderService> _logger;
		private readonly EncoderConfiguration _configuration;
		private readonly SoFakingConfiguration _sofakingConfiguration;
		private static bool _busy;
		private string _tempFile;
		private CancellationTokenSource _cancellationTokenSource;
		private ITranscodingJob _transcodingJob;
		private string _shFile;
		private string _batFile;
		private bool disposed = false;
		private bool keepFiles = false;
		private const char optionalFlag = '?';
		private static long? _lastTempFileSize;
		private static readonly TimeSpan stalledCheckInterval = new TimeSpan(0, 5, 0);

		public FFMPEGEncoderService(ILogger<FFMPEGEncoderService> logger, EncoderConfiguration configuration, SoFakingConfiguration sofakingConfiguration)
		{
			_logger = logger;
			_configuration = configuration;
			_sofakingConfiguration = sofakingConfiguration;

			SetCancellationToken();
		}

		public async Task<IMediaInfo> GetMediaInfo(string filePath)
		{
			FFMPEGFileModel fm;
			try
			{
				fm = await GetFileModelAsync(filePath);
			} catch(Exception ex)
			{
				_logger.LogError($"Failed constructing FFMPEG File model: {ex.Message}", ex);
				throw;
			}

			if(fm == null)
			{
				throw new Exception($"Could not get FFMPEG file model: {nameof(fm)} was null.");
			}

			var fi = new FileInfo(filePath);
			var videoStream = fm.MainVideoStream;
			var mainAudioStream = fm.MainAudioStream(_sofakingConfiguration.AudioLanguages);

			if(!string.IsNullOrWhiteSpace(mainAudioStream?.Language) && !_sofakingConfiguration.AudioLanguages.Contains(mainAudioStream.Language))
			{
				throw new Exception($"The main audio stream's language {mainAudioStream.Language} does not equal {_sofakingConfiguration.AudioLanguages[0]}.");
			}

			if (fm.Duration == null)
			{
				throw new Exception($"No main duration found in {filePath}");
			}

			int? AVBitrateKbs = fm.Duration.TotalSeconds == 0 ? null : (int?)(Math.Ceiling((fi.Length * 8) / 1024d) / fm.Duration.TotalSeconds);

			return new MediaInfo {
				VideoCodec = videoStream.StreamCodec,
				HorizontalVideoResolution = videoStream.HorizontalResolution,
				AVBitrateKbs = AVBitrateKbs,
				FileInfo = fi,
				Duration = fm.Duration,
				AudioCodec = mainAudioStream.StreamCodec
			};
		}

		public async Task StartTranscodingAsync(ITranscodingJob transcodingJob, CancellationToken cancellationToken = default)
		{
			if (!File.Exists(transcodingJob.SourceFile))
			{
				throw new EncoderException($"Cannot start transcoding. File {transcodingJob.SourceFile} does not exist.");
			}

			if(Busy)
			{
				_logger.LogWarning($"Cannot start another transcoding, encoder busy with {CurrentFile}. {transcodingJob.SourceFile} rejected.");
				return;
			}

			cancellationToken.Register(() => {
				_logger.LogDebug("Encoder cancelled from the outside");
				Kill();
			});

			_transcodingJob = transcodingJob;
			if (_transcodingJob.Action == EncodingTargetFlags.None)
			{
				throw new Exception("No transcoding action selected");
			}

			_busy = true;

			_logger.LogDebug("Getting FFMPEG File Model");
			var fm = await GetFileModelAsync(CurrentFile);
			if (fm == null)
			{
				throw new Exception($"Could not get FFMPEG file model: {nameof(fm)} was null.");
			}

			var mainAudioStream = fm.MainAudioStream(_sofakingConfiguration.AudioLanguages);
			var ffmpeg = new Engine(_configuration.FFMPEGBinary);

			_logger.LogDebug("Preparing files");
			_tempFile = Path.Combine(_sofakingConfiguration.TempFolder, Path.GetFileNameWithoutExtension(CurrentFile) + ".mkv");
			_shFile = Path.Combine(_sofakingConfiguration.TempFolder, Path.GetFileNameWithoutExtension(CurrentFile) + ".sh");
			_batFile = Path.Combine(_sofakingConfiguration.TempFolder, Path.GetFileNameWithoutExtension(CurrentFile) + ".bat");

			// Discard all streams except those in _configuration.AudioLanguages
			// Reencode the english stream only, and add it as primary stream. Copy all other desirable audio languages from the list.
			_logger.LogDebug("Constructing the encoder command");
			var a = new StringBuilder();
			a.Append($"-i \"{CurrentFile}\" ");
			a.Append("-c copy ");

			// Copy metadata
			a.Append("-map_metadata 0 ");

			// Video
			a.Append($"-map 0:v:0 -c:v ");
			if(_transcodingJob.Action.HasFlag(EncodingTargetFlags.NeedsNewVideo))
			{
				// Resize?
				a.Append(_configuration.OutputVideoCodec + " " + (fm.MainVideoStream.HorizontalResolution > _sofakingConfiguration.MaxHorizontalVideoResolution ? $"-vf scale={_sofakingConfiguration.MaxHorizontalVideoResolution}:-2:flags=lanczos+accurate_rnd " : string.Empty));

				// Make sure we have the right bitrate, important for PS4 compatibility
				a.Append($"-b:v {_configuration.OutputVideoBitrateMbits}M ");
				a.Append($"-maxrate {_configuration.OutputVideoBitrateMbits}M ");
				a.Append($"-bufsize {(int)Math.Ceiling(_configuration.OutputVideoBitrateMbits/2)}M ");

				// These arguments are here for improved compatibility. Level 4.0 is the lowest bandwidth required for FullHD - good for better compatibility and network streaming
				// http://blog.mediacoderhq.com/h264-profiles-and-levels/
				a.Append("-profile:v high -level:v 4.2 -pix_fmt yuv420p ");

				// Get the highest quality possible
				a.Append("-preset veryslow ");
			} else
			{
				a.Append("copy ");
			}
			a.Append($"-tune {(_transcodingJob.Action.HasFlag(EncodingTargetFlags.VideoIsAnimation) ? "animation" : "film")} ");

			// Audio
			a.Append("-map -0:a "); // Drop all audio tracks, and then add only those we want below.
			var audioTrackCounter = 0;
			
			// Main track, transcode if needed
			a.Append($"-map {mainAudioStream.StreamId}:{mainAudioStream.StreamIndex} -c:a:{audioTrackCounter} {(_transcodingJob.Action.HasFlag(EncodingTargetFlags.NeedsNewAudio) ? $"{_configuration.OutputAudioCodec} -b:a:{audioTrackCounter} {_configuration.OutputAudioBitrateMbits}M" : "copy")} ");
			if (_transcodingJob.Action.HasFlag(EncodingTargetFlags.NeedsNewAudio))
			{
				a.Append($"-metadata:s:a:{audioTrackCounter} title=\"{(!mainAudioStream.Metadata.ContainsKey("title") ? string.Empty : mainAudioStream.Metadata?["title"] + " ")}(PS4 Compatible)\" ");
			}
			a.Append($"-disposition:a:{audioTrackCounter} default ");
			audioTrackCounter++;

			// Copy the remaining audio tracks, as long as they are in the configured array of desired languages.
			foreach(var audioStream in fm.Streams.Where(x => x.StreamType == StreamTypeEnum.Audio  && _sofakingConfiguration.AudioLanguages.Contains(x.Language)))
			{
				if(audioStream == mainAudioStream)
				{
					continue;
				}

				// TODO: Refactor this
				var hasAcceptableCodec = audioStream.StreamCodec.ToLower() == "ac3" || audioStream.StreamCodec.ToLower() == "aac" || audioStream.StreamCodec.ToLower().Contains("pcm");
				a.Append($"-map {audioStream.StreamId}:{audioStream.StreamIndex} -c:a:{audioTrackCounter} {(!hasAcceptableCodec ? $"{_configuration.OutputAudioCodec} -b:a:{audioTrackCounter} {_configuration.OutputAudioBitrateMbits}M" : "copy")} ");
				if (!hasAcceptableCodec)
				{
					a.Append($"-metadata:s:a:{audioTrackCounter} title=\"{(!audioStream.Metadata.ContainsKey("title") ? string.Empty : audioStream.Metadata["title"] + " ")}(PS4 Compatible)\" ");
				}
				audioTrackCounter++;
			}

			// Subtitles
			a.Append($"-map 0:s{optionalFlag} ");

			// Prevents some errors.
			a.Append("-max_muxing_queue_size 9999 ");

			// Metadata
			// See: https://matroska.org/technical/specs/tagging/index.html
			a.Append($"-metadata COMMENT=\"Original file name: {Path.GetFileName(CurrentFile)}\" ");
			a.Append($"-metadata ENCODER_SETTINGS=\"V: {_configuration.OutputVideoCodec + (_transcodingJob.Action.HasFlag(EncodingTargetFlags.NeedsNewVideo) ? $"@{_configuration.OutputVideoBitrateMbits}M" : " (copy)")}, A:{_configuration.OutputAudioCodec}@{_configuration.OutputAudioBitrateMbits}M\" ");

			// Cover image
			if (!string.IsNullOrWhiteSpace(_transcodingJob.CoverImageJpg))
			{
				a.Append($"-attach \"{_transcodingJob.CoverImageJpg}\" -metadata:s:t mimetype=image/jpeg ");
			}

			if (_transcodingJob.Metadata != null && _transcodingJob.Metadata.Count > 0)
			{
				foreach (var m in _transcodingJob.Metadata)
				{
					if(string.IsNullOrWhiteSpace(m.Value))
					{
						continue;
					}

					if(m.Key == FFMPEGMetadataEnum.year)
					{
						if (int.TryParse(m.Value, out int year))
						{
							a.Append($"-metadata DATE_RELEASED=\"{year}\" ");
						}

						continue;
					}

					if (m.Key == FFMPEGMetadataEnum.description)
					{
						if (int.TryParse(m.Value, out int year))
						{
							a.Append($"-metadata SUMMARY=\"{year}\" ");
						}

						continue;
					}

					if (m.Key == FFMPEGMetadataEnum.IMDBRating)
					{
						if (double.TryParse(m.Value, out double imdbRating))
						{
							// convert 10-based rating to a 5-based rated
							double ratingBaseFive = Math.Floor(((imdbRating/10) * 5) * 10) / 10;
							a.Append($"-metadata RATING=\"{ratingBaseFive}\" ");
						}

						continue;
					}

					a.Append($"-metadata {Enum.GetName(typeof(FFMPEGMetadataEnum), m.Key).ToUpper()}=\"{m.Value}\" ");
				}
			}

			a.Append($"\"{_tempFile}\"");

			_logger.LogDebug("Setting up events");
			ffmpeg.Progress += (object sender, ConversionProgressEventArgs e) =>
			{
				OnProgress.Invoke(this, new EncodingProgressEventArgs(((double)e.ProcessedDuration.Ticks / (double)transcodingJob.Duration.Ticks) * 100d, CurrentFile, e.SizeKb, e.ProcessedDuration, e.Fps));
			};
				
			ffmpeg.Error += (object sender, ConversionErrorEventArgs e) => {
				_logger.LogError($"Encoding error {e.Exception.Message}", e.Exception);
				Kill();
				OnError.Invoke(this, new EncodingErrorEventArgs(e.Exception.Message));
			};
				
			ffmpeg.Complete += (object sender, ConversionCompleteEventArgs e) =>
			{
				_logger.LogWarning($"Encoding complete! {CurrentFile}");
				//if (_onSuccessInternal == null)
				//{
				//	_logger.LogError("No success action defined!");
				//	throw new EncoderException("No success action defined!");
				//}
				//// DONT CHANGE THE ORDER OF CALLS BELOW!!!
				//_logger.LogWarning("A");
				//_transcodingJob.OnComplete(); // Deletes the original source file
				//_logger.LogWarning("B");
				//_onDoneInternal(); // WORKS
				//_logger.LogWarning("C");
				//_onSuccessInternal(_tempFile); // Move the finished file, remove transcoding job from queue
				//_logger.LogWarning("D");
				//CleanTempData();
				//_logger.LogWarning("E");
				//_busy = false;
				//_logger.LogWarning("F");
				OnSuccess.Invoke(this, new EncodingSuccessEventArgs(_tempFile));
				_busy = false;
			};

			_logger.LogDebug($"Adding a {_shFile} sh file for debugging.");
			// Make an .sh file with the same command for eventual debugging
			await File.WriteAllTextAsync(_shFile, _configuration.FFMPEGBinary + " " + a.ToString());

			// For Windows...
			// TODO: Refactor paths
			_logger.LogDebug($"Adding a {_batFile} bat file for debugging.");
			var cmdWin = "ffmpeg -t 00:01:00 " + a.ToString().Replace("h264_omx", "h264").Replace("/mnt/hd1/", "Z:\\").Replace("/", "\\") + "\r\npause";
			cmdWin = Regex.Replace(cmdWin, @"Z:\\TEMP\\Transcoding\\(?<FileName>[^""]+?\.mkv)", "C:\\Users\\ohmy\\Desktop\\FFMPEG\\${FileName}", RegexOptions.Singleline | RegexOptions.IgnoreCase);
			await File.WriteAllTextAsync(_batFile, cmdWin);

			_logger.LogDebug("Starting ffmpeg");
			try
			{
				_ = ffmpeg.ExecuteAsync(a.ToString(), _cancellationTokenSource.Token);
			} catch(Exception ex)
			{
				_logger.LogError($"Starting FFMPEG failed! {ex.Message}", ex);
				Kill();
				return;
			}
			TranscodingStarted = DateTime.Now;
			_logger.LogDebug("Ffmpeg started");
			_stalledFileCandidate = null;
			StallMonitor();
			OnStart.Invoke(this, new EventArgs());
		}

		public void Kill()
		{
			_logger.LogWarning("Killing the encoder");
			_cancellationTokenSource.Cancel();
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		public void DisposeAndKeepFiles()
		{
			keepFiles = true;
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing)
		{
			if (disposed)
				return;

			if (disposing)
			{
				if (_transcodingJob != null && !keepFiles)
				{
					CleanTempData();
					Kill();
				}
			}

			disposed = true;
		}

		private void StallMonitor()
		{
			// Watch the task
			_ = Task.Run(() =>
			{
				while (true)
				{
					Thread.Sleep(stalledCheckInterval);
					_logger.LogWarning("Encoder monitoring stall");
					if (!Busy)
					{
						_logger.LogDebug("Transcoding not started");
						continue;
					}

					if (TranscodingStarted == null && CurrentFile != null)
					{
						if (_stalledFileCandidate == CurrentFile)
						{
							_logger.LogError($"Encoder stalled: {nameof(TranscodingStarted)} null (transcoding not started) even though {nameof(CurrentFile)} is set.");
							Kill();
							continue;
						}

						_logger.LogDebug($"Stalled candidate: {CurrentFile}");
						_stalledFileCandidate = CurrentFile;
					}

					if (TranscodingStarted != null && TranscodingStarted.Value < DateTime.Now.AddMinutes(-5) && _tempFile == null || !File.Exists(_tempFile))
					{
						_logger.LogError($"Encoder stalled: No temp file {_tempFile} seen since five minutes ago.");
						Kill();
						continue;
					}

					long currentTempFileSize = new FileInfo(_tempFile).Length;

					if (_lastTempFileSize != null && currentTempFileSize == _lastTempFileSize)
					{
						_logger.LogError($"Encoder stalled: No file increment in last ${stalledCheckInterval.TotalMinutes} minutes in {_tempFile}");
						Kill();
					}

					_logger.LogDebug($"Current Temp File Size = {currentTempFileSize}");
					_lastTempFileSize = currentTempFileSize;
				}
			});
		}

		private void CleanTempData(bool keepCommand = false)
		{
			_logger.LogDebug($"Cleaning up the TEMP data{(keepCommand ? ", keeping the command file." : ".")}");
			
			if (File.Exists(_tempFile))
			{
				File.Delete(_tempFile);
			}

			if (!keepCommand && File.Exists(_shFile))
			{ 
				File.Delete(_shFile);
			}

			if (!keepCommand && File.Exists(_batFile))
			{
				File.Delete(_batFile);
			}

			_tempFile = null;
			TranscodingStarted = null;
			_lastTempFileSize = null;
			_stalledFileCandidate = null;
			_transcodingJob = null; // This kills the .OnError attached to transcoding job
		}

		private static class Regexes
		{
			public static Regex FFMPEGBasicInfo => new Regex(@"^\s{2}Duration:\s(?<Duration>.+),\sstart\:\s(?<Start>.+),\sbitrate\:\s(?<Bitrate>\d+)\skb\/s", RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.Compiled | RegexOptions.ExplicitCapture, TimeSpan.FromSeconds(5));
			public static Regex FFMPEGStreams => new Regex(@"Stream\s#(?<StreamId>\d):(?<StreamIndex>\d)(?:\((?<Language>\w{3})\))?:\s(?<StreamType>\w+):\s(?<StreamCodec>.+?)(?:,\s|$)(?<StreamDetails>.+)?$(?:\s*Metadata:(?<MetaData>(?s).+?(?=$\s{5}\w|$\s\w)))?", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.ExplicitCapture, TimeSpan.FromSeconds(5));
			public static Regex FFMPEGMetadata => new Regex(@"^\s*(?<Key>[^\s]+)\s*:\s(?<Value>.+)$", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture | RegexOptions.Multiline);
		}

		/// <summary>
		/// Reset cancellation token source, which is originally only intended to be used once
		/// </summary>
		private void SetCancellationToken()
		{
			_cancellationTokenSource = new CancellationTokenSource();
			_cancellationTokenSource.Token.Register(() => {
				CleanTempData(true);
				OnCancelled.Invoke(this, new EventArgs());
				_busy = false;
				_logger.LogInformation("Encoding cancelled");
				_cancellationTokenSource = new CancellationTokenSource();
				_cancellationTokenSource.Token.Register(() => SetCancellationToken());
			});
		}

		private async Task<FFMPEGFileModel> GetFileModelAsync(string filePath)
		{
			if (string.IsNullOrWhiteSpace(filePath)) throw new ArgumentNullException(nameof(filePath));
			if (!File.Exists(filePath)) throw new Exception($"File {filePath} not found");

			var process = new Process()
			{
				StartInfo = new ProcessStartInfo
				{
					FileName = _configuration.FFMPEGBinary,
					Arguments = $"-i \"{filePath}\" -hide_banner",
					RedirectStandardOutput = true,
					RedirectStandardError = true,
					UseShellExecute = false,
					CreateNoWindow = true
				}
			};

			string result = string.Empty;

			await Task.Run(() =>
			{
				process.Start();
				result = process.StandardError.ReadToEnd();
				process.WaitForExit();
			});

			if (result == string.Empty)
			{
				_logger.LogError($"Did not receive any file information from FFMPEG about {filePath}");
				return null;
			}

			var basicInfo = Regexes.FFMPEGBasicInfo.Match(result);
			if (!basicInfo.Success)
			{
				_logger.LogError($"File did not have a valid FFMPEG header: {filePath}");
				return null;
			}

			var fm = new FFMPEGFileModel();
			fm.Duration = TimeSpan.ParseExact(basicInfo.Groups["Duration"].Value, "hh\\:mm\\:ss\\.ff", null);
			fm.Start = double.Parse(basicInfo.Groups["Start"].Value);
			fm.BitrateKbs = int.Parse(basicInfo.Groups["Bitrate"].Value);

			foreach (Match s in Regexes.FFMPEGStreams.Matches(result))
			{
				if (!int.TryParse(s.Groups["StreamId"].Value, out int streamId) || !int.TryParse(s.Groups["StreamIndex"].Value, out int streamIndex))
				{
					continue;
				}

				var metadata = new Dictionary<string, string>();

				foreach (Match m in Regexes.FFMPEGMetadata.Matches(s.Groups["MetaData"]?.Value))
				{
					metadata.Add(m.Groups["Key"].Value.Replace("\r", string.Empty), m.Groups["Value"].Value.Replace("\r", string.Empty));
				}

				fm.Streams.Add(new FFMPEGStreamModel
				{
					StreamIndex = streamIndex,
					StreamId = streamId,
					Language = s.Groups["Language"]?.Value?.Replace("\r", string.Empty),
					StreamType = Enum.Parse<StreamTypeEnum>(s.Groups["StreamType"].Value),
					StreamCodec = s.Groups["StreamCodec"]?.Value?.Replace("\r", string.Empty),
					StreamDetails = s.Groups["StreamDetails"]?.Value?.Replace("\r", string.Empty),
					Metadata = metadata
				});
			}

			return fm;
		}
	}
}
