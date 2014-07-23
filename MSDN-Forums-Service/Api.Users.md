# Forums users

## Get user details

```httprequest
GET http://forumsapi.msdn.microsoft.com/users/{user}[?api-version={string}]
```

| Parameter   | Type     | Default | Notes 
|:------------|:--------:|:-------:|:----------------------------------------------------------------------------------------------------------------------------
| URL
| user        | string   |         | Id or display name of the user.
| api-version | float    | 1.0     | The api version number to use for processing the request and formatting results.  Currently, only "1.0" is supported.  The version may also be requested by using the "ACCEPT api-version=1.0" HTTP Request header.

### By user id

#### Sample request
```httprequest
GET http://forumsapi.msdn.microsoft.com/users/5812d39c-2064-4fee-ba3e-eb9716fb7a37

GET http://forumsapi.msdn.microsoft.com/users/Nigel%20Beasley
```

#### Sample response
```json
{
    "hasMore" : false,
    "values" : [
        {
            "id":"5812d39c-2064-4fee-ba3e-eb9716fb7a37",
            "displayName":"Nigel Beasley",
            "role" : [ "communityContributor" ],
            "url" :"http://forumsapi.msdn.microsoft.com/forums/users/5812d39c-2064-4fee-ba3e-eb9716fb7a37",
            "webUrl": "http://social.msdn.microsoft.com/Profile/Nigel%20Beasley",
            "points" : 7,
            "messagesCount" : 14,
            "answersCount" : 5
        }
    ]
}
```

## search users

```httprequest
GET http://forumsapi.msdn.microsoft.com/users[?userId={string}[,{string}]&userName={string}[,{string}]&role={string}[,{string}]&sort={string}&order={string}&page={integer}&pageSize={integer}&api-version={string}]
```

| Parameter      | Type     | Default | Notes
|:-----------    |:--------:|:-------:|:----------------------------------------------------------------------------------------------------------------------------
| Query
| userId           | string   |         | Id of the user.  A comma-separated list of users may be supplied.
| userName           | string   |         | Display name of the user.  A comma-separated list of users may be supplied.
| role           | string   |         | The role of the user.  Allowed roles are: "microsoftEmployee", "microsoftValuableProfessional", "partner", "microsoftSupportStaff", "microsoftContingentStaff", and "communityContributor". A comma-separated list of roles may be supplied. If multiple roles are provided, a user will be included in the result set if any role is matched.
| sort           | string   | points  | The sort order of returned users.  Valid values are "displayName", "points", "answersCount", "messagesCount".
| order          | string   | desc    | The sort order of returned threads.  Valid values are "asc", "desc".
| page           | integer  | 1       | When the number of results exceeds the maximum page size, then the page parameter indicates which page of results to return.  This value is unit-based, e.g. the first page of results is indicated by ?page=1
| pageSize       | integer  | 50      | The number of results to return per page.  This value cannot exceed 50.
| api-version    | float    | 1.0     | The api version number to use for processing the request and formatting results.  Currently, only "1.0" is supported.  The version may also be requested by using the "ACCEPT api-version=1.0" HTTP Request header.


### By multiple IDs

#### Sample request
```httprequest
GET http://forumsapi.msdn.microsoft.com/users?userId=5812d39c-2064-4fee-ba3e-eb9716fb7a37,26b8b233-d0a6-41cc-859c-eb96b2225607
```

#### Sample response
```json
{
    "hasMore" : false,
    "values" : [ 
        {
            "id":"5812d39c-2064-4fee-ba3e-eb9716fb7a37",
            "displayName": "Nigel Beasley",
            "role" : [ "microsoftValuedProfessional" ],
            "url" :"http://forumsapi.msdn.microsoft.com/forums/users/5812d39c-2064-4fee-ba3e-eb9716fb7a37",
            "webUrl": "http://social.msdn.microsoft.com/Profile/Nigel%20Beasley",
            "points" : 7,
            "messagesCount" : 14,
            "answersCount" : 5
        },
        {
            "id":"26b8b233-d0a6-41cc-859c-eb96b2225607",
            "displayName": "larrysir",
            "role" : [ "microsoftContingentStaff" ],
            "url" :"http://forumsapi.msdn.microsoft.com/forums/users/26b8b233-d0a6-41cc-859c-eb96b2225607",
            "webUrl": "http://social.msdn.microsoft.com/Profile/larrysir",
            "points" : 0,
            "messagesCount" : 2,
            "answersCount" : 0
        }
    ]
}
```


## Errors

REST API operations for forums services return standard HTTP status codes, as defined in the HTTP/1.1 Status Code Definitions.  API operations my also return additional error information in the response body.

The body of the error response follows the JSON format shown here.

#### Sample response

```json
{
    "errors" : [
            "Invalid parameter value: 'from'. ",
            "Invalid parameter value: '042870'."
    ]
}
```