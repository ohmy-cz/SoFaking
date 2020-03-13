using System.Collections.Generic;
using System.Threading.Tasks;

namespace net.jancerveny.sofaking.BusinessLogic.Interfaces
{
    public interface IVerifiedMovieSearchService
    {
        Task<IReadOnlyCollection<IVerifiedMovie>> Search(string query);
    }
}
