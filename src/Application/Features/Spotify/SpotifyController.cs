using Listenfy.Application.Settings;
using Listenfy.Shared.Api;
using Listenfy.Shared.Api.Responses;
using Listenfy.Shared.Results;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Listenfy.Application.Features.Spotify;

public class SpotifyController(IMediator mediator, IOptions<SpotifySettings> options) : BaseController
{
    private readonly SpotifySettings _spotifySettings = options.Value;

    [HttpGet("callback")]
    [ProducesResponseType(typeof(ApiResponse<OAuthResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Callback([FromQuery] string code, [FromQuery] string state, [FromQuery] string? error)
    {
        var result = await mediator.Send(
            new OAuthCallbackRequest
            {
                Code = code,
                Error = error,
                RedirectUri = _spotifySettings.RedirectUrl,
                State = state,
            }
        );
        return result.Match(_ => Ok(result.ToSuccessfulApiResponse()), ReturnErrorResponse);
    }
}
