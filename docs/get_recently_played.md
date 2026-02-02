## Request

GET /me/player/recently-played

`limit` integer
The maximum number of items to return. Default: 20. Minimum: 1. Maximum: 50.

Default: limit=20
Range: 0 - 50
Example: limit=10

`after` integer
A Unix timestamp in milliseconds. Returns all items after (but not including) this cursor position. If after is specified, before must not be specified.

Example: after=1484811043508

`before` integer
A Unix timestamp in milliseconds. Returns all items before (but not including) this cursor position. If before is specified, after must not be specified.

## Response

A paged set of tracks

`href` string
A link to the Web API endpoint returning the full result of the request.

`limit` integer
The maximum number of items in the response (as set in the query or by default).

`next` string
URL to the next page of items. ( null if none)

`cursors` object
The cursors used to find the next set of items.

- `after` string
  The cursor to use as key to find the next page of items.

- `before` string
  The cursor to use as key to find the previous page of items.

`total` integer
The total number of items available to return.

`items` array of PlayHistoryObject
The cursor to use as key to find the previous page of items.

- `track` object
  The track the user listened to.

- `played_at` string [date-time]
  The date and time the track was played.

- `context` object
  The context the track was played from.
