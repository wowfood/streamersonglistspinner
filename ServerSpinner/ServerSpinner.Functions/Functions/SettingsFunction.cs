using System.Net;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.EntityFrameworkCore;
using ServerSpinner.Core.Data;
using ServerSpinner.Functions.Contracts;
using ServerSpinner.Functions.Entities;
using ServerSpinner.Functions.Data;
using ServerSpinner.Functions.Helpers;
using ServerSpinner.Functions.Services;

namespace ServerSpinner.Functions.Functions;

public class SettingsFunction
{
    private static readonly JsonSerializerOptions CamelCase = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
    private static readonly JsonSerializerOptions CaseInsensitive = new() { PropertyNameCaseInsensitive = true };

    private readonly IAuthService _authService;
    private readonly AppDbContext _db;
    private readonly ISettingsMapper _settingsMapper;

    public SettingsFunction(AppDbContext db, IAuthService authService, ISettingsMapper settingsMapper)
    {
        _db = db;
        _authService = authService;
        _settingsMapper = settingsMapper;
    }

    [Function("GetSettings")]
    public async Task<HttpResponseData> Get(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "settings")]
        HttpRequestData req)
    {
        var principal = _authService.Authenticate(req);
        if (principal is null) return req.CreateResponse(HttpStatusCode.Unauthorized);

        var (streamerId, _) = JwtHelper.GetClaims(principal);
        if (!Guid.TryParse(streamerId, out var id)) return req.CreateResponse(HttpStatusCode.Unauthorized);

        var settings = await _db.StreamerSettings.FirstOrDefaultAsync(s => s.StreamerId == id)
                       ?? new StreamerSettings { StreamerId = id };

        var response = req.CreateResponse(HttpStatusCode.OK);
        response.Headers.Add("Content-Type", "application/json");
        await response.WriteStringAsync(JsonSerializer.Serialize(_settingsMapper.ToDto(settings), CamelCase));
        return response;
    }

    [Function("SaveSettings")]
    public async Task<HttpResponseData> Save(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "settings")]
        HttpRequestData req)
    {
        var principal = _authService.Authenticate(req);
        if (principal is null) return req.CreateResponse(HttpStatusCode.Unauthorized);

        var (streamerId, _) = JwtHelper.GetClaims(principal);
        if (!Guid.TryParse(streamerId, out var id)) return req.CreateResponse(HttpStatusCode.Unauthorized);

        SettingsDto? dto;
        try
        {
            using var reader = new StreamReader(req.Body);
            var bodyStr = await reader.ReadToEndAsync();
            dto = JsonSerializer.Deserialize<SettingsDto>(bodyStr, CaseInsensitive);
        }
        catch (JsonException)
        {
            return req.CreateResponse(HttpStatusCode.BadRequest);
        }

        if (dto is null) return req.CreateResponse(HttpStatusCode.BadRequest);

        var settings = await _db.StreamerSettings.FirstOrDefaultAsync(s => s.StreamerId == id);
        if (settings == null)
        {
            settings = new StreamerSettings { StreamerId = id };
            _db.StreamerSettings.Add(settings);
        }

        _settingsMapper.Apply(dto, settings);
        await _db.SaveChangesAsync();

        return req.CreateResponse(HttpStatusCode.OK);
    }
}