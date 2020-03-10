using System.Threading.Tasks;

namespace net.jancerveny.sofaking.BusinessLogic.Interfaces
{
    public interface ITorrentClientService
    {
        Task<ITorrentAddedResponse> AddTorrentAsync(string magnetLink);
    }
}
