using Listenfy.Application.Features.Spotify.CompleteOAuth;
using Listenfy.Application.Features.Spotify.Shared;
using Listenfy.Application.Settings;
using Listenfy.Shared.Api;
using Listenfy.Shared.Api.Responses;
using Listenfy.Shared.Results;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Listenfy.Application.Features.Spotify;

public class SpotifyController(IMediator mediator) : BaseController
{
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
                State = state,
            }
        );
        return result.Match(_ => Ok(result.ToSuccessfulApiResponse()), ReturnErrorResponse);
    }

    [HttpPost("oauth/complete")]
    [ProducesResponseType(typeof(ApiResponse<OAuthResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Complete(CompleteOAuthRequest request)
    {
        var result = await mediator.Send(request);
        return result.Match(_ => Ok(result.ToSuccessfulApiResponse()), ReturnErrorResponse);
    }
}
