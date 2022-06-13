using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace Cw5.Controllers
{
    [Route("/api/warehouses")]
    [ApiController]
    public class WarehousesController : ControllerBase
    {
        private readonly IDbService dbService;
        public WarehousesController(IDbService dbService)
        {
            this.dbService = dbService;
        }

        [HttpPost]
        public async Task<IActionResult> AddWarehouseAsync(Warehouse warehouse)
        {
            int id = -1;
            try
            {
                id = await dbService.AddWarehouse(warehouse);
            }
            catch(Exception e)
            {
                return BadRequest(e.Message);
            }
            return Ok(id);
        }
    }
}
