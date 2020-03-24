public enum MovieStatusEnum
{
    WatchingFor = 0,
    DownloadQueued = 1,
    Downloading = 2,
    DownloadingPaused = 3,
    Downloaded = 4,
    NoVideoFilesError = 5,
    TranscodingQueued = 6,
    AnalysisStarted = 7,
    TranscodingStarted = 8,
    TranscodingFinished = 9,
    TranscodingError = 10,
    Finished = 11,
    FileInUse = 12,
    FileNotFound = 13, // The download directory or downloaded file missing
    CouldNotDeleteDownloadDirectory = 14,
    TranscodingIncomplete = 15, // After restarting the client
    TranscodingRunningTooLong = 16, // Transcoding took more than 24 hours
    AnalysisAudioFailed = 17, // FFMPEG returned an empty string or threw an error
    AnalysisVideoFailed = 18, // FFMPEG returned an empty string or threw an error
    TorrentNotFound = 19 // Torrent has been removed from Transmission
}