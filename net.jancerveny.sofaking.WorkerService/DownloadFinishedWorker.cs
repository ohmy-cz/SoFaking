using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using net.jancerveny.sofaking.BusinessLogic;
using net.jancerveny.sofaking.BusinessLogic.Interfaces;
using net.jancerveny.sofaking.BusinessLogic.Models;
using net.jancerveny.sofaking.DataLayer.Models;
using net.jancerveny.sofaking.WorkerService.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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
			public static Regex AllowedFileTypes => new Regex(@"(.+(\.mkv|\.avi|\.mp4|\.srt|\.sub))", RegexOptions.Compiled | RegexOptions.IgnoreCase);
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
			while (!stoppingToken.IsCancellationRequested)
			{

				_logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
				IReadOnlyList<ITorrentClientTorrent> torrents;

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
					_logger.LogError("Handling movies failed", ex);
					continue;
				}


				try
				{
					await HandleTorrentStatusUpdates(torrents);
				} catch(Exception ex)
				{
					_logger.LogError("Handling movies failed", ex);
				}

				try
				{
					await HandleDownloadedMovies(torrents, stoppingToken);
				}
				catch (Exception ex)
				{
					_logger.LogError("Handling downloaded and transcoded movies failed", ex);
				}

				try
				{
					await HandleQueuedTranscoding(torrents, stoppingToken);
				}
				catch (Exception ex)
				{
					_logger.LogError("Handling queued transcoding failed", ex);
				}

				await Task.Delay(10 * 1000, stoppingToken);
			}
		}

		private async Task HandleQueuedTranscoding(IReadOnlyList<ITorrentClientTorrent> torrents, CancellationToken cancellationToken)
		{
			if((await _movieService.GetMoviesAsync()).Where(x => x.Status == MovieStatusEnum.Transcoding).Any())
			{
				return;
			}

			var queuedMovieJob = (await _movieService.GetMoviesAsync()).Where(x => x.Status == MovieStatusEnum.TranscodingQueued).FirstOrDefault();
			
			if (queuedMovieJob != null)
			{
				var torrent = torrents.Where(x => x.Hash == queuedMovieJob.TorrentHash).FirstOrDefault();
				if (torrent != null)
				{
					await Transcode(queuedMovieJob.Id, torrent, cancellationToken, async () => { await SuccessFinishingActionAsync(torrent, queuedMovieJob); });
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

			if(downloadedMovies.Count() == 0)
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
						await _torrentClient.RemoveTorrent(torrent.Id);
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
				var transcodingResult = await Transcode(movieJob.Id, torrent, cancellationToken, async () => { await SuccessFinishingActionAsync(torrent, movieJob); });

				if(transcodingResult == TranscodeResultEnum.NoVideoFiles)
				{
					Directory.Delete(downloadDirectory, true);
					await _movieService.SetMovieStatus(movieJob.Id, MovieStatusEnum.NoVideoFilesError);
					await _torrentClient.RemoveTorrent(torrent.Id);
					continue;
				}

				if (transcodingResult == TranscodeResultEnum.TranscodingNotNeeded)
				{
					var finishedMovieDirectory = MovieFinishedDirectory(movieJob);
					if(!Directory.Exists(finishedMovieDirectory))
					{
						Directory.CreateDirectory(finishedMovieDirectory);
					}

					try
					{
						foreach (var videoFile in GetVideoFilesInDir(downloadDirectory))
						{
							File.Move(videoFile, Path.Combine(finishedMovieDirectory, Path.GetFileName(videoFile)));
						}
					} catch(Exception e)
					{
						await _movieService.SetMovieStatus(movieJob.Id, MovieStatusEnum.FileInUse);
						await _torrentClient.RemoveTorrent(torrent.Id);
						_logger.LogError($"Could not move the final downloaded files: {e.Message}. Is FFMPEG still running?", e);
						continue;
					}

					await SuccessFinishingActionAsync(torrent, movieJob);
				}
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
					using (var client = _clientFactory.CreateClient())
					{
						using (var result = await client.GetAsync(movie.ImageUrl))
						{
							if (result.IsSuccessStatusCode)
							{
								var content = await result.Content.ReadAsByteArrayAsync();
								await File.WriteAllBytesAsync(Path.Combine(MovieFinishedDirectory(movie), "Cover.jpg"), content);
							}
						}
					}
				}
			}
			catch (Exception ex)
			{
				_logger.LogError("Could not create a Cover image.", ex);
			}
		}

		private async Task<TranscodeResultEnum> Transcode(int movieJobId, ITorrentClientTorrent torrent, CancellationToken cancellationToken, Action finishingAction)
		{
			var sourcePath = MovieDownloadDirectory(torrent);
			var videoFiles = GetVideoFilesInDir(sourcePath);

			if(videoFiles == null || videoFiles.Length == 0)
			{
				await _movieService.SetMovieStatus(movieJobId, MovieStatusEnum.TranscodingError);
				_logger.LogWarning($"{torrent.Name} has no compatible video files.");
				return TranscodeResultEnum.NoVideoFiles;
			}

			var movie = (await _movieService.GetMoviesAsync()).Where(x => x.Id == movieJobId).FirstOrDefault();
			await _movieService.SetMovieStatus(movieJobId, MovieStatusEnum.AnalysisStarted);
			var pendingTranscodingJobs = new List<ITranscodingJob>();

			// Check if any files need transcoding
			foreach (var videoFile in videoFiles)
			{
				var mediaInfo = await _encoderService.GetMediaInfo(videoFile);
				var flags = EncodingTargetFlags.None;

				if (!HasAcceptableVideo(mediaInfo, new FileInfo(videoFile)))
				{
					flags |= EncodingTargetFlags.NeedsNewVideo;
				}

				if (!HasAcceptableAudio(mediaInfo))
				{
					flags |= EncodingTargetFlags.NeedsNewAudio;
				}

				if (movie.Genres.HasFlag(GenreFlags.Animated))
				{
					flags |= EncodingTargetFlags.VideoIsAnimation;
				}

				if(flags == EncodingTargetFlags.None)
				{
					continue;
				}

				pendingTranscodingJobs.Add(new TranscodingJob
				{
					SourceFile = videoFile,
					DestinationFolder = MovieFinishedDirectory(movie),
					Action = flags,
					OnComplete = () =>
					{
						File.Delete(videoFile);
						if (_transcodingJobs.Count == 0)
						{
							finishingAction();
						}
					},
					OnError = async () =>
					{
						await _movieService.SetMovieStatus(movieJobId, MovieStatusEnum.TranscodingError);
						if (_transcodingJobs.Count == 0)
						{
							finishingAction();
						}
					},
					CancellationToken = cancellationToken
				});
			}

			// No transcoding needed for any video files in the folder
			// Using this pendingTranscodingJobsList allows us to return for movies that need no transcoding, so they won't wait for the transcoding to be complete and can be simply copied  over to the resulting folder.
			if (!pendingTranscodingJobs.Any())
			{
				return TranscodeResultEnum.TranscodingNotNeeded;
			}

			// If something else is transcoding, then queue
			var dd = _transcodingJobs.Any();
			var ff = (await _movieService.GetMoviesAsync()).Where(x => x.Status == MovieStatusEnum.Transcoding).Any();
			if (dd || ff)
			{
				await _movieService.SetMovieStatus(movieJobId, MovieStatusEnum.TranscodingQueued);
				return TranscodeResultEnum.Queued;
			}

			// Start transcoding by copying our pending tasks into the static global queue
			await _movieService.SetMovieStatus(movieJobId, MovieStatusEnum.Transcoding);
			foreach(var transcodingJob in pendingTranscodingJobs)
			{
				_transcodingJobs.TryAdd(_transcodingJobs.IsEmpty ? 0 : _transcodingJobs.Last().Key + 1, transcodingJob);
			}

			// Get the first job from the stack, then drop it
			_ = Task.Run(async () =>
			{
				while (_transcodingJobs.TryGetValue(0, out var transcodingJob))
				{
					if (transcodingJob.SourceFile == _encoderService.CurrentFile)
					{
						continue;
					}

					try
					{
						_encoderService.StartTranscoding(transcodingJob, () =>
						{
							_transcodingJobs.TryRemove(0, out _);
							if (_transcodingJobs.Count == 0)
							{
								finishingAction();
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

			return TranscodeResultEnum.Transcoding;
		}

		private static string[] GetVideoFilesInDir(string sourcePath)
		{
			if(!Directory.Exists(sourcePath))
			{
				return null;
			}

			return Directory
				.GetFiles(sourcePath)
				.Where(x => Regexes.AllowedFileTypes.IsMatch(x) && !x.ToLower().Contains("sample"))
				.OrderByDescending(x => new FileInfo(x).Length)
				.ToArray();
		}

		private async Task SuccessFinishingActionAsync(ITorrentClientTorrent torrent, Movie movie)
		{
			await AddCoverImage(movie);
			Directory.Delete(MovieDownloadDirectory(torrent), true);
			await _movieService.SetMovieStatus(movie.Id, MovieStatusEnum.Finished);
			await _torrentClient.RemoveTorrent(torrent.Id);
		}

		private bool HasAcceptableVideo(IMediaInfo mediaInfo, FileInfo videoFile) {
			var acceptableCodec = _configuration.AcceptedVideoCodecs.Contains(mediaInfo.VideoCodec);
			var acceptableResolution = int.Parse(mediaInfo.VideoResolution.Split("x")[0]) <= int.Parse(_configuration.Resolution.Split("x")[0]);
			var acceptableSize = videoFile.Length > (_configuration.MaxPS4FileSizeGb * 1024 * 1024 * 1024);
			var acceptableBitrate = (mediaInfo.VideoBitrateKbs == null || mediaInfo.VideoBitrateKbs <= _encoderService.TargetVideoBitrateKbs);

			return acceptableCodec && acceptableResolution && acceptableSize && acceptableBitrate;
		}
		private bool HasAcceptableAudio(IMediaInfo mediaInfo) => _configuration.AcceptedAudioCodecs.Contains(mediaInfo.AudioCodec);

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
