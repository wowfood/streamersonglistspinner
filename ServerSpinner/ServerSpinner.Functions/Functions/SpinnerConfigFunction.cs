using System.Net;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.EntityFrameworkCore;
using ServerSpinner.Functions.Contracts;
using ServerSpinner.Functions.Entities;
using ServerSpinner.Functions.Data;
using ServerSpinner.Functions.Helpers;
using ServerSpinner.Functions.Services;

namespace ServerSpinner.Functions.Functions;

public class SpinnerConfigFunction
{
    private readonly IAuthService _authService;
    private readonly AppDbContext _db;
    private readonly ISpinnerConfigMapper _spinnerConfigMapper;

    public SpinnerConfigFunction(AppDbContext db, IAuthService authService, ISpinnerConfigMapper spinnerConfigMapper)
    {
        _db = db;
        _authService = authService;
        _spinnerConfigMapper = spinnerConfigMapper;
    }

    [Function("GetMySpinnerConfig")]
    public async Task<HttpResponseData> GetMyConfig(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "spinner-config")]
        HttpRequestData req)
    {
        var principal = _authService.Authenticate(req);
        if (principal is null) return req.CreateResponse(HttpStatusCode.Unauthorized);

        var (streamerId, _) = JwtHelper.GetClaims(principal);
        if (!Guid.TryParse(streamerId, out var id)) return req.CreateResponse(HttpStatusCode.Unauthorized);

        var settings = await _db.StreamerSettings.FirstOrDefaultAsync(s => s.StreamerId == id)
                       ?? new StreamerSettings();

        var response = req.CreateResponse(HttpStatusCode.OK);
        response.Headers.Add("Content-Type", "application/json");
        await response.WriteStringAsync(JsonSerializer.Serialize(_spinnerConfigMapper.ToConfigResponse(settings),
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }));
        return response;
    }

    [Function("GetSpinnerConfigForOverlay")]
    public async Task<HttpResponseData> GetConfigForOverlay(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "spinner-config/{streamerId:guid}")]
        HttpRequestData req,
        Guid streamerId)
    {
        var settings = await _db.StreamerSettings.FirstOrDefaultAsync(s => s.StreamerId == streamerId)
                       ?? new StreamerSettings();

        var response = req.CreateResponse(HttpStatusCode.OK);
        response.Headers.Add("Content-Type", "application/json");
        await response.WriteStringAsync(JsonSerializer.Serialize(_spinnerConfigMapper.ToConfigResponse(settings),
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }));
        return response;
    }
}