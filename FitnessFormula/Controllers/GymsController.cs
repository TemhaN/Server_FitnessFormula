using FitnessFormula.Data;
using FitnessFormula.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FitnessFormula.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class GymsController : ControllerBase
    {
        private readonly FitnessDbContext _context;

        public GymsController(FitnessDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Gym>>> GetGyms()
        {
            var gyms = await _context.Gyms
                .Select(g => new
                {
                    g.GymId,
                    g.GymName,
                    g.Address
                })
                .ToListAsync();

            return Ok(gyms);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Gym>> GetGym(int id)
        {
            var gym = await _context.Gyms
                .Where(g => g.GymId == id)
                .Select(g => new
                {
                    g.GymId,
                    g.GymName,
                    g.Address
                })
                .FirstOrDefaultAsync();

            if (gym == null)
            {
                return NotFound(new { message = $"Спортзал с ID {id} не найден." });
            }

            return Ok(gym);
        }
    }
}