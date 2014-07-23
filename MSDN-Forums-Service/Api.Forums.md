# Forums

## Get forum details

```httprequest
GET http://api.msdn.microsoft.com/forums/{forum}[?api-version={string}]
```

| Parameter   | Type     | Default | Notes
|:------------|:--------:|:-------:|:----------------------------------------------------------------------------------------------------------------------------
| URL
| forum       | string   |         | The name or id of a forum.  A comma-separated list of forums may be supplied.
| api-version | float    | 1.0     | The api version number to use for processing the request and formatting results.  Currently, only "1.0" is supported.  The version may also be requested by using the "ACCEPT api-version=1.0" HTTP Request header.

### By forum id

#### Sample request
```httprequest
GET http://forumsapi.msdn.microsoft.com/forums/7a9b8c49-06ac-44fe-b0fd-81485bd31386
```

#### Sample response
```json
{
    "hasMore" : false,
    "values" : [ 
        {
            "id": "7a9b8c49-06ac-44fe-b0fd-81485bd31386",
            "name": "2018",
            "url": "http://forumsapi.msdn.microsoft.com/forums/7a9b8c49-06ac-44fe-b0fd-81485bd31386",
            "webUrl": "http://social.msdn.microsoft.com/forums/zh-cn/home?forum=sqlserverzhchs",
            "displayName": "领先一步 从TechNet 中文速递邮件开始",
            "description": "希望获得微软最新产品与技术白皮书？希望第一时间下载微软每月安全更新？希望在线体验微软最新产品？希望了解权威的知识库文章？更多精彩内容，尽在免费的TechNet中文速递邮件中！",
            "modified": "2009-01-26T23:27:38.3160342Z",
            "type": "Forum",
            "threads": 64,
            "answeredThreads": 3,
            "unansweredThreads": 12,
			"discussionThreads": 8,
            "replies": 134,
            "locked": false,
            "active": true,
            "brands":["Microsoft","Msdn"],
            "locale":"en-US",
            "categories": [
                {
                    "id": "646470fc-459b-47be-88bb-8ca5b443b71d",
                    "name": "marketingprogramcn",
					"displayName": "Chinese Marketing",
                    "brand": "Microsoft",
                    "locale": "en-US"
                },
                {
                    "id": "646470fc-459b-47be-88bb-8ca5b443b71d",
                    "name": "marketingprogramcn",
					"displayName": "Chinese Marketing",
                    "brand": "Msdn",
                    "locale": "en-US"
                }
            ]
        }  
    ]
}
```

## search forums

```httprequest
GET https://forumsapi.msdn.microsoft.com/forums[?forumId={string}[,{string}]&forumName={string}[,{string}]&categoryId={string}[,{string}]&categoryName={string}[,{string}]&brand={string}&type={string}[,{string}]&locale={string}&threadCreatedFrom={dateTime}&threadCreatedTo={dateTime}&messageCreatedFrom={dateTime}&messageCreatedTo={dateTime}&sort={string}&order={string}&page={integer}&pageSize={integer}&api-version={float}]
```

| Parameter  | Type     | Default | Notes
|:-----------|:--------:|:-------:|:----------------------------------------------------------------------------------------------------------------------------
| Query
| forumId                  | string   |         | The id of a forum.  A comma-separated list of forums may be supplied.
| forumName                  | string   |         | The name of a forum.  A comma-separated list of forums may be supplied.
| categoryId               | string   |         | The id of the forum category.  Forums in this category will be returned. A comma-separated list of categories may be supplied.
| categoryName               | string   |         | The name of the forum category.   Forums in this category will be returned. A comma-separated list of categories may be supplied.
| search                 | string   |         | A free text search string.  All forums whose display name matches the search string will be returned.
| brand                  | string   |         | The brand name.  Valid values are "Microsoft", Msdn", "Technet".  Forums in this brand will be returned.
| type                   | string   |         | The forum type. A comma-separated list of types may be supplied. Valid values are "forum", "moderatorForum", "privateForum", "moderatorPostingForum". When multiple values are supplied we match on any value.
| locale                 | string   |         | The forum locale.  The locale may be specified as either the LCID integer value, or as a locale string.  For example, to return US english forums only, you could supply either ?locale=1033 or ?locale=en-us.  
| threadCreatedFrom   | dateTime |         | The minimum thread creation date.  Only forums with a thread created on or after this date will be returned.
| threadCreatedTo     | dateTime |         | The maximum thread creation date.  Only forums with a thread created on or before this date will be returned.
| replyCreatedFrom  | dateTime |         | The minimum message creation date.  Only forums with a message created on or after this date will be returned.
| replyCreatedTo    | dateTime |         | The maximum message creation date.  Only forums with a message created onr or before this date will be returned.
| sort                   | string   | threadDate | The sort order of returned threads.  Valid values are "threadDate", "replyDate".
| order                  | string   | desc    | The sort order of returned threads.  Valid values are "asc" and "desc".
| page                   | integer  | 1       | When the number of results exceeds the maximum page size, then the page parameter indicates which page of results to return.  This value is 1-based, meaning that the first page of results is indicated by ?page=1
| pageSize               | integer  | 50      | The number of results to return per page.  This value cannot exceed 50.
| api-version            | float    | 1.0     | The api version number to use for processing the request and formatting results.  Currently, only "1.0" is supported.  The version may also be requested by using the "ACCEPT api-version=1.0" HTTP Request header.

