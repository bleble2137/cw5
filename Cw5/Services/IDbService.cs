using System.Collections.Generic;
using System.Threading.Tasks;


namespace Cw5
{
    public interface IDbService
    {

        public Task<int> AddWarehouse(Warehouse warehouse);
    }
}
