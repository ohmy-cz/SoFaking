using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using net.jancerveny.sofaking.BusinessLogic;
using net.jancerveny.sofaking.BusinessLogic.Interfaces;
using net.jancerveny.sofaking.BusinessLogic.Models;
using net.jancerveny.sofaking.Common.Constants;
using net.jancerveny.sofaking.Common.Models;
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
	public class SoFakingWorker : BackgroundService
	{
		private static class Regexes
		{
			public static Regex FileSystemSafeName => new Regex(@"[^\sa-z0-9_-ěëéèščřžýůüáæäåøöíï]+", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline);
			public static Regex VideoFileTypes => new Regex(@"(.+(\.mkv|\.avi|\.mp4))", RegexOptions.Compiled | RegexOptions.IgnoreCase);
			public static Regex FileNamePattern => new Regex(@"^(?<FileName>.+)(?<FileExtension>\.[a-z]{3})$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
		}
		private readonly IHttpClientFactory _clientFactory;
		private readonly ILogger<SoFakingWorker> _logger;
		private readonly ILogger<FFMPEGEncoderService> _loggerEnc;
		private readonly DownloadFinishedWorkerConfiguration _configuration;
		private readonly SoFakingConfiguration _sofakingConfiguration;
		private readonly EncoderConfiguration _encoderConfiguration;
		private readonly MovieService _movieService;
		private readonly ITorrentClientService _torrentClient;
		protected static IEncoderService _encoderTranscodingInstance;
		private static ConcurrentDictionary<int, ITranscodingJob> _transcodingJobs = new ConcurrentDictionary<int, ITranscodingJob>();

		public SoFakingWorker(ILogger<SoFakingWorker> logger, ILogger<FFMPEGEncoderService> loggerEnc, IHttpClientFactory clientFactory, DownloadFinishedWorkerConfiguration configuration, MovieService movieService, ITorrentClientService torrentClient,/* IEncoderService encoderService,*/ SoFakingConfiguration sofakingConfiguration, EncoderConfiguration encoderConfiguration)
		{
			if (clientFactory == null) throw new ArgumentNullException(nameof(clientFactory));
			if (movieService == null) throw new ArgumentNullException(nameof(movieService));
			if (configuration == null) throw new ArgumentNullException(nameof(configuration));
			if (logger == null) throw new ArgumentNullException(nameof(logger));
			if (loggerEnc == null) throw new ArgumentNullException(nameof(loggerEnc));
			if (torrentClient == null) throw new ArgumentNullException(nameof(torrentClient));
			if (encoderConfiguration == null) throw new ArgumentNullException(nameof(encoderConfiguration));
			if (sofakingConfiguration == null) throw new ArgumentNullException(nameof(sofakingConfiguration));
			_sofakingConfiguration = sofakingConfiguration;
			_clientFactory = clientFactory;
			_movieService = movieService;
			_configuration = configuration;
			_logger = logger;
			_loggerEnc = loggerEnc;
			_torrentClient = torrentClient;
			_encoderConfiguration = encoderConfiguration;
		}

		protected override async Task ExecuteAsync(CancellationToken stoppingToken)
		{
			try
			{
				await HandleCrashedTranscoding();
			}
			catch (Exception ex)
			{
				var st = new StackTrace(ex, true);
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
				_logger.LogInformation($"Transcoding: {_encoderTranscodingInstance?.CurrentFile ?? "Nothing"}");
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

				await Task.Delay(45 * 1000, stoppingToken);
			}
		}

		/// <summary>
		/// Cleanup  method to be called after succesfull download, transcoding and manipulation of files.
		/// </summary>
		protected async Task MovieDownloadedSuccesfulyAsync(ITorrentClientTorrent torrent, Movie movie)
		{
			try
			{
				_logger.LogDebug($"Will delete: {MovieDownloadDirectory(torrent)}");
#if RELEASE
				Directory.Delete(MovieDownloadDirectory(torrent), true);
#endif
			}
			catch (IOException _)
			{
				await _movieService.SetMovieStatus(movie.Id, MovieStatusEnum.CouldNotDeleteDownloadDirectory);
			}
			_logger.LogDebug($"Will remove torrent: {torrent.Id}");
#if RELEASE
			await _torrentClient.RemoveTorrent(torrent.Id);
#endif
			await _movieService.SetMovieStatus(movie.Id, MovieStatusEnum.Finished);

			// Inform Minidlna or other services about the new download.
			if (!string.IsNullOrWhiteSpace(_configuration.FinishedCommandExecutable))
			{
				var process = new Process()
				{
					StartInfo = new ProcessStartInfo
					{
						FileName = _configuration.FinishedCommandExecutable,
						Arguments = _configuration.FinishedCommandArguments,
						UseShellExecute = false,
						CreateNoWindow = true
					}
				};

				await Task.Run(() =>
				{
					process.Start();
					process.WaitForExit();
				});
			}
		}

		private async Task HandleQueuedTranscoding(IReadOnlyList<ITorrentClientTorrent> torrents, CancellationToken cancellationToken)
		{
			if(_encoderTranscodingInstance != null)
			{
				_logger.LogInformation($"Skipping queued jobs: Transcoding of {_encoderTranscodingInstance.CurrentFile} running.");
				return;
			}

			var queuedMovieJob = (await _movieService.GetMoviesAsync())
				.Where(x => 
					x.Status == MovieStatusEnum.TranscodingQueued || 
					x.Status == MovieStatusEnum.TranscodingIncomplete)
				.OrderByDescending(x => x.Status == MovieStatusEnum.TranscodingQueued)
				.FirstOrDefault();


			if (queuedMovieJob == null)
			{
				_logger.LogInformation($"No jobs in the queue.");
				return;
			}

			var torrent = torrents.Where(x => x.Hash == queuedMovieJob.TorrentHash).FirstOrDefault();
			if (torrent == null)
			{
				// TODO: Find the next working queued job immediately. Now it waits for the next loop iteration.
				// TODO: Make Torrent optional at this point?
				await _movieService.SetMovieStatus(queuedMovieJob.Id, MovieStatusEnum.TorrentNotFound);
				_logger.LogWarning($"Could not find a matching torrent for job id {queuedMovieJob.Id}: {queuedMovieJob.TorrentName}");
				return;
			}

			_logger.LogInformation($"Picking up {(queuedMovieJob.Status == MovieStatusEnum.TranscodingIncomplete ? "an incomplete" : "a queued")} job id {queuedMovieJob.Id}: {queuedMovieJob.TorrentName}");
			_ = await Transcode(queuedMovieJob.Id, torrent, cancellationToken);
		}

		private async Task HandleCrashedTranscoding()
		{
			var movieJobs = (await _movieService.GetMoviesAsync()).Where(x => x.Status == MovieStatusEnum.TranscodingStarted || x.Status == MovieStatusEnum.AnalysisStarted);
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

				_logger.LogInformation($"Found a downloaded movie {movieJob.TorrentName}");

				// Move the file in its own folder, if the torrent was a single file.
				var downloadDirectory = MovieDownloadDirectory(torrent, out string torrentFileNameExtension);

				if (!string.IsNullOrWhiteSpace(torrentFileNameExtension))
				{
					_logger.LogInformation($"Moving downloaded file to its own directory {torrent}");

					var downloadFile = Path.Combine(_configuration.MoviesDownloadDir, torrent.Name);
					if (!Directory.Exists(downloadDirectory) && !File.Exists(downloadFile))
					{
						await _movieService.SetMovieStatus(movieJob.Id, MovieStatusEnum.FileNotFound);
						_logger.LogDebug($"Will delete torrent: {torrent.Id}");
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
						File.Move(downloadFile, Path.Combine(downloadDirectory, Regexes.FileSystemSafeName.Replace($"{movieJob.Year} {movieJob.Title}", string.Empty) + torrentFileNameExtension), true);
					}
				}

				// Move only the video files if no transcoding was necessary.
				var transcodingResult = await Transcode(movieJob.Id, torrent, cancellationToken);

				if(transcodingResult.Result == TranscodeResultEnum.NoVideoFiles)
				{
					_logger.LogDebug($"Will delete: {downloadDirectory}");
#if RELEASE
					Directory.Delete(downloadDirectory, true);
					await _torrentClient.RemoveTorrent(torrent.Id);
#endif
					await _movieService.SetMovieStatus(movieJob.Id, MovieStatusEnum.NoVideoFilesError);
					continue;
				}

				if (transcodingResult.Result == TranscodeResultEnum.TranscodingNotNeeded)
				{
					if (transcodingResult?.FilesToMove?.Length > 0)
					{
						await MoveVideoFilesToFinishedDir(movieJob, torrent, transcodingResult.FilesToMove);
					}
					await MovieDownloadedSuccesfulyAsync(torrent, movieJob);
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
				IMediaInfo mediaInfo = null;
				_logger.LogInformation($"Analyzing {Path.GetFileName(videoFile)}");
				try
				{
					using (var encoder = new FFMPEGEncoderService(_loggerEnc, _encoderConfiguration, _sofakingConfiguration))
					{
						mediaInfo = await encoder.GetMediaInfo(videoFile);
					}
				}
				catch (Exception ex){
					_logger.LogError($"{nameof(FFMPEGEncoderService.GetMediaInfo)} failed with: {ex.Message}", ex);
				}

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
					_logger.LogDebug($"Video file {videoFile} doesn't need transcoding, adding to files to move.");
					filesToMove.Add(videoFile);
					continue;
				}

				_logger.LogDebug($"Adding {videoFile} to transcoding jobs.");
				pendingTranscodingJobs.Add(new TranscodingJob
				{
					SourceFile = videoFile,
					Action = flags,
					Duration = mediaInfo.Duration,
					CancellationToken = cancellationToken,
					Metadata = new Dictionary<FFMPEGMetadataEnum, string> {
						{ FFMPEGMetadataEnum.title, movie.Title },
						{ FFMPEGMetadataEnum.year, movie.Year.ToString() },
						{ FFMPEGMetadataEnum.director, movie.Director },
						{ FFMPEGMetadataEnum.description, movie.Description },
						{ FFMPEGMetadataEnum.episode_id, movie.EpisodeId },
						{ FFMPEGMetadataEnum.IMDBRating, movie.ImdbScore.ToString() },
						{ FFMPEGMetadataEnum.genre, movie.Genres.ToString() },
						{ FFMPEGMetadataEnum.show, movie.Show }
					}
				});
			}

			// No transcoding needed for any video files in the folder
			// Using this pendingTranscodingJobsList allows us to return for movies that need no transcoding, so they won't wait for the transcoding to be complete and can be simply copied  over to the resulting folder.
			if (!pendingTranscodingJobs.Any())
			{
				_logger.LogDebug($"Transcoding not needed, no pending jobs.");
				return new TranscodeResult(TranscodeResultEnum.TranscodingNotNeeded, filesToMove.ToArray());
			}

			// If something else is transcoding, then queue
			var tjmem = _transcodingJobs.Any();
			var tjdb = (await _movieService.GetMoviesAsync()).Where(x => x.Status == MovieStatusEnum.TranscodingStarted).Any();
			if (tjmem || tjdb)
			{
				_logger.LogDebug($"Queuing {movie.TorrentName}");
				await _movieService.SetMovieStatus(movieJobId, MovieStatusEnum.TranscodingQueued);
				return new TranscodeResult(TranscodeResultEnum.Queued);
			}

			// Set the a cover image for the first file (the largest = the movie), so it will be attached to the output file.
			string coverImageJpg = null;
			try
			{
				coverImageJpg = Path.Combine(_sofakingConfiguration.TempFolder, movie.Id + "-Cover.jpg");
				await Download.GetFile(movie.ImageUrl, coverImageJpg);
				pendingTranscodingJobs[0].CoverImageJpg = coverImageJpg;
			}
			catch (Exception ex)
			{
				coverImageJpg = null;
				_logger.LogError($"Could not download a Cover image. {ex.Message}", ex);
			}

			// Start transcoding by copying our pending tasks into the static global queue
			_logger.LogDebug($"Setting movie status to TranscodingStarted.");
			await _movieService.SetMovieStatus(movieJobId, MovieStatusEnum.TranscodingStarted);
			foreach(var transcodingJob in pendingTranscodingJobs)
			{
				int attempts = 0;
				bool success = false;
				while(attempts <= 10)
				{
					var id = _transcodingJobs.IsEmpty ? 0 : _transcodingJobs.Select(x => x.Key).Max() + 1;
					_logger.LogDebug($"Trying to add transconding job {id} to the global queue");
					if (_transcodingJobs.TryAdd(id, transcodingJob))
					{
						success = true;
						break;
					}

					attempts++;
					Thread.Sleep(TimeSpan.FromSeconds(3));
				}

				if(!success)
				{
					_logger.LogError($"Couldn't add transcoding job {transcodingJob.SourceFile} to the global queue.");
				}
			}

			// Get the first job from the stack, then drop it when done
			_ = Task.Run(async () =>
			{
				while (_transcodingJobs.Any() && _transcodingJobs.TryGetValue(_transcodingJobs.First().Key, out var transcodingJob))
				{
					if(_encoderTranscodingInstance != null)
					{
						continue;
					}

					try
					{
						// Do this as the first thing, so no other encoding gets started
						_encoderTranscodingInstance = new FFMPEGEncoderService(_loggerEnc, _encoderConfiguration, _sofakingConfiguration);
						_logger.LogDebug($"Preparing transcoding of {transcodingJob.SourceFile}");

						Action FirstOut = () => {
							if (!_transcodingJobs.Any())
							{
								_logger.LogDebug("No transcoding jobs left to remove");
								return;
							}

							var removed = _transcodingJobs.TryRemove(_transcodingJobs.First().Key, out _);
							_logger.LogWarning($"Removing first from the queue, result: {removed}.");
						};

						// TODO: Use a factory
						_encoderTranscodingInstance.OnStart += (object sender, EventArgs e) => {
							_logger.LogDebug("Transcoding started");
						};

						_encoderTranscodingInstance.OnProgress += (object sender, EncodingProgressEventArgs e) => {
							_logger.LogDebug($"Transcoding progress: {e.ProgressPercent:0.##}%");
						};

						_encoderTranscodingInstance.OnError += (object sender, EncodingErrorEventArgs e) => {
							FirstOut();
							_encoderTranscodingInstance.Dispose();
							_encoderTranscodingInstance = null;

							_logger.LogDebug($"Transcoding failed: {e.Error}");
						};

						_encoderTranscodingInstance.OnCancelled += (object sender, EventArgs e) => {
							FirstOut();
							_encoderTranscodingInstance.Dispose();
							_encoderTranscodingInstance = null;

							_logger.LogDebug("Transcoding cancelled");
						};

						_encoderTranscodingInstance.OnSuccess += async (object sender, EncodingSuccessEventArgs e) => {
							FirstOut();

							_logger.LogWarning($"Adding {e.FinishedFile} to the list of files to move ({filesToMove.Count()})");
							filesToMove.Add(e.FinishedFile);

							if (_transcodingJobs.Count == 0)
							{
								_logger.LogWarning("All transcoding done.");
								await MoveVideoFilesToFinishedDir(movie, torrent, filesToMove.ToArray(), coverImageJpg);
								await MovieDownloadedSuccesfulyAsync(torrent, movie);
							}

							// Do this as the last thing, so no other encoding gets started
							_encoderTranscodingInstance.Dispose();
							_encoderTranscodingInstance = null;
						};

						_logger.LogWarning($"Starting transcoding of {transcodingJob.SourceFile}");
						await _encoderTranscodingInstance.StartTranscodingAsync(transcodingJob, cancellationToken);
					}
					catch (Exception e)
					{
						if (_transcodingJobs.Any())
						{
							var removed = _transcodingJobs.TryRemove(_transcodingJobs.First().Key, out _);
							_logger.LogWarning($"Removing first from the queue, result: {removed}.");
						}

						await _movieService.SetMovieStatus(movieJobId, MovieStatusEnum.TranscodingError);
						_logger.LogError(e.Message);

						_encoderTranscodingInstance.DisposeAndKeepFiles();
						_encoderTranscodingInstance = null;
					}
				}
			});

			return new TranscodeResult(TranscodeResultEnum.Transcoding, filesToMove.ToArray());
		}

		private async Task MoveVideoFilesToFinishedDir(Movie movie, ITorrentClientTorrent torrent, string[] videoFilesToMove, string coverImageJpg = null)
		{
			if (movie == null) throw new ArgumentNullException(nameof(movie));
			if (torrent == null) throw new ArgumentNullException(nameof(torrent));
			if (videoFilesToMove == null || !videoFilesToMove.Any())
			{
				_logger.LogError($"Cannot move video files: {nameof(videoFilesToMove)} was empty.");
				throw new ArgumentNullException(nameof(videoFilesToMove));
			}

			_logger.LogWarning($"Moving video files:\n{string.Join("\n ", videoFilesToMove)}");

			var finishedMovieDirectory = MovieFinishedDirectory(movie);
			if (!Directory.Exists(finishedMovieDirectory))
			{
				Directory.CreateDirectory(finishedMovieDirectory);
			}

			// Move existing Cover image, or download a new one if null.
			var finishedCoverImageJpg = Path.Combine(finishedMovieDirectory, "Cover.jpg");

			try
			{
				if (!File.Exists(finishedCoverImageJpg))
				{
					if (coverImageJpg != null && File.Exists(coverImageJpg))
					{
						File.Move(coverImageJpg, finishedCoverImageJpg);
					}
					else
					{
						try
						{
							await Download.GetFile(movie.ImageUrl, finishedCoverImageJpg);
						}
						catch (Exception ex)
						{
							finishedCoverImageJpg = null;
							_logger.LogError($"Could not create a Cover image. {ex.Message}", ex);
						}
					}

					if (finishedCoverImageJpg != null)
					{
						await WindowsFolder.SetFolderPictureAsync(finishedCoverImageJpg);
						File.SetAttributes(finishedCoverImageJpg, File.GetAttributes(finishedCoverImageJpg) | FileAttributes.Hidden);
					}
				}
			}
			catch (Exception e)
			{
				_logger.LogInformation($"Could create a cover image: {e.Message}.", e);
			}

			try
			{
				// Here we're assuming that the  first, largest file will be the main movie file.
				var mainMovieFile = videoFilesToMove[0];
				var destinationFileNameWithoutExtension = Regexes.FileSystemSafeName.Replace($"{movie.Year} {movie.Title}", string.Empty);
				_logger.LogWarning($"Moving MAIN movie file: {mainMovieFile} to {Path.Combine(finishedMovieDirectory, destinationFileNameWithoutExtension + Regexes.FileNamePattern.Match(mainMovieFile).Groups["FileExtension"].Value)}");
				File.Move(mainMovieFile, Path.Combine(finishedMovieDirectory, destinationFileNameWithoutExtension + Regexes.FileNamePattern.Match(mainMovieFile).Groups["FileExtension"].Value));

				if (videoFilesToMove.Length > 1)
				{
					foreach (var videoFile in videoFilesToMove.Skip(1))
					{
						_logger.LogInformation($"Moving video file: {videoFile} to {Path.Combine(finishedMovieDirectory, Path.GetFileName(videoFile))}");
						File.Move(videoFile, Path.Combine(finishedMovieDirectory, Path.GetFileName(videoFile)));
					}
				}
			}
			catch (Exception e)
			{
				await _movieService.SetMovieStatus(movie.Id, MovieStatusEnum.FileInUse);
				_logger.LogDebug($"Will delete torrent: {torrent.Id}");
#if RELEASE
				await _torrentClient.RemoveTorrent(torrent.Id);
#endif
				_logger.LogInformation($"Could not move the final downloaded files: {e.Message}. Is FFMPEG still running?", e);
			}
		}

		/// <summary>
		/// Get all video files in a directory, from the largest to the smallest. Skips banned files.
		/// </summary>
		private static string[] GetVideoFilesInDir(string sourcePath)
		{
			if(!Directory.Exists(sourcePath))
			{
				return null;
			}

			return Directory
				.GetFiles(sourcePath)
				.Where(x => Regexes.VideoFileTypes.IsMatch(Path.GetFileName(x)) && !BannedVideoFiles.Tokens.Where(y => Path.GetFileName(x).ToLower().Contains(y)).Any())
				.OrderByDescending(x => new FileInfo(x).Length)
				.ToArray();
		}

		private bool HasAcceptableVideo(IMediaInfo mediaInfo)
		{
			if (_configuration.AcceptedVideoCodecs == null) throw new ArgumentNullException(nameof(_configuration.AcceptedVideoCodecs));
			if (mediaInfo == null) throw new ArgumentNullException(nameof(mediaInfo));
			if (mediaInfo.VideoCodec == null) throw new ArgumentNullException(nameof(mediaInfo.VideoCodec));
			if (mediaInfo.FileInfo == null) throw new ArgumentNullException(nameof(mediaInfo.FileInfo));
			if (mediaInfo.HorizontalVideoResolution == -1) throw new ArgumentException($"Horizontal resolution invalid: {nameof(mediaInfo.HorizontalVideoResolution)}");

			var acceptableCodec = false;
			foreach (var vc in _configuration.AcceptedVideoCodecs)
			{
				if (mediaInfo.VideoCodec.IndexOf(vc) >= 0)
				{
					acceptableCodec = true;
					break;
				}
			}

			var acceptableResolution = mediaInfo.HorizontalVideoResolution <= _sofakingConfiguration.MaxHorizontalVideoResolution;
			var acceptableSize = mediaInfo.FileInfo.Length > (_sofakingConfiguration.MaxSizeGb * 1024 * 1024);
			// TODO: Fixing getting the video bitrate right would speed up the program significantly.
			// Unfortunately, FFMPEG can't return bitrate of only the video stream. So we will ONLY stream copy if video and all the audio streams combined have a lower bitrate than level 4.2 h264 video bitrate compatible with PS4 (6,25Mbit/s)
			var acceptableBitrate = (mediaInfo.AVBitrateKbs == null || mediaInfo.AVBitrateKbs <= TargetVideoBitrateKbs);

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
				torrentFileNameExtension = fileName.Groups["FileExtension"].Value;
			}

			return MovieDownloadDirectory(torrent);
		}

		private string MovieDownloadDirectory(ITorrentClientTorrent torrent)
		{
			var fileName = Regexes.FileNamePattern.Match(torrent.Name);
			return Path.Combine(_configuration.MoviesDownloadDir, fileName.Success ? fileName.Groups["FileName"].Value : torrent.Name);
		}

		private string MovieFinishedDirectory(Movie movie) => Path.Combine(_configuration.MoviesFinishedDir, Regexes.FileSystemSafeName.Replace(movie.Title, string.Empty));

		private int TargetVideoBitrateKbs => (int)(_encoderConfiguration.OutputVideoBitrateMbits) * 1024;

		/// <summary>
		/// Compound bitrate for video and audio
		/// </summary>
		private int TargetAVBitrateKbs => (int)(_encoderConfiguration.OutputVideoBitrateMbits + _encoderConfiguration.OutputAudioBitrateMbits) * 1024;
	}
}