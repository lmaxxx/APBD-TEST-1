using APBD_TEST.Exceptions;
using APBD_TEST.Models;
using APBD_TEST.Services;
using Microsoft.AspNetCore.Mvc;

namespace APBD_TEST.Controllers;

[Route("api/[controller]")]
[ApiController]
public class VisitsController : ControllerBase
{
    private readonly IDbService _dbService;
    public VisitsController(IDbService dbService)
    {
        _dbService = dbService;
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetVisitById(int id)
    {
        try
        {
            var res = await _dbService.GetVisitById(id);
            return Ok(res);
        }
        catch(NotFoundException e)
        {
            return NotFound(e.Message);
        }
    }

    [HttpPost()]
    public async Task<IActionResult> CreateVisit([FromBody]CreateVisitDto createVisitDto)
    {
        if (!createVisitDto.services.Any()) return BadRequest("Services cannot be empty");

        try
        {
            await _dbService.CreateVisit(createVisitDto);
        }
        catch (ConflictException e)
        {
            return Conflict(e.Message);
        }
        catch (NotFoundException e)
        {
            return NotFound(e.Message);
        }
        
        return CreatedAtAction(nameof(GetVisitById), new { id = createVisitDto.visitId }, null);
    }
}