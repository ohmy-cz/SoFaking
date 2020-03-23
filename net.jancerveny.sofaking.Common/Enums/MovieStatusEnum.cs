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
    FileNotFound = 13,
    CouldNotCleanup = 14,
    TranscodingIncomplete = 15,
    TranscodingRunningTooLong = 16, // Transcoding took more than 24 hours
    AnalysisAudioFailed = 17,
    AnalysisVideoFailed = 18
}