using System.Security.Claims;
using Microsoft.Azure.Functions.Worker.Http;

namespace ServerSpinner.Functions.Services;

public interface IAuthService
{
    ClaimsPrincipal? Authenticate(HttpRequestData req);
}