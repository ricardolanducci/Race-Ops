using Microsoft.AspNetCore.Mvc;
using RaceOps.Application.DTOs;
using RaceOps.Application.UseCases;

namespace RaceOps.Web.Controllers;

[ApiController]
[Route("api/races/{raceId:int}")]
public class LiveController(
    GetLiveViewUseCase getLiveView,
    GetEventConfigUseCase getEventConfig,
    SaveEventConfigUseCase saveEventConfig,
    SaveStintUseCase saveStint) : ControllerBase
{
    /// <summary>Visão live completa. 204 enquanto o seed inicial não terminou.</summary>
    [HttpGet("live")]
    public async Task<IActionResult> GetLiveView(int raceId, CancellationToken ct)
    {
        var view = await getLiveView.ExecuteAsync(raceId, ct);
        return view is null ? NoContent() : Ok(view);
    }

    [HttpGet("config")]
    public async Task<IActionResult> GetConfig(int raceId, CancellationToken ct)
        => Ok(await getEventConfig.ExecuteAsync(raceId, ct));

    [HttpPut("config")]
    public async Task<IActionResult> SaveConfig(int raceId, [FromBody] EventConfigDto dto, CancellationToken ct)
    {
        await saveEventConfig.ExecuteAsync(dto with { RaceId = raceId }, ct);
        return NoContent();
    }

    [HttpPut("stints/{kartNumber}")]
    public async Task<IActionResult> SaveStint(int raceId, string kartNumber, [FromBody] StintDto dto, CancellationToken ct)
    {
        await saveStint.ExecuteAsync(raceId, kartNumber, dto, ct);
        return NoContent();
    }
}
