using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using net.jancerveny.sofaking.BusinessLogic;
using net.jancerveny.sofaking.BusinessLogic.Interfaces;
using net.jancerveny.sofaking.BusinessLogic.Models;
using net.jancerveny.sofaking.Common.Utils;
using net.jancerveny.sofaking.DataLayer.Models;
using net.jancerveny.sofaking.WorkerService.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace net.jancerveny.sofaking.WorkerService
{
	public class DownloadFinishedWorker : BackgroundService
	{
		private static class Regexes
		{
			public static Regex FileSystemSafeName => new Regex(@"[^\sa-z0-9_-]+", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline);
			public static Regex VideoFileTypes => new Regex(@"(.+(\.mkv|\.avi|\.mp4))", RegexOptions.Compiled | RegexOptions.IgnoreCase);
			public static Regex FileNamePattern => new Regex(@"^(.+)(\.[a-z]{3})$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
		}
		private readonly IHttpClientFactory _clientFactory;
		private readonly ILogger<DownloadFinishedWorker> _logger;
		private readonly DownloadFinishedWorkerConfiguration _configuration;
		private readonly MovieService _movieService;
		private readonly ITorrentClientService _torrentClient;
		private readonly IEncoderService _encoderService;
		private static ConcurrentDictionary<int, ITranscodingJob> _transcodingJobs = new ConcurrentDictionary<int, ITranscodingJob>();

		public DownloadFinishedWorker(ILogger<DownloadFinishedWorker> logger, IHttpClientFactory clientFactory, DownloadFinishedWorkerConfiguration configuration, MovieService movieService, ITorrentClientService torrentClient, IEncoderService encoderService)
		{
			if (clientFactory == null) throw new ArgumentNullException(nameof(clientFactory));
			if (movieService == null) throw new ArgumentNullException(nameof(movieService));
			if (configuration == null) throw new ArgumentNullException(nameof(configuration));
			if (logger == null) throw new ArgumentNullException(nameof(logger));
			if (torrentClient == null) throw new ArgumentNullException(nameof(torrentClient));
			if (encoderService == null) throw new ArgumentNullException(nameof(encoderService));
			_clientFactory = clientFactory;
			_movieService = movieService;
			_configuration = configuration;
			_logger = logger;
			_torrentClient = torrentClient;
			_encoderService = encoderService;
		}

		protected override async Task ExecuteAsync(CancellationToken stoppingToken)
		{
			try
			{
				await HandleCrashedTranscoding();
			}
			catch (Exception ex)
			{
				// Get stack trace for the exception with source file information
				var st = new StackTrace(ex, true);
				// Get the top stack frame
				var frame0 = st.GetFrame(0);
				var frame1 = st.GetFrame(1);
				var frame2 = st.GetFrame(2);
				var frame3 = st.GetFrame(3);
				_logger.LogError($"Handling crashed transcoding failed {ex.Message}", ex);
				_logger.LogError($"{frame0.GetFileName()?.Split("\\")?.Last() ?? string.Empty} L{frame0.GetFileLineNumber()}");
				_logger.LogError($"{frame1.GetFileName()?.Split("\\")?.Last() ?? string.Empty} L{frame1.GetFileLineNumber()}");
				_logger.LogError($"{frame2.GetFileName()?.Split("\\")?.Last() ?? string.Empty} L{frame2.GetFileLineNumber()}");
				_logger.LogError($"{frame3.GetFileName()?.Split("\\")?.Last() ?? string.Empty} L{frame3.GetFileLineNumber()}");
			}

			while (!stoppingToken.IsCancellationRequested)
			{
				_logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
				IReadOnlyList<ITorrentClientTorrent> torrents;

				try
				{
					await HandleLongRunningTranscoding();
				} catch(Exception ex)
				{
					var st = new StackTrace(ex, true);
					var frame0 = st.GetFrame(0);
					var frame1 = st.GetFrame(1);
					var frame2 = st.GetFrame(2);
					var frame3 = st.GetFrame(3);
					_logger.LogError($"Could not handle long running transcoding check. {ex.Message}", ex);
					_logger.LogError($"{frame0.GetFileName()?.Split("\\")?.Last() ?? string.Empty} L{frame0.GetFileLineNumber()}");
					_logger.LogError($"{frame1.GetFileName()?.Split("\\")?.Last() ?? string.Empty} L{frame1.GetFileLineNumber()}");
					_logger.LogError($"{frame2.GetFileName()?.Split("\\")?.Last() ?? string.Empty} L{frame2.GetFileLineNumber()}");
					_logger.LogError($"{frame3.GetFileName()?.Split("\\")?.Last() ?? string.Empty} L{frame3.GetFileLineNumber()}");
				}

				try
				{
					torrents = await _torrentClient.GetAllTorrents();

					if (torrents.Count() == 0)
					{
						continue;
					}

					_logger.LogInformation($"Found {torrents.Count()} torrents");
				}
				catch (Exception ex)
				{
					var st = new StackTrace(ex, true);
					var frame0 = st.GetFrame(0);
					var frame1 = st.GetFrame(1);
					var frame2 = st.GetFrame(2);
					var frame3 = st.GetFrame(3);
					_logger.LogError($"Getting all torrents from the Torrent client {_torrentClient.GetType().Name} failed: {ex.Message}", ex);
					_logger.LogError($"{frame0.GetFileName()?.Split("\\")?.Last() ?? string.Empty} L{frame0.GetFileLineNumber()}");
					_logger.LogError($"{frame1.GetFileName()?.Split("\\")?.Last() ?? string.Empty} L{frame1.GetFileLineNumber()}");
					_logger.LogError($"{frame2.GetFileName()?.Split("\\")?.Last() ?? string.Empty} L{frame2.GetFileLineNumber()}");
					_logger.LogError($"{frame3.GetFileName()?.Split("\\")?.Last() ?? string.Empty} L{frame3.GetFileLineNumber()}");
					continue;
				}

				try
				{
					await HandleTorrentStatusUpdates(torrents);
				} catch(Exception ex)
				{
					var st = new StackTrace(ex, true);
					var frame0 = st.GetFrame(0);
					var frame1 = st.GetFrame(1);
					var frame2 = st.GetFrame(2);
					var frame3 = st.GetFrame(3);
					_logger.LogError($"Handling torrent status updates failed: {ex.Message}", ex);
					_logger.LogError($"{frame0.GetFileName()?.Split("\\")?.Last() ?? string.Empty} L{frame0.GetFileLineNumber()}");
					_logger.LogError($"{frame1.GetFileName()?.Split("\\")?.Last() ?? string.Empty} L{frame1.GetFileLineNumber()}");
					_logger.LogError($"{frame2.GetFileName()?.Split("\\")?.Last() ?? string.Empty} L{frame2.GetFileLineNumber()}");
					_logger.LogError($"{frame3.GetFileName()?.Split("\\")?.Last() ?? string.Empty} L{frame3.GetFileLineNumber()}");
				}

				try
				{
					await HandleQueuedTranscoding(torrents, stoppingToken);
				}
				catch (Exception ex)
				{
					var st = new StackTrace(ex, true);
					var frame0 = st.GetFrame(0);
					var frame1 = st.GetFrame(1);
					var frame2 = st.GetFrame(2);
					var frame3 = st.GetFrame(3);
					_logger.LogError($"Handling of queued transcoding failed: {ex.Message}", ex);
					_logger.LogError($"{frame0.GetFileName()?.Split("\\")?.Last() ?? string.Empty} L{frame0.GetFileLineNumber()}");
					_logger.LogError($"{frame1.GetFileName()?.Split("\\")?.Last() ?? string.Empty} L{frame1.GetFileLineNumber()}");
					_logger.LogError($"{frame2.GetFileName()?.Split("\\")?.Last() ?? string.Empty} L{frame2.GetFileLineNumber()}");
					_logger.LogError($"{frame3.GetFileName()?.Split("\\")?.Last() ?? string.Empty} L{frame3.GetFileLineNumber()}");
				}

				try
				{
					await HandleDownloadedMovies(torrents, stoppingToken);
				}
				catch (Exception ex)
				{
					var st = new StackTrace(ex, true);
					var frame0 = st.GetFrame(0);
					var frame1 = st.GetFrame(1);
					var frame2 = st.GetFrame(2);
					var frame3 = st.GetFrame(3);
					_logger.LogError($"Handling downloaded and transcoded movies failed {ex.Message}", ex);
					_logger.LogError($"{frame0.GetFileName()?.Split("\\")?.Last() ?? string.Empty} L{frame0.GetFileLineNumber()}");
					_logger.LogError($"{frame1.GetFileName()?.Split("\\")?.Last() ?? string.Empty} L{frame1.GetFileLineNumber()}");
					_logger.LogError($"{frame2.GetFileName()?.Split("\\")?.Last() ?? string.Empty} L{frame2.GetFileLineNumber()}");
					_logger.LogError($"{frame3.GetFileName()?.Split("\\")?.Last() ?? string.Empty} L{frame3.GetFileLineNumber()}");
				}

				await Task.Delay(10 * 1000, stoppingToken);
			}
		}

		private async Task HandleLongRunningTranscoding()
		{
			var longRunningTranscoding = (await _movieService.GetMoviesAsync()).Where(x => x.Status == MovieStatusEnum.TranscodingStarted && x.TranscodingStarted != null && x.TranscodingStarted < DateTime.Now.AddDays(-1)).FirstOrDefault();
			if (longRunningTranscoding != null)
			{
				await _movieService.SetMovieStatus(longRunningTranscoding.Id, MovieStatusEnum.TranscodingRunningTooLong);
			}
		}

		private async Task HandleQueuedTranscoding(IReadOnlyList<ITorrentClientTorrent> torrents, CancellationToken cancellationToken)
		{
			if((await _movieService.GetMoviesAsync()).Where(x => x.Status == MovieStatusEnum.TranscodingStarted).Any())
			{
				return;
			}

			var queuedMovieJob = (await _movieService.GetMoviesAsync()).Where(x => x.Status == MovieStatusEnum.TranscodingQueued || x.Status == MovieStatusEnum.TranscodingIncomplete).FirstOrDefault();
			
			if (queuedMovieJob != null)
			{
				var torrent = torrents.Where(x => x.Hash == queuedMovieJob.TorrentHash).FirstOrDefault();
				if (torrent != null)
				{
					var transcodingResult = await Transcode(queuedMovieJob.Id, torrent, cancellationToken);
					if (transcodingResult.FilesToMove.Length > 0)
					{
						await MoveVideoFiles(queuedMovieJob, torrent, transcodingResult.FilesToMove);
					}
				}
			}
		}

		private async Task HandleCrashedTranscoding()
		{
			var movieJobs = (await _movieService.GetMoviesAsync()).Where(x => x.Status == MovieStatusEnum.TranscodingStarted);
			if(movieJobs.Count() > 0)
			{
				foreach (var movieJob in movieJobs)
				{
					await _movieService.SetMovieStatus(movieJob.Id, MovieStatusEnum.TranscodingIncomplete);
				}
			}
		}

		private async Task HandleDownloadedMovies(IReadOnlyList<ITorrentClientTorrent> torrents, CancellationToken cancellationToken)
		{
			var downloadedMovies = (await _movieService.GetMoviesAsync())
				.Where(x =>
					x.Deleted == null &&
					(x.Status == MovieStatusEnum.Downloaded)
				);

			if (downloadedMovies.Count() == 0)
			{
				return;
			}

			foreach (var torrent in torrents)
			{
				var movieJob = downloadedMovies
					.Where(x => x.TorrentHash == torrent.Hash)
					.FirstOrDefault();

				if (movieJob == null)
				{
					continue;
				}

				// Move the file in its own folder, if the torrent was a single file.
				var downloadDirectory = MovieDownloadDirectory(torrent, out string torrentFileNameExtension);

				if (!string.IsNullOrWhiteSpace(torrentFileNameExtension))
				{
					var downloadFile = Path.Combine(_configuration.MoviesDownloadDir, torrent.Name);
					if (!Directory.Exists(downloadDirectory) && !File.Exists(downloadFile))
					{
						await _movieService.SetMovieStatus(movieJob.Id, MovieStatusEnum.FileNotFound);
						_logger.LogDebug($"Would delete torrent: {torrent.Id}");
#if RELEASE
						await _torrentClient.RemoveTorrent(torrent.Id);
#endif
						throw new Exception("Download doesn't exist either as File or folder. Maybe it was removed by the user?");
					}

					if (!Directory.Exists(downloadDirectory))
					{
						Directory.CreateDirectory(downloadDirectory);
					}

					if (File.Exists(downloadFile))
					{
						File.Move(downloadFile, Path.Combine(downloadDirectory, Regexes.FileSystemSafeName.Replace(movieJob.Title, string.Empty) + torrentFileNameExtension));
					}
				}

				// Move only the video files if no transcoding was necessary.
				var transcodingResult = await Transcode(movieJob.Id, torrent, cancellationToken);

				if(transcodingResult.Result == TranscodeResultEnum.NoVideoFiles)
				{
					_logger.LogDebug($"Would delete: {downloadDirectory}");
#if RELEASE
					Directory.Delete(downloadDirectory, true);
					await _torrentClient.RemoveTorrent(torrent.Id);
#endif
					await _movieService.SetMovieStatus(movieJob.Id, MovieStatusEnum.NoVideoFilesError);
					continue;
				}

				if(transcodingResult.FilesToMove.Length > 0)
				{
					await MoveVideoFiles(movieJob, torrent, transcodingResult.FilesToMove);
				}

				if (transcodingResult.Result == TranscodeResultEnum.TranscodingNotNeeded)
				{
					await SuccessFinishingActionAsync(torrent, movieJob);
				}
			}
		}

		private async Task MoveVideoFiles(Movie movie, ITorrentClientTorrent torrent, string[] videoFilesToMove)
		{
			var finishedMovieDirectory = MovieFinishedDirectory(movie);
			if (!Directory.Exists(finishedMovieDirectory))
			{
				Directory.CreateDirectory(finishedMovieDirectory);
			}

			try
			{
				foreach (var videoFile in videoFilesToMove)
				{
					File.Move(videoFile, Path.Combine(finishedMovieDirectory, Path.GetFileName(videoFile)));
				}
			}
			catch (Exception e)
			{
				await _movieService.SetMovieStatus(movie.Id, MovieStatusEnum.FileInUse);
				_logger.LogDebug($"Would delete torrent: {torrent.Id}");
#if RELEASE
				await _torrentClient.RemoveTorrent(torrent.Id);
#endif
				_logger.LogError($"Could not move the final downloaded files: {e.Message}. Is FFMPEG still running?", e);
			}
		}

		private async Task HandleTorrentStatusUpdates(IReadOnlyList<ITorrentClientTorrent> torrents)
		{
			var downloadingMovies = (await _movieService.GetMoviesAsync())
				.Where(x =>
					x.Deleted == null &&
					(x.Status == MovieStatusEnum.Downloading || x.Status == MovieStatusEnum.DownloadQueued || x.Status == MovieStatusEnum.DownloadingPaused)
				);

			foreach (var torrent in torrents)
			{
				var movieJob = downloadingMovies.Where(x => x.TorrentHash == torrent.Hash).FirstOrDefault();
				if (movieJob == null)
				{
					continue;
				}

				// Update our database status
				MovieStatusEnum status = movieJob.Status;
				switch (torrent.Status)
				{
					case TorrentStatusEnum.Check:
					case TorrentStatusEnum.CheckWait:
					case TorrentStatusEnum.DownloadWait:
						status = MovieStatusEnum.DownloadQueued;
						break;
					case TorrentStatusEnum.Download:
						status = MovieStatusEnum.Downloading;
						break;
					case TorrentStatusEnum.Stopped:
						status = MovieStatusEnum.DownloadingPaused;
						break;
				}

				if (movieJob.Status != status)
				{
					await _movieService.SetMovieStatus(movieJob.Id, status);
				}

				if (!torrent.IsFinished)
				{
					continue;
				}

				var path = Path.Combine(_configuration.MoviesDownloadDir, torrent.Name);
				if (Directory.Exists(path) || File.Exists(path))
				{
					await _movieService.SetMovieStatus(movieJob.Id, MovieStatusEnum.Downloaded);
				}
			}
		}

		private async Task AddCoverImage(Movie movie)
		{
			try
			{
				if (!string.IsNullOrWhiteSpace(movie.ImageUrl))
				{
					await Download.GetFile(_clientFactory, movie.ImageUrl, Path.Combine(MovieFinishedDirectory(movie), "Cover.jpg"));
				}
			}
			catch (Exception ex)
			{
				_logger.LogError("Could not create a Cover image.", ex);
			}
		}

		// TODO: Split into Analyse and Transcode, so the Analysis part doesn't get called when run from Queued
		private async Task<TranscodeResult> Transcode(int movieJobId, ITorrentClientTorrent torrent, CancellationToken cancellationToken)
		{
			var sourcePath = MovieDownloadDirectory(torrent);
			var videoFiles = GetVideoFilesInDir(sourcePath);

			if(videoFiles == null || videoFiles.Length == 0)
			{
				await _movieService.SetMovieStatus(movieJobId, MovieStatusEnum.TranscodingError);
				_logger.LogWarning($"{torrent.Name} has no compatible video files.");
				return new TranscodeResult(TranscodeResultEnum.NoVideoFiles);
			}

			var movie = (await _movieService.GetMoviesAsync()).Where(x => x.Id == movieJobId).FirstOrDefault();
			await _movieService.SetMovieStatus(movieJobId, MovieStatusEnum.AnalysisStarted);
			var pendingTranscodingJobs = new List<ITranscodingJob>();
			var filesToMove = new List<string>();

			// Check if any files need transcoding
			foreach (var videoFile in videoFiles)
			{
				_logger.LogInformation($"Analyzing {Path.GetFileName(videoFile)}");
				string coverImageFile = null;
				var mediaInfo = await _encoderService.GetMediaInfo(videoFile);
				if(mediaInfo == null)
				{
					_logger.LogWarning($"File {Path.GetFileName(videoFile)} returned no {nameof(mediaInfo)}");
					continue;
				}

				var flags = EncodingTargetFlags.None;

				try
				{
					if (!HasAcceptableVideo(mediaInfo))
					{
						flags |= EncodingTargetFlags.NeedsNewVideo;
					}
				}
				catch (ArgumentException ex)
				{
					_logger.LogError($"{Path.GetFileName(videoFile)}", ex);
					_logger.LogError($"{ex.ParamName}: {ex.Message}");
					await _movieService.SetMovieStatus(movieJobId, MovieStatusEnum.AnalysisVideoFailed);
					continue;
				}
				
				try
				{
					if (!HasAcceptableAudio(mediaInfo))
					{
						flags |= EncodingTargetFlags.NeedsNewAudio;
					}
				}
				catch (ArgumentException ex)
				{
					_logger.LogError($"{Path.GetFileName(videoFile)}", ex);
					_logger.LogError($"{ex.ParamName}: {ex.Message}");
					await _movieService.SetMovieStatus(movieJobId, MovieStatusEnum.AnalysisAudioFailed);
					continue;
				}

				if (movie.Genres.HasFlag(GenreFlags.Animation))
				{
					flags |= EncodingTargetFlags.VideoIsAnimation;
				}

				if(flags == EncodingTargetFlags.None)
				{
					filesToMove.Add(videoFile);
					continue;
				}

				if (!string.IsNullOrWhiteSpace(movie.ImageUrl))
				{
					try
					{
						coverImageFile = Regexes.FileNamePattern.Match(videoFile).Groups[1].Value + ".jpg";
						await Download.GetFile(_clientFactory, movie.ImageUrl, coverImageFile);
					} catch(Exception _)
					{
						coverImageFile = null;
					}
				}

				pendingTranscodingJobs.Add(new TranscodingJob
				{
					SourceFile = videoFile,
					DestinationFolder = MovieFinishedDirectory(movie),
					Action = flags,
					OnComplete = () =>
					{
						_logger.LogDebug($"Would delete: {videoFile}");
#if RELEASE
						File.Delete(videoFile);
#endif
					},
					OnError = async () =>
					{
						_logger.LogError($"Transcoding had some error");
						await _movieService.SetMovieStatus(movieJobId, MovieStatusEnum.TranscodingError);
					},
					CancellationToken = cancellationToken,
					Metadata = new Dictionary<FFMPEGMetadataEnum, string> {
						{ FFMPEGMetadataEnum.title, movie.Title },
						{ FFMPEGMetadataEnum.cover, coverImageFile },
						{ FFMPEGMetadataEnum.year, movie.Year.ToString() },
						{ FFMPEGMetadataEnum.author, movie.Director },
						{ FFMPEGMetadataEnum.description, movie.Description },
						{ FFMPEGMetadataEnum.episode_id, movie.EpisodeId },
						{ FFMPEGMetadataEnum.genre, movie.Genres.ToString() },
						{ FFMPEGMetadataEnum.show, movie.Show }
					}
				});
			}

			// No transcoding needed for any video files in the folder
			// Using this pendingTranscodingJobsList allows us to return for movies that need no transcoding, so they won't wait for the transcoding to be complete and can be simply copied  over to the resulting folder.
			if (!pendingTranscodingJobs.Any())
			{
				return new TranscodeResult(TranscodeResultEnum.TranscodingNotNeeded, filesToMove.ToArray());
			}

			// If something else is transcoding, then queue
			var tjmem = _transcodingJobs.Any();
			var tjdb = (await _movieService.GetMoviesAsync()).Where(x => x.Status == MovieStatusEnum.TranscodingStarted).Any();
			if (tjmem || tjdb)
			{
				await _movieService.SetMovieStatus(movieJobId, MovieStatusEnum.TranscodingQueued);
				return new TranscodeResult(TranscodeResultEnum.Queued, filesToMove.ToArray());
			}

			// Start transcoding by copying our pending tasks into the static global queue
			await _movieService.SetMovieStatus(movieJobId, MovieStatusEnum.TranscodingStarted);
			foreach(var transcodingJob in pendingTranscodingJobs)
			{
				_transcodingJobs.TryAdd(_transcodingJobs.IsEmpty ? 0 : _transcodingJobs.Last().Key + 1, transcodingJob);
			}

			// Get the first job from the stack, then drop it
			_ = Task.Run(async () =>
			{
				while (_transcodingJobs.Count() > 0 && _transcodingJobs.TryGetValue(_transcodingJobs.FirstOrDefault().Key, out var transcodingJob))
				{
					if (transcodingJob.SourceFile == _encoderService.CurrentFile)
					{
						continue;
					}

					try
					{
						_encoderService.StartTranscoding(transcodingJob, () =>
						{
							_transcodingJobs.TryRemove(_transcodingJobs.First().Key, out _);
						}, async () =>
						{
							if (_transcodingJobs.Count == 0)
							{
								await SuccessFinishingActionAsync(torrent, movie); // TODO: This could be getting wrong values because of threading
							}
						});
					}
					catch (Exception e)
					{
						_transcodingJobs.TryRemove(0, out _);
						await _movieService.SetMovieStatus(movieJobId, MovieStatusEnum.TranscodingError);
						_logger.LogError(e.Message);
					}
				}
			});

			return new TranscodeResult(TranscodeResultEnum.Transcoding, filesToMove.ToArray());
		}

		private static string[] GetVideoFilesInDir(string sourcePath)
		{
			if(!Directory.Exists(sourcePath))
			{
				return null;
			}

			return Directory
				.GetFiles(sourcePath)
				.Where(x => Regexes.VideoFileTypes.IsMatch(x) && !x.ToLower().Contains("sample"))
				.OrderByDescending(x => new FileInfo(x).Length)
				.ToArray();
		}

		public async Task SuccessFinishingActionAsync(ITorrentClientTorrent torrent, Movie movie)
		{
			await AddCoverImage(movie);
			try
			{
				_logger.LogDebug($"Would delete: {MovieDownloadDirectory(torrent)}");
#if RELEASE
				Directory.Delete(MovieDownloadDirectory(torrent), true);
#endif
			} catch(IOException e)
			{
				await _movieService.SetMovieStatus(movie.Id, MovieStatusEnum.CouldNotCleanup);
			}
			_logger.LogDebug($"Would remove torrent: {torrent.Id}");
#if RELEASE
			await _torrentClient.RemoveTorrent(torrent.Id);
#endif
			await _movieService.SetMovieStatus(movie.Id, MovieStatusEnum.Finished);
		}

		private bool HasAcceptableVideo(IMediaInfo mediaInfo)
		{
			if (_configuration.MaxPS4FileSizeGb == 0) throw new ArgumentNullException(nameof(_configuration.MaxPS4FileSizeGb));
			if (_configuration.Resolution == null) throw new ArgumentNullException(nameof(_configuration.Resolution));
			if (_configuration.AcceptedVideoCodecs == null) throw new ArgumentNullException(nameof(_configuration.AcceptedVideoCodecs));
			if (mediaInfo == null) throw new ArgumentNullException(nameof(mediaInfo));
			if (mediaInfo.VideoCodec == null) throw new ArgumentNullException(nameof(mediaInfo.VideoCodec));
			if (mediaInfo.FileInfo == null) throw new ArgumentNullException(nameof(mediaInfo.FileInfo));
			if (!int.TryParse(_configuration.Resolution.Split("x")[0], out int allowedVideoWidth)) throw new ArgumentException($"Allowed video width not configured", nameof(_configuration.Resolution));
			if (!int.TryParse(mediaInfo.VideoResolution.Split("x")[0], out int videoWidth)) throw new ArgumentException($"Unknown video file resolution: {mediaInfo?.VideoResolution ?? "N/A"}", nameof(mediaInfo.VideoResolution));

			var acceptableCodec = false;
			foreach (var vc in _configuration.AcceptedVideoCodecs)
			{
				if (mediaInfo.VideoCodec.IndexOf(vc) >= 0)
				{
					acceptableCodec = true;
					break;
				}
			}

			var acceptableResolution = videoWidth <= allowedVideoWidth;
			var acceptableSize = mediaInfo.FileInfo.Length > (_configuration.MaxPS4FileSizeGb * 1024 * 1024);
			var acceptableBitrate = (mediaInfo.BitrateKbs == null || mediaInfo.BitrateKbs <= _encoderService.TargetBitrateKbs);

			return acceptableCodec && acceptableResolution && acceptableSize && acceptableBitrate;
		}

		private bool HasAcceptableAudio(IMediaInfo mediaInfo)
		{
			if (_configuration.AcceptedAudioCodecs == null) throw new ArgumentNullException(nameof(_configuration.AcceptedAudioCodecs));
			if (mediaInfo == null) throw new ArgumentNullException(nameof(mediaInfo));
			if (mediaInfo.AudioCodec == null) throw new ArgumentNullException(nameof(mediaInfo.AudioCodec));

			return _configuration.AcceptedAudioCodecs.Contains(mediaInfo.AudioCodec);
		}
		
		private string MovieDownloadDirectory(ITorrentClientTorrent torrent, out string torrentFileNameExtension)
		{
			torrentFileNameExtension = null;
			var fileName = Regexes.FileNamePattern.Match(torrent.Name);
			if (fileName.Success)
			{
				torrentFileNameExtension = fileName.Groups[2].Value;
			}

			return MovieDownloadDirectory(torrent);
		}

		private string MovieDownloadDirectory(ITorrentClientTorrent torrent)
		{
			var fileName = Regexes.FileNamePattern.Match(torrent.Name);
			return Path.Combine(_configuration.MoviesDownloadDir, fileName.Success ? fileName.Groups[1].Value : torrent.Name);
		}

		private string MovieFinishedDirectory(Movie movie) => Path.Combine(_configuration.MoviesFinishedDir, Regexes.FileSystemSafeName.Replace(movie.Title, string.Empty));
	}
}