### By multiple IDs

#### Sample request
```httprequest
GET http://forumsapi.msdn.microsoft.com/forums?forumId=2dac51a6-0bcd-4ad7-b4d3-3f49fb3fe002,1ac816b4-e220-44ca-91c1-6f9f414c3ec6
```

#### Sample response
```json
{
    "hasMore" : false,
    "values" : [ 
        {
            "id": "2dac51a6-0bcd-4ad7-b4d3-3f49fb3fe002",
            "url": "http://forumsapi.msdn.microsoft.com/forums/2dac51a6-0bcd-4ad7-b4d3-3f49fb3fe002",
            "webUrl": "http://social.msdn.microsoft.com/forums/en-us/home?forum=liveessentials",
            "name": "windowsliveessentialsit",
            "displayName": "Windows Live Essentials Forum",
            "description": "Forum italiano dedicato alla famiglia di prodotti Windows Live",
            "modified": "2009-09-21T11:43:14.3160342Z",
            "type": "Forum",
            "threads": 84,
            "answeredThreads": 46,
            "unansweredThreads": 36,
			"discussionThreads": 8,	
            "replies": 315,
            "locked": true,
            "active": true,
            "brands":["Microsoft","Msdn"],
            "locale":"en-US",
            "categories": [
                {
                    "id": "58aff5d6-8c67-4295-98a9-03f8808a3da6",
                    "name": "windowslive",
					"displayName": "Windows Live",
                    "brand": "Msdn",
                    "locale": "en-US"
                },
                {
                    "id": "58aff5d6-8c67-4295-98a9-03f8808a3da6",
                    "name": "windowslive",
					"displayName": "Windows Live",
                    "brand": "Microsoft",
                    "locale": "en-US"
                }
            ]
        },
        {
            "id": "1ac816b4-e220-44ca-91c1-6f9f414c3ec6",
            "name": "WLDiscuss",
            "displayName": "Windows Live Discussion",
            "description": "The place to discuss Windows Live",
            "lastUpdated": "2009-06-19T09:35:51.3160342Z",
            "type": "Forum",
            "threads": 17,
            "answeredThreads": 7,
            "unansweredThreads": 8,
			"discussionThreads": 8,
            "replies": 38,
            "locked": false,
            "active": true,
            "brands":["Microsoft","Msdn"],
            "locale":"en-US",
            "categories": null
        }
    ]
}
```


### By search query

#### Sample request
```httprequest
GET http://forumsapi.msdn.microsoft.com/forums?locale=1033&threadCreatedFrom=2014-05-15&type=forum&brand=technet&sort=threadDate
```

#### Sample response
```json
{
    "hasMore" : false,
    "values" : [ 
        {
            "id": "2dac51a6-0bcd-4ad7-b4d3-3f49fb3fe002",
            "name": "windowsliveessentialsit",
            "url": "http://forumsapi.msdn.microsoft.com/forums/2dac51a6-0bcd-4ad7-b4d3-3f49fb3fe002",
            "webUrl": "http://social.msdn.microsoft.com/forums/it-it/home?forum=windowsliveessentialsit",
            "displayName": "Windows Live Essentials Forum",
            "description": "Forum italiano dedicato alla famiglia di prodotti Windows Live",
            "modified": "2009-09-21T11:43:14.3160342Z",
            "type": "Forum",
            "threads": 84,
            "answeredThreads": 46,
            "unansweredThreads": 36,
			"discussionThreads": 8,
            "replies": 315,
            "locked": true,
            "active": true,
            "brands":["Microsoft","Msdn"],
            "locale":"en-US",
            "categories": [
                {
                    "id": "58aff5d6-8c67-4295-98a9-03f8808a3da6",
                    "name": "windowslive",
					"displayName": "Windows Live",
                    "brand": "Microsoft",
                    "locale": "en-US"
                },
                {
                    "id": "58aff5d6-8c67-4295-98a9-03f8808a3da6",
                    "name": "windowslive",
					"displayName": "Windows Live",
                    "brand": "Msdn",
                    "locale": "en-US"
                }
            ]
        },
        {
            "id": "1ac816b4-e220-44ca-91c1-6f9f414c3ec6",
            "name": "WLDiscuss",
            "displayName": "Windows Live Discussion",
            "description": "The place to discuss Windows Live",
            "modified": "2009-06-19T09:35:51.25",
            "type": "Forum",
            "threads": 17,
            "answeredThreads": 7,
            "unansweredThreads": 8,
			"discussionThreads": 8,
            "replies": 38,
            "locked": false,
            "active": true,
            "brands":["Microsoft","Msdn"],
            "locale":"en-US",
            "categories": null
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
           "Invalid parameter value: 'date'.",
           "Invalid parameter value: '042870'."
    ]
}
```