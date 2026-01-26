# Refreshing tokens

A refresh token is a security credential that allows client applications to
obtain new access tokens without requiring users to reauthorize the
application.

[Access tokens](https://developer.spotify.com/documentation/web-api/concepts/access-token) are intentionally
configured to have a limited lifespan (1 hour), at the end of which, new tokens
can be obtained by providing the original refresh token acquired during the
authorization token request response:

`{

"access_token": "NgCXRK...MzYjw",

"token_type": "Bearer",

"scope": "user-read-private user-read-email",

"expires_in": 3600,

"refresh_token": "NgAagA...Um_SHo"

}

`

## Request

To refresh an access token, we must send a `POST` request with the following
parameters:

| Body Parameter | Relevance | Value |
| --- | --- | --- |
| grant\_type | _Required_ | Set it to `refresh_token`. |
| refresh\_token | _Required_ | The refresh token returned from the authorization token request. |
| client\_id | **Only required for the** [PKCE extension](https://developer.spotify.com/documentation/web-api/tutorials/code-pkce-flow) | The client ID for your app, available from the developer dashboard. |

And the following headers:

| Header Parameter | Relevance | Value |
| --- | --- | --- |
| Content-Type | _Required_ | Always set to `application/x-www-form-urlencoded`. |
| Authorization | **Only required for the** [Authorization Code](https://developer.spotify.com/documentation/web-api/tutorials/code-flow) | Base 64 encoded string that contains the client ID and client secret key. The field must have the format: `Authorization: Basic <base64 encoded client_id:client_secret>` |

### Example

The following code snippets represent two examples:

- A client side (browser) JavaScript function to refresh tokens issued following the [Authorization Code with PKCE extension flow](https://developer.spotify.com/documentation/web-api/tutorials/code-pkce-flow).
- A server side (nodeJS with express) Javascript method to refresh tokens issued under the [Authorization Code flow](https://developer.spotify.com/documentation/web-api/tutorials/code-flow).

browser

nodeJS

`  const getRefreshToken = async () => {

// refresh token that has been previously stored

const refreshToken = localStorage.getItem('refresh_token');

const url = "https://accounts.spotify.com/api/token";

    const payload = {

      method: 'POST',

      headers: {

        'Content-Type': 'application/x-www-form-urlencoded'

      },

      body: new URLSearchParams({

        grant_type: 'refresh_token',

        refresh_token: refreshToken,

        client_id: clientId

      }),

    }

    const body = await fetch(url, payload);

    const response = await body.json();

    localStorage.setItem('access_token', response.access_token);

    if (response.refresh_token) {

      localStorage.setItem('refresh_token', response.refresh_token);

    }

}

`

## Response

If everything goes well, you'll receive a `200 OK` response which is very
similar to the response when issuing an access token:

`{

access_token: 'BQBLuPRYBQ...BP8stIv5xr-Iwaf4l8eg',

token_type: 'Bearer',

expires_in: 3600,

refresh_token: 'AQAQfyEFmJJuCvAFh...cG_m-2KTgNDaDMQqjrOa3',

scope: 'user-read-email user-read-private'

}

`

The refresh token contained in the response, can be used to request new tokens.
Depending on the grant used to get the initial refresh token, a refresh token
might not be included in each response. When a refresh token is not returned,
continue using the existing token.
