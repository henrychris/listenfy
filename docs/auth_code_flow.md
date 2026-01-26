# Authorization Code Flow

The authorization code flow is suitable for long-running applications (e.g. web
and mobile apps) where the user grants permission only once.

If you’re using the authorization code flow in a mobile app, or any other type
of application where the client secret can't be safely stored, then you should
use the PKCE extension. Keep reading to learn how to correctly implement it.

The following diagram shows how the _authorization code flow_ works:

![Authorization Code Flow](https://developer-assets.spotifycdn.com/images/documentation/web-api/auth-code-flow.png)

#### Pre-requisites

This guide assumes that:

- You have read the [authorization guide](https://developer.spotify.com/documentation/web-api/concepts/authorization).
- You have created an app following the [apps guide](https://developer.spotify.com/documentation/web-api/concepts/apps).

#### Example

You can find an example app implementing Authorization Code flow on GitHub in
the [web-api-examples](https://github.com/spotify/web-api-examples/tree/master/authorization/authorization_code) repository.

## Request User Authorization

The first step is to request authorization from the user so that our app can access
to the Spotify resources on the user's behalf. To do this, our application must
build and send a `GET` request to the `/authorize` endpoint with the following
parameters:

| Query Parameter | Relevance                            | Value                                                                                                                                                                                                                                                                                                                                                                                                                                                                       |
| --------------- | ------------------------------------ | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| client_id       | _Required_                           | The Client ID generated after registering your application.                                                                                                                                                                                                                                                                                                                                                                                                                 |
| response_type   | _Required_                           | Set to `code`.                                                                                                                                                                                                                                                                                                                                                                                                                                                              |
| redirect_uri    | _Required_                           | The URI to redirect to after the user grants or denies permission. This URI needs to have been entered in the Redirect URI allowlist that you specified when you registered your application (See the [app guide](https://developer.spotify.com/documentation/web-api/concepts/apps)). The value of `redirect_uri` here must exactly match one of the values you entered when you registered your application, including upper or lowercase, terminating slashes, and such. |
| state           | _Optional, but strongly recommended_ | This provides protection against attacks such as cross-site request forgery. See [RFC-6749](https://datatracker.ietf.org/doc/html/rfc6749#section-4.1).                                                                                                                                                                                                                                                                                                                     |
| scope           | _Optional_                           | A space-separated list of [scopes](https://developer.spotify.com/documentation/web-api/concepts/scopes).If no scopes are specified, authorization will be granted only to access publicly available information: that is, only information normally visible in the Spotify desktop, web, and mobile players.                                                                                                                                                                |
| show_dialog     | _Optional_                           | Whether or not to force the user to approve the app again if they’ve already done so. If `false` (default), a user who has already approved the application may be automatically redirected to the URI specified by `redirect_uri`. If `true`, the user will not be automatically redirected and will have to approve the app again.                                                                                                                                        |

The following JavaScript code example implements the `/login` method using
[Express](https://expressjs.com/) framework to initiates the authorization
request:

`var client_id = 'CLIENT_ID';

var redirect_uri = 'http://127.0.0.1:8888/callback';

var app = express();

app.get('/login', function(req, res) {

var state = generateRandomString(16);

var scope = 'user-read-private user-read-email';

res.redirect('https://accounts.spotify.com/authorize?' +

    querystring.stringify({

      response_type: 'code',

      client_id: client_id,

      scope: scope,

      redirect_uri: redirect_uri,

      state: state

    }));

});

`

Once the request is processed, the user will see the authorization dialog
asking to authorize access within the `user-read-private` and `user-read-email`
scopes.

The Spotify OAuth 2.0 service presents details of the
[scopes](https://developer.spotify.com/documentation/web-api/concepts/scopes) for which access
is being sought. If the user is not logged in, they are prompted to do so using
their Spotify credentials. When the user is logged in, they are asked to
authorize access to the data sets or features defined in the scopes.

Finally, the user is redirected back to your specified `redirect_uri`. After
the user accepts, or denies your request, the Spotify OAuth 2.0 service
redirects the user back to your `redirect_uri`. In this example, the redirect
address is `https://127.0.0.1:8888/callback`

#### Response

If the user accepts your request, then the user is redirected back to the
application using the `redirect_uri` passed on the authorized request described
above.

The callback contains two query parameters:

| Query Parameter | Value                                                            |
| --------------- | ---------------------------------------------------------------- |
| code            | An authorization code that can be exchanged for an access token. |
| state           | The value of the `state` parameter supplied in the request.      |

For example:

`https://my-domain.com/callback?code=NApCCg..BkWtQ&state=34fFs29kd09

`

If the user does not accept your request or if an error has occurred, the response
query string contains the following parameters:

| Query Parameter | Value                                                         |
| --------------- | ------------------------------------------------------------- |
| error           | The reason authorization failed, for example: "access_denied" |
| state           | The value of the `state` parameter supplied in the request.   |

For example:

`https://my-domain.com/callback?error=access_denied&state=34fFs29kd09

`

In both cases, your app should compare the `state` parameter that it received
in the redirection URI with the state parameter it originally provided to
Spotify in the authorization URI. If there is a mismatch then your app should
reject the request and stop the authentication flow.

## Request an access token

If the user accepted your request, then your app is ready to exchange the
authorization code for an access token. It can do this by sending a `POST`
request to the `/api/token` endpoint.

The body of this `POST` request must contain the following parameters encoded
in `application/x-www-form-urlencoded`:

| Body Parameters | Relevance  | Value                                                                                                                                                                                                    |
| --------------- | ---------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| grant_type      | _Required_ | This field must contain the value `"authorization_code"`.                                                                                                                                                |
| code            | _Required_ | The authorization code returned from the previous request.                                                                                                                                               |
| redirect_uri    | _Required_ | This parameter is used for validation only (there is no actual redirection). The value of this parameter must exactly match the value of `redirect_uri` supplied when requesting the authorization code. |

The request must include the following HTTP headers:

| Header Parameter | Relevance  | Value                                                                                                                                                                     |
| ---------------- | ---------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Authorization    | _Required_ | Base 64 encoded string that contains the client ID and client secret key. The field must have the format: `Authorization: Basic <base64 encoded client_id:client_secret>` |
| Content-Type     | _Required_ | Set to `application/x-www-form-urlencoded`.                                                                                                                               |

This step is usually implemented within the callback described on the request
of the previous steps:

`app.get('/callback', function(req, res) {

var code = req.query.code || null;

var state = req.query.state || null;

if (state === null) {

    res.redirect('/#' +

      querystring.stringify({

        error: 'state_mismatch'

      }));

} else {

    var authOptions = {

      url: 'https://accounts.spotify.com/api/token',

      form: {

        code: code,

        redirect_uri: redirect_uri,

        grant_type: 'authorization_code'

      },

      headers: {

        'content-type': 'application/x-www-form-urlencoded',

        'Authorization': 'Basic ' + (new Buffer.from(client_id + ':' + client_secret).toString('base64'))

      },

      json: true

    };

}

});

`

### Response

On success, the response will have a `200 OK` status and the following JSON data
in the response body:

| key           | Type   | Description                                                                                               |
| ------------- | ------ | --------------------------------------------------------------------------------------------------------- |
| access_token  | string | An access token that can be provided in subsequent calls, for example to Spotify Web API services.        |
| token_type    | string | How the access token may be used: always "Bearer".                                                        |
| scope         | string | A space-separated list of scopes which have been granted for this `access_token`                          |
| expires_in    | int    | The time period (in seconds) for which the access token is valid.                                         |
| refresh_token | string | See [refreshing tokens](https://developer.spotify.com/documentation/web-api/tutorials/refreshing-tokens). |

### What's next?

- Congratulations! Your fresh access token is ready to be used! How can we make
  API calls with it? take a look at to the [access\\
  token](https://developer.spotify.com/documentation/web-api/concepts/access-token) guide to learn how to make
  an API call using your new fresh access token.

- If your access token has expired, you can learn how to issue a new one without
  requiring users to reauthorize your application by reading the [refresh token](https://developer.spotify.com/documentation/web-api/tutorials/refreshing-tokens) guide.
