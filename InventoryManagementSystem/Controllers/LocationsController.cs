using InventoryManagementSystem.Services;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using InventoryManagementSystem.Models;

namespace InventoryManagementSystem.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LocationsController : ControllerBase
    {
        private readonly MongoDbService _mongoDbService;

        public LocationsController(MongoDbService mongoDbService)
        {
            _mongoDbService = mongoDbService;
        }

        [HttpGet("regions")]
        public async Task<IActionResult> GetRegions()
        {
            var regions = await _mongoDbService.Regions.Find(_ => true).ToListAsync();
            return Ok(regions.OrderBy(r => r.Name));
        }

        [HttpGet("provinces/{regionCode}")]
        public async Task<IActionResult> GetProvinces(string regionCode)
        {
            if (int.TryParse(regionCode, out int regionId))
            {
                var provinces = await _mongoDbService.Provinces.Find(p => p.RegionId == regionId).ToListAsync();
                return Ok(provinces.OrderBy(p => p.Name));
            }
            return BadRequest("Invalid Region Code");
        }

        [HttpGet("municipalities/{provinceCode}")]
        public async Task<IActionResult> GetMunicipalities(string provinceCode)
        {
            if (int.TryParse(provinceCode, out int provinceId))
            {
                var municipalities = await _mongoDbService.Municipalities.Find(m => m.ProvinceId == provinceId).ToListAsync();
                return Ok(municipalities.OrderBy(m => m.Name));
            }
            return BadRequest("Invalid Province Code");
        }

        [HttpGet("barangays/{cityCode}")]
        public async Task<IActionResult> GetBarangays(string cityCode)
        {
            if (int.TryParse(cityCode, out int cityId))
            {
                var barangays = await _mongoDbService.Barangays.Find(b => b.MunicipalityId == cityId).ToListAsync();
                return Ok(barangays.OrderBy(b => b.Name));
            }
            return BadRequest("Invalid City Code");
        }
    }
}