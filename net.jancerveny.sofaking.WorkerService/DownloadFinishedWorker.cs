using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using net.jancerveny.sofaking.BusinessLogic;
using net.jancerveny.sofaking.BusinessLogic.Interfaces;
using net.jancerveny.sofaking.WorkerService.Models;
using System;
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
			public static Regex AllowedFiles => new Regex(@"(.+(\.mkv|\.avi|\.mp4|\.srt|\.sub))", RegexOptions.Compiled | RegexOptions.IgnoreCase);
			public static Regex TranscodedFileNamePattern => new Regex(@"(.+)(\.[a-z]{3})", RegexOptions.Compiled | RegexOptions.IgnoreCase);
		}
		private readonly IHttpClientFactory _clientFactory;
		private readonly ILogger<DownloadFinishedWorker> _logger;
		private readonly DownloadFinishedWorkerConfiguration _configuration;
		private readonly MovieService _movieService;
		private readonly ITorrentClientService _torrentClient;
		private readonly IEncoderService _encoderService;

		public DownloadFinishedWorker(ILogger<DownloadFinishedWorker> logger, IHttpClientFactory clientFactory, DownloadFinishedWorkerConfiguration configuration, MovieService movieService, ITorrentClientService torrentClient, IEncoderService encoderService)
		{
			if (clientFactory == null) throw new ArgumentNullException(nameof(clientFactory));
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
				await Task.Delay(10 * 1000, stoppingToken);

				_logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
				IReadOnlyList<ITorrentClientTorrent> torrents = null;

				try
				{
					torrents = await _torrentClient.GetAllTorrents();

					if (torrents.Count() == 0)
					{
						continue;
					}

					_logger.LogInformation("Found following torrents:\r\n" + string.Join("\r\n", torrents.Select(x => x.Name)));
				}  catch(Exception ex)
				{
					_logger.LogError("Handling movies failed", ex);
					continue;
				}
				
				try
				{
					await HandleDownloadingMovies(torrents);
				} catch(Exception ex)
				{
					_logger.LogError("Handling movies failed", ex);
				}

				try
				{
					await HandleDownloadedAndTranscodedMovies(torrents, stoppingToken);
				}
				catch (Exception ex)
				{
					_logger.LogError("Handling downloaded and transcoded movies failed", ex);
				}

				try
				{
					await StartQueuedTranscoding(torrents, stoppingToken);
				}
				catch (Exception ex)
				{
					_logger.LogError("Handling queued transcoding failed", ex);
				}
			}
		}

		private async Task StartQueuedTranscoding(IReadOnlyList<ITorrentClientTorrent> torrents, CancellationToken cancellationToken)
		{
			if(_movieService.GetMovies().Where(x => x.Status == MovieStatusEnum.Transcoding).Any())
			{
				return;
			}
			var queuedJob = _movieService.GetMovies().Where(x => x.Status == MovieStatusEnum.TranscodingQueued).FirstOrDefault();
			
			if (queuedJob != null)
			{
				var torrent = torrents.Where(x => x.Hash == queuedJob.TorrentHash).FirstOrDefault();
				if (torrent != null)
				{
					await StartTranscoding(queuedJob.Id, torrent, cancellationToken);
				}
			}
		}

		private async Task HandleDownloadedAndTranscodedMovies(IReadOnlyList<ITorrentClientTorrent> torrents, CancellationToken cancellationToken)
		{
			var downloadedMovies = _movieService.GetMovies()
				.Where(x =>
					x.Deleted == null &&
					(x.Status == MovieStatusEnum.Downloaded || x.Status == MovieStatusEnum.TranscodingFinished)
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

				var path = Path.Combine(_configuration.FinishedDownloadsDir, torrent.Name);

				// TODO: It *can* be a single file.
				if (Directory.Exists(path))
				{
					// Make sure we don't encode twice
					if (movieJob.Status == MovieStatusEnum.Downloaded && await MediaFileNeedsTranscoding(GetVideoFile(path)))
					{
						await QueueOrStartTranscoding(movieJob.Id, torrent, cancellationToken);
						return;
					}

					await CleanUpAndMove(path, movieJob.Title, movieJob.ImageUrl, cancellationToken);
				}

				await _movieService.SetMovieStatus(movieJob.Id, MovieStatusEnum.Finished);
				await _torrentClient.RemoveTorrent(torrent.Id);
			}
		}

		private async Task HandleDownloadingMovies(IReadOnlyList<ITorrentClientTorrent> torrents)
		{
			var downloadingMovies = _movieService.GetMovies()
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

				var path = Path.Combine(_configuration.FinishedDownloadsDir, torrent.Name);
				if (Directory.Exists(path) || File.Exists(path))
				{
					await _movieService.SetMovieStatus(movieJob.Id, MovieStatusEnum.Downloaded);
				}
			}
		}

		//private async Task HandleTranscodingMovies(IReadOnlyList<ITorrentClientTorrent> torrents, CancellationToken cancellationToken)
		//{
		//	var transcodingMovies = _movieService.GetMovies()
		//			.Where(x =>
		//				x.Deleted == null &&
		//				(x.Status == MovieStatusEnum.TranscodingFinished)
		//			);

		//	foreach (var torrent in torrents)
		//	{
		//		var movieJob = transcodingMovies.Where(x => x.TorrentClientTorrentId == torrent.Id).FirstOrDefault();
		//		if (movieJob == null)
		//		{
		//			continue;
		//		}

		//		var path = Path.Combine(_configuration.FinishedDownloadsDir, torrent.Name);

		//		if (File.Exists(path))
		//		{
		//			//  Transcoding finished!
		//			await _movieService.SetMovieStatus(movieJob.Id, MovieStatusEnum.TranscodingFinished);

		//			if (await CleanUpAndMove(path, movieJob.Title, movieJob.ImageUrl, cancellationToken))
		//			{
		//				await _movieService.SetMovieStatus(movieJob.Id, MovieStatusEnum.Finished);
		//			}
		//		}
		//	}
		//}

		private async Task<bool> CleanUpAndMove(string sourcePath, string movieName, string coverImageUrl, CancellationToken cancellationToken)
		{
			return await Task.Run(async () =>
			{
				var sanitizedMovieName = Regexes.FileSystemSafeName.Replace(movieName, string.Empty);
				if (string.IsNullOrWhiteSpace(sanitizedMovieName))
				{
					_logger.LogError($"Could not move {sourcePath} to {_configuration.FinishedDir}/{sanitizedMovieName}, because the latter is not a valid file name.");
					return false;
				}

				// Clean up files that don't belong in this folder
				if (Directory.Exists(sourcePath))
				{
					try
					{
						foreach (var unknownFile in Directory.GetFiles(sourcePath).Where(x => !Regexes.AllowedFiles.IsMatch(x)))
						{
							File.Delete(unknownFile);
						}

						foreach (var unknownDirectory in Directory.GetDirectories(sourcePath))
						{
							Directory.Delete(unknownDirectory, true);
						}
					}
					catch (Exception ex)
					{
						_logger.LogError("Could not clean up unknown files", ex);
					}
				}

				// Move cleaned files
				var destinationPath = Path.Combine(_configuration.FinishedDir, sanitizedMovieName);
				MoveAllRecursively(sourcePath, destinationPath);
				if (Directory.Exists(sourcePath))
				{
					Directory.Delete(sourcePath);
				}

				// Add a Cover image
				try
				{
					if (!string.IsNullOrWhiteSpace(coverImageUrl))
					{
						using (var client = _clientFactory.CreateClient())
						{
							using (var result = await client.GetAsync(coverImageUrl))
							{
								if (result.IsSuccessStatusCode)
								{
									var content = await result.Content.ReadAsByteArrayAsync();
									await File.WriteAllBytesAsync(Path.Combine(destinationPath, "Cover.jpg"), content, cancellationToken);
								}
							}
						}
					}
				}
				catch (Exception ex) {
					_logger.LogError("Could not create a Cover image.", ex);
				}

				// Move to the destination
				// If sourcePath is a folder, it should copy its contents over to the destination folder.
				// If sourcePath is a file, it should copy itself to the desitnation folder.
				//if(File.Exists(sourcePath))
				//{
				//	destinationPath += sanitizedMovieName + Regexes.TranscodedFileNamePattern.Match(sourcePath).Groups[2].Value;
				//}
				return true;
			});
		}

		private bool HasAcceptableVideo(IMediaInfo mediaInfo, FileInfo videoFile) => _configuration.AcceptedVideoCodecs.Contains(mediaInfo.VideoCodec) && videoFile.Length <= (_configuration.MaxPS4FileSizeGb * 1024 * 1024 * 1024);
		private bool HasAcceptableAudio(IMediaInfo mediaInfo) => _configuration.AcceptedAudioCodecs.Contains(mediaInfo.AudioCodec);

		private async Task<bool> MediaFileNeedsTranscoding(string videoFile)
		{
			var mediaInfo = await _encoderService.GetMediaInfo(videoFile);
			
			if(HasAcceptableVideo(mediaInfo, new FileInfo(videoFile)) && HasAcceptableAudio(mediaInfo))
			{
				return true;
			}

			return false;
		}

		private async Task StartTranscoding(int movieJobId, ITorrentClientTorrent torrent, CancellationToken cancellationToken)
		{
			var sourcePath = Path.Combine(_configuration.FinishedDownloadsDir, torrent.Name);
			var transcodingTempPath = _configuration.TranscodingTempDir;
			await _movieService.SetMovieStatus(movieJobId, MovieStatusEnum.TranscodingStarted);

			var videoFile = GetVideoFile(sourcePath);
			var transcodingFinishedPath = GetTranscodedFileName(videoFile);
			var mediaInfo = await _encoderService.GetMediaInfo(videoFile);
			var flags = EncodingTargetFlags.None;
			if (!HasAcceptableVideo(mediaInfo, new FileInfo(videoFile)))
			{
				flags |= EncodingTargetFlags.Video;
			}
			if (!HasAcceptableAudio(mediaInfo))
			{
				flags |= EncodingTargetFlags.Audio;
			}

			await _movieService.SetMovieStatus(movieJobId, MovieStatusEnum.Transcoding);
			_encoderService.StartEncoding(videoFile, transcodingFinishedPath, flags, async () => { await _movieService.SetMovieStatus(movieJobId, MovieStatusEnum.TranscodingFinished); }, cancellationToken);
		}

		private async Task QueueOrStartTranscoding(int movieJobId, ITorrentClientTorrent torrent, CancellationToken cancellationToken)
		{
			if(_movieService.GetMovies().Where(x => x.Status == MovieStatusEnum.Transcoding || x.Status == MovieStatusEnum.TranscodingStarted).Any())
			{
				await _movieService.SetMovieStatus(movieJobId, MovieStatusEnum.TranscodingQueued);
				return;
			}

			await StartTranscoding(movieJobId, torrent, cancellationToken);
		}

		private static string GetTranscodedFileName(string fileName) 
		{
			var m = Regexes.TranscodedFileNamePattern.Match(fileName);
			return $"{m.Groups[1].Value}.TRANSCODED{m.Groups[2].Value}";
		}

		private static string GetVideoFile(string sourcePath)
		{
			if(!Directory.Exists(sourcePath))
			{
				throw new Exception("Folder does not exist");
			}

			return Directory
				.GetFiles(sourcePath)
				.Where(x => Regexes.AllowedFiles.IsMatch(x))
				.OrderByDescending(x => new FileInfo(x).Length)
				.FirstOrDefault();
		}

		private static void MoveAllRecursively(string sourcePath, string destinationPath)
		{
			// Source path is a file
			if(File.Exists(sourcePath))
			{
				if(!Directory.Exists(destinationPath))
				{
					Directory.CreateDirectory(destinationPath);
				}

				File.Move(sourcePath, Path.Combine(destinationPath, Path.GetFileName(sourcePath)));
				return;
			}

			if (!Directory.Exists(destinationPath))
			{
				Directory.Move(sourcePath, destinationPath);
			} else
			{
				foreach (var subSourcePath in Directory.GetFileSystemEntries(sourcePath))
				{
					//Path.GetDirectoryName(subSourcePath)
					//var destinationDir = Path.Combine(destinationPath, );
					var subDestinationPath = destinationPath;
					if(Directory.Exists(subSourcePath))
					{
						subDestinationPath = Path.GetDirectoryName(subSourcePath);
					}
					MoveAllRecursively(subSourcePath, subDestinationPath);
				}
				//foreach (var subDirectory in Directory.GetDirectories(sourcePath))
				//{
				//	var destinationDir = Path.Combine(destinationPath, Path.GetDirectoryName(subDirectory));
				//	MoveAllRecursively(subDirectory, destinationDir);
				//}

				//// Move all  files in the directory
				//foreach (var file in Directory.GetFiles(sourcePath))
				//{
				//	File.Move(file, Path.Combine(destinationPath, Path.GetFileName(file)));
				//}
			}
		}
	}
}
