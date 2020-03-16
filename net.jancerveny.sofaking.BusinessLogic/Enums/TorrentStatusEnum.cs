public enum TorrentStatusEnum
{
    // https://github.com/transmission/transmission/blob/master/libtransmission/transmission.h
    Stopped = 0, /* Torrent is stopped */
    CheckWait = 1, /* Queued to check files */
    Check = 2, /* Checking files */
    DownloadWait = 3, /* Queued to download */
    Download = 4, /* Downloading */
    SeedWait = 5, /* Queued to seed */
    Seed = 6 /* Seeding */
}
