using Microsoft.AspNetCore.Mvc;
using RaceOps.Application.UseCases;

namespace RaceOps.Web.Controllers;

[ApiController]
[Route("api/races")]
public class RacesController(
    GetCurrentRacesUseCase getCurrentRaces,
    GetLiveSessionUseCase getLiveSession,
    GetRacerDetailUseCase getRacerDetail,
    GetStreamingConnectionUseCase getStreamingConnection) : ControllerBase
{
    [HttpGet("current")]
    public async Task<IActionResult> GetCurrentRaces(CancellationToken ct)
    {
        var races = await getCurrentRaces.ExecuteAsync(ct);
        return Ok(races);
    }

    [HttpGet("{raceId:int}/session")]
    public async Task<IActionResult> GetSession(int raceId, CancellationToken ct)
    {
        var session = await getLiveSession.ExecuteAsync(raceId, ct);
        return Ok(session);
    }

    [HttpGet("{raceId:int}/racer/{racerId}")]
    public async Task<IActionResult> GetRacer(int raceId, string racerId, CancellationToken ct)
    {
        var detail = await getRacerDetail.ExecuteAsync(raceId, racerId, ct);
        return Ok(detail);
    }

    [HttpGet("{raceId:int}/streaming-connection")]
    public async Task<IActionResult> GetStreamingConnection(int raceId, CancellationToken ct)
    {
        var info = await getStreamingConnection.ExecuteAsync(raceId, ct);
        return Ok(info);
    }
}
