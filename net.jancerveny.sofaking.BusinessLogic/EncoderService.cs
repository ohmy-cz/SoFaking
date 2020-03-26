﻿using FFmpeg.NET;
using FFmpeg.NET.Enums;
using FFmpeg.NET.Events;
using Microsoft.Extensions.Logging;
using net.jancerveny.sofaking.BusinessLogic.Exceptions;
using net.jancerveny.sofaking.BusinessLogic.Interfaces;
using net.jancerveny.sofaking.BusinessLogic.Models;
using net.jancerveny.sofaking.Common.Models;
using net.jancerveny.sofaking.Common.Utils;
using System;
using System.IO;
using System.Linq;
using System.Net.Http;
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
		private static bool _busy;
		private string _tempFile;
		private CancellationTokenSource _cancellationTokenSource;
		private ITranscodingJob _transcodingJob;
		private string _commandFile;
		private string _coverImageFile;
		/// <summary>
		/// Compound bitrate for video and audio
		/// </summary>
		public int TargetBitrateKbs { get { return (_configuration.OutputVideoBitrateMbits + _configuration.OutputAudioBitrateMbits) * 1024; } }
		public string CurrentFile { get; private set; }
		public bool Busy { get { return _busy; } }
		public DateTime? TranscodingStarted { get; private set; }
		private const char optionalFlag = '?';
		private static long? _lastTempFileSize;
		/// <summary>
		/// Constant Rate Factor (CRF)
		/// @see https://trac.ffmpeg.org/wiki/Encode/H.264
		/// </summary>
		private const int _crf = 16;
		private static readonly TimeSpan stalledCheckInterval = new TimeSpan(0, 5, 0);
		private Action _onDoneInternal;

		public EncoderService(ILogger<EncoderService> logger, EncoderConfiguration configuration, SoFakingConfiguration sofakingConfiguration)
		{
			_cancellationTokenSource = new CancellationTokenSource();
			_logger = logger;
			_configuration = configuration;
			_sofakingConfiguration = sofakingConfiguration;
			StallMonitor();
		}

		public async Task<IMediaInfo> GetMediaInfo(string filePath)
		{
			var ffmpeg = new Engine(_configuration.FFMPEGBinary);
			var inputFile = new MediaFile(filePath);
			var metaData = await ffmpeg.GetMetaDataAsync(inputFile); // TODO: Make own, this sometimes fails

			int? bitrateKbs = metaData.Duration.TotalSeconds == 0 ? null : (int?)(Math.Ceiling((metaData.FileInfo.Length * 8) / 1024d) / metaData.Duration.TotalSeconds);

			return new MediaInfo { 
				VideoCodec = metaData?.VideoData?.Format,
				VideoResolution = metaData?.VideoData?.FrameSize,
				BitrateKbs = bitrateKbs,
				FileInfo = metaData?.FileInfo,
				AudioCodec = metaData?.AudioData?.Format // I assume this takes the default audio stream...
			};
		}

		public void StartTranscoding(ITranscodingJob transcodingJob, Action onStart, Action onDoneInternal, Action onSuccessInternal, CancellationToken cancellationToken = default)
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

			// TODO: Find a nicer way?
			_onDoneInternal = onDoneInternal;
			_transcodingJob = transcodingJob;

			cancellationToken.Register(() => {
				_logger.LogDebug("Encoder cancelled from the outside");
				this.Kill();
			});

			CurrentFile = _transcodingJob.SourceFile;
			Task.Run(async () =>
			{
				if (_transcodingJob.Action == EncodingTargetFlags.None)
				{
					throw new Exception("No transcoding action selected");
				}

				_busy = true;
				var destinationFile = Path.Combine(_transcodingJob.DestinationFolder, Path.GetFileNameWithoutExtension(CurrentFile) + ".mkv");
				var ffmpeg = new Engine(_configuration.FFMPEGBinary);
				_tempFile = Path.Combine(_configuration.TempFolder, Path.GetFileNameWithoutExtension(CurrentFile) + ".mkv");
				_commandFile = Path.Combine(_configuration.TempFolder, Path.GetFileNameWithoutExtension(CurrentFile) + ".sh");
				_coverImageFile = Path.Combine(_configuration.TempFolder, Path.GetFileNameWithoutExtension(CurrentFile) + ".jpg");

				// Discard all streams except those in _configuration.AudioLanguages
				// Reencode the english stream only, and add it as primary stream. Copy all other desirable audio languages from the list.
				_logger.LogDebug("Constructing the encoder command");
				var a = new StringBuilder();
				a.Append($"-i \"{CurrentFile}\" ");
				a.Append("-c copy ");

				// Copy metadata
				a.Append("-map_metadata 0 ");

				// Video
				a.Append($"-map 0:v -c:v {(_transcodingJob.Action.HasFlag(EncodingTargetFlags.NeedsNewVideo) ? _configuration.OutputVideoCodec + $" -vf scale=1080:-2 -preset veryslow -crf {_crf} " : "copy")} "); // -b:v {_configuration.OutputVideoBitrateMbits}M gets ignored when CRF is used
				a.Append($"-tune {(_transcodingJob.Action.HasFlag(EncodingTargetFlags.VideoIsAnimation) ? "animation" : "film")} ");

				// Audio
				a.Append("-map -0:a "); // Drop all audio tracks, and then add only those we want below.
				var audioTrackCounter = 0;
				foreach(var lang in _sofakingConfiguration.AudioLanguages)
				{
					a.Append($"-map 0:a:m:language:{lang}{optionalFlag} -c:a:{audioTrackCounter} {(_transcodingJob.Action.HasFlag(EncodingTargetFlags.NeedsNewAudio) ? $"{_configuration.OutputAudioCodec} -b:a:{audioTrackCounter} {_configuration.OutputAudioBitrateMbits}M" : "copy")} ");
					if(audioTrackCounter == 0)
					{
						if(_transcodingJob.Action.HasFlag(EncodingTargetFlags.NeedsNewAudio))
						{
							a.Append($"-metadata:s:a:{audioTrackCounter} title=\"PS4 Compatible\" ");
						}

						a.Append($"-disposition:a:{audioTrackCounter} default ");
					}
					audioTrackCounter++;

					// The English default track  is usually in a very high quality that we can't play on a PS4, but we don't want to lose
					// So we copy it as the second track.
					if (lang == "eng" && _transcodingJob.Action.HasFlag(EncodingTargetFlags.NeedsNewAudio))
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

				// Metadata
				// See: https://matroska.org/technical/specs/tagging/index.html
				a.Append("-metadata ENCODER=\"SoFaking\" ");
				a.Append($"-metadata COMMENT=\"Original file name: {Path.GetFileName(CurrentFile)}\" ");
				a.Append($"-metadata ENCODER_SETTINGS=\"V: {_configuration.OutputVideoCodec} -CRF {_crf}, A:{_configuration.OutputAudioCodec}@{_configuration.OutputAudioBitrateMbits}M\" ");
				if (_transcodingJob.Metadata != null && _transcodingJob.Metadata.Count > 0)
				{
					foreach (var m in _transcodingJob.Metadata)
					{
						if(string.IsNullOrWhiteSpace(m.Value))
						{
							continue;
						}

						if(m.Key == FFMPEGMetadataEnum.cover)
						{
							if (!string.IsNullOrWhiteSpace(m.Value))
							{
								try
								{
									await Download.GetFile(m.Value, _coverImageFile);
									a.Append($"-attach \"{_coverImageFile}\" -metadata:s:t mimetype=image/jpeg ");
								}
								catch (Exception _)
								{
									_coverImageFile = null;
								}
							}

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
					_logger.LogInformation($"Progress: {Math.Floor(e.ProcessedDuration / e.TotalDuration)}%\tProcessed duration: {e.ProcessedDuration}\tFPS:{e.Fps}\tSize: {e.SizeKb} kb\t{Path.GetFileName(CurrentFile)}");
				};
				
				ffmpeg.Error += (object sender, ConversionErrorEventArgs e) => {
					_logger.LogError($"Encoding error {e.Exception.Message}", e.Exception);
					CleanTempData();
					_onDoneInternal();
					_transcodingJob.OnError();
					_busy = false;
				};
				
				ffmpeg.Complete += (object sender, ConversionCompleteEventArgs e) =>
				{
					if(!Directory.Exists(_transcodingJob.DestinationFolder))
					{
						Directory.CreateDirectory(_transcodingJob.DestinationFolder);
					}

					File.Move(_tempFile, destinationFile);
					_logger.LogWarning($"Encoding complete! {CurrentFile}");
					CleanTempData();
					_onDoneInternal();
					_transcodingJob.OnComplete();
					onSuccessInternal();
					_busy = false;
				};

				_cancellationTokenSource.Token.Register(() => {
					_logger.LogWarning("Encoding cancelled");
					CleanTempData();
					_onDoneInternal();
					_busy = false;
				});

				_logger.LogDebug($"Adding a {_commandFile} command file for debugging.");
				// Make an .sh file with the same command for eventual debugging
				await File.WriteAllTextAsync(_commandFile, _configuration.FFMPEGBinary + " " + a.ToString());

				_logger.LogDebug("Starting ffmpeg");
				await ffmpeg.ExecuteAsync(a.ToString(), _cancellationTokenSource.Token);
				TranscodingStarted = DateTime.Now;
				onStart?.Invoke();
			});
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
					if (TranscodingStarted == null)
					{
						_logger.LogDebug("Transcoding not started");
						continue;
					}

					if (_tempFile == null || !File.Exists(_tempFile))
					{
						_logger.LogError($"Encoder stalled: No temp file {_tempFile} seen since {stalledCheckInterval.TotalMinutes} minutes.");
						CleanTempData(true);
						try
						{
							_onDoneInternal.Invoke();
						} catch(Exception ex)
						{
							_logger.LogError($"Error in {nameof(_onDoneInternal)}: {ex.Message}", ex);
						}
						try
						{
							_logger.LogDebug($"OnError is: {(_transcodingJob?.OnError == null ? "null" : _transcodingJob?.OnError?.GetType().Name) }");
							_transcodingJob?.OnError?.Invoke();
						}
						catch (Exception ex)
						{
							_logger.LogError($"Error in {nameof(_transcodingJob.OnError)}: {ex.Message}", ex);
						}
						_busy = false;
						continue;
					}

					long currentTempFileSize = new FileInfo(_tempFile).Length;

					if (_lastTempFileSize != null && currentTempFileSize == _lastTempFileSize)
					{
						_logger.LogError($"Encoder stalled: No file increment in last ${stalledCheckInterval.TotalMinutes} minutes in {_tempFile}");
						CleanTempData(true);
						try
						{
							_onDoneInternal.Invoke();
						}
						catch (Exception ex)
						{
							_logger.LogError($"Error in {nameof(_onDoneInternal)}: {ex.Message}", ex);
						}
						try
						{
							_logger.LogDebug($"OnError is: {(_transcodingJob?.OnError == null ? "null" : _transcodingJob?.OnError?.GetType().Name) }");
							_transcodingJob?.OnError?.Invoke();
						}
						catch (Exception ex)
						{
							_logger.LogError($"Error in {nameof(_transcodingJob.OnError)}: {ex.Message}", ex);
						}
					}

					_logger.LogDebug($"Current Temp File Size = {currentTempFileSize}");
					_lastTempFileSize = currentTempFileSize;
				}
			});
		}

		private void CleanTempData(bool keepCommand = false)
		{
			_logger.LogDebug($"Cleaning up the TEMP data{(keepCommand ? ", keeping the command file." : ".")}");
			
			if (!string.IsNullOrWhiteSpace(_coverImageFile))
			{
				File.Delete(_coverImageFile);
			}

			if (File.Exists(_tempFile))
			{
				File.Delete(_tempFile);
			}

			if (!keepCommand && File.Exists(_commandFile))
			{ 
				File.Delete(_commandFile);
			}

			CurrentFile = null;
			_coverImageFile = null;
			_tempFile = null;
			TranscodingStarted = null;
			_lastTempFileSize = null;
			//_transcodingJob = null; // This kills the .OnError attached to transcoding job
		}

		public void Kill()
		{
			_logger.LogDebug("Killing the encoder");
			_cancellationTokenSource.Cancel();
		}
	}
}
