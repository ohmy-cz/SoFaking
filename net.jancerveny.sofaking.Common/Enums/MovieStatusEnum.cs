public enum MovieStatusEnum
{
    WatchingFor,
    DownloadQueued,
    Downloading,
    DownloadingPaused,
    Downloaded,
    NoVideoFilesError,
    TranscodingQueued,
    AnalysisStarted,
    TranscodingStarted,
    TranscodingFinished,
    TranscodingError,
    Finished,
    FileInUse,
    FileNotFound,
    CouldNotCleanup,
    TranscodingIncomplete,
    TranscodingRunningTooLong, // Transcoding took more than 24 hours
    AnalysisAudioFailed,
    AnalysisVideoFailed
}