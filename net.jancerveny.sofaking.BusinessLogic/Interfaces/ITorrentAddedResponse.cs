namespace net.jancerveny.sofaking.BusinessLogic.Interfaces
{
    public interface ITorrentAddedResponse
    {
        int TorrentId { get; set; }
        string Hash { get; set; }
    }
}
