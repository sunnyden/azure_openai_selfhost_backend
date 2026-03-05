# User API Reference

Endpoints available to **all authenticated users**.  
Authentication is performed with a JWT Bearer token obtained from [`POST /user/auth`](#post-userauth).

> **Note:** Admin users may also call every endpoint documented here.  
> For admin-only endpoints see [api-admin.md](api-admin.md).

---

## Table of Contents

- [Authentication](#authentication)
  - [POST /user/auth](#post-userauth)
- [User Profile](#user-profile)
  - [GET /user/me](#get-userme)
  - [POST /user/update-password](#post-userupdate-password)
- [Models](#models)
  - [GET /model/list](#get-modellist)
- [Chat](#chat)
  - [POST /chat/completion](#post-chatcompletion)
  - [POST /chat/streamingCompletion](#post-chatstreamingcompletion)
- [Transactions](#transactions)
  - [GET /transaction/list](#get-transactionlist)
- [Chat History](#chat-history)
  - [POST /chat-history/create](#post-chat-historycreate)
  - [GET /chat-history/get/{id}](#get-chat-historygetid)
  - [GET /chat-history/list](#get-chat-historylist)
  - [POST /chat-history/update-title](#post-chat-historyupdate-title)
  - [POST /chat-history/update](#post-chat-historyupdate)
  - [POST /chat-history/append-messages](#post-chat-historyappend-messages)
  - [POST /chat-history/delete](#post-chat-historydelete)

---

## Authentication

### POST /user/auth

Authenticates a user with a username and password, and returns a signed JWT token.  
This is the **only endpoint that does not require an `Authorization` header**.

**Request Body**

```json
{
  "userName": "alice",
  "password": "s3cr3t"
}
```

| Field | Type | Required | Description |
|---|---|---|---|
| `userName` | `string` | ✅ | The account username |
| `password` | `string` | ✅ | The account password (plaintext; transmitted over HTTPS) |

**Success Response `200`**

```json
{
  "isSuccess": true,
  "data": "eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9...",
  "error": null
}
```

`data` is a JWT token string. Pass it in subsequent requests as:

```
Authorization: Bearer eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9...
```

**Error Responses**

| Status | Condition |
|---|---|
| `401` | Username not found or password incorrect |

---

## User Profile

### GET /user/me

Returns the profile of the currently authenticated user.

**Authentication:** Required

**Request Body:** None

**Success Response `200`**

```json
{
  "isSuccess": true,
  "data": {
    "id": 42,
    "userName": "alice",
    "isAdmin": false,
    "remainingCredit": 8.50,
    "creditQuota": 10.00,
    "lastCreditReset": "2024-06-01T00:00:00Z"
  },
  "error": null
}
```

| Field | Type | Description |
|---|---|---|
| `id` | `integer` | Unique user identifier |
| `userName` | `string` | Username |
| `isAdmin` | `boolean` | Whether the user has the admin role |
| `remainingCredit` | `number` | Current credit balance (cost units) |
| `creditQuota` | `number` | Periodic credit allowance refilled automatically |
| `lastCreditReset` | `string \| null` | ISO 8601 timestamp of the last credit reset, or `null` if never reset |

> **Note:** The `password` field is always omitted from user responses.

**Error Responses**

| Status | Condition |
|---|---|
| `401` | Missing or invalid JWT token |

---

### POST /user/update-password

Changes the password of the currently authenticated user.

**Authentication:** Required

**Request Body**

```json
{
  "oldPassword": "current_password",
  "newPassword": "new_secure_password"
}
```

| Field | Type | Required | Description |
|---|---|---|---|
| `oldPassword` | `string` | ✅ | The user's current password |
| `newPassword` | `string` | ✅ | The desired new password |

**Success Response `200`**

```json
{
  "isSuccess": true,
  "data": null,
  "error": null
}
```

**Error Responses**

| Status | Condition |
|---|---|
| `401` | Missing/invalid JWT token, or `oldPassword` is incorrect |
| `404` | Authenticated user record not found |

---

## Models

### GET /model/list

Returns the list of AI models that have been assigned to the currently authenticated user.

**Authentication:** Required

**Request Body:** None

**Success Response `200`**

```json
{
  "isSuccess": true,
  "data": [
    {
      "identifier": "gpt-4o",
      "friendlyName": "GPT-4o",
      "endpoint": "https://my-resource.openai.azure.com/",
      "deployment": "gpt-4o",
      "costPromptToken": 0.000005,
      "costResponseToken": 0.000015,
      "isVision": true,
      "maxTokens": 128000,
      "supportTool": true,
      "apiVersionOverride": null,
      "reasoningModel": false
    }
  ],
  "error": null
}
```

| Field | Type | Description |
|---|---|---|
| `identifier` | `string` | Unique model identifier used in chat requests |
| `friendlyName` | `string` | Human-readable display name |
| `endpoint` | `string` | Azure OpenAI resource endpoint URL |
| `deployment` | `string` | Azure deployment name |
| `costPromptToken` | `number` | Cost per prompt token (in credit units) |
| `costResponseToken` | `number` | Cost per response token (in credit units) |
| `isVision` | `boolean` | Whether the model supports image inputs |
| `maxTokens` | `integer` | Maximum context window in tokens |
| `supportTool` | `boolean` | Whether the model supports tool/function calling |
| `apiVersionOverride` | `string \| null` | Custom Azure API version, or `null` to use the default |
| `reasoningModel` | `boolean` | Whether the model is a reasoning model (e.g. o-series) |

> **Note:** The `key` (access key) field is never returned in any response.

**Error Responses**

| Status | Condition |
|---|---|
| `401` | Missing or invalid JWT token |

---

## Chat

### POST /chat/completion

Sends a chat completion request and returns the **full response** once generation is complete.

**Authentication:** Required

**Request Body**

```json
{
  "model": "gpt-4o",
  "request": {
    "messages": [
      {
        "role": "system",
        "content": [{ "type": "text", "text": "You are a helpful assistant." }]
      },
      {
        "role": "user",
        "content": [{ "type": "text", "text": "Hello!" }]
      }
    ],
    "max_tokens": 512,
    "stream": false,
    "MCPCorrelationId": null
  }
}
```

| Field | Type | Required | Description |
|---|---|---|---|
| `model` | `string` | ✅ | Identifier of the model to use (must be assigned to the user) |
| `request.messages` | `ChatMessage[]` | ✅ | Ordered list of conversation messages |
| `request.max_tokens` | `integer` | ✅ | Maximum number of tokens to generate |
| `request.stream` | `boolean` | | Set to `false` for this endpoint (streaming uses a dedicated endpoint) |
| `request.MCPCorrelationId` | `string \| null` | | Correlation ID for Model Context Protocol sessions |

**ChatMessage object**

| Field | Type | Values | Description |
|---|---|---|---|
| `role` | `string` | `"system"`, `"user"`, `"assistant"` | The speaker role |
| `content` | `ChatContentItem[]` | | One or more content parts |

**ChatContentItem object**

| Field | Type | Values | Description |
|---|---|---|---|
| `type` | `string` | `"text"`, `"image"`, `"audio"` | Content type |
| `text` | `string \| null` | | Text content (for `type: "text"`) |
| `base64Data` | `string \| null` | | Base64-encoded binary data (for `type: "image"` or `"audio"`) |

**Success Response `200`**

```json
{
  "isSuccess": true,
  "data": {
    "id": "chatcmpl-abc123",
    "stopReason": "stop",
    "message": "Hello! How can I help you today?",
    "promptTokens": 25,
    "responseTokens": 10,
    "totalTokens": 35
  },
  "error": null
}
```

| Field | Type | Description |
|---|---|---|
| `id` | `string` | Unique completion identifier |
| `stopReason` | `string` | Reason generation stopped (e.g. `"stop"`, `"length"`) |
| `message` | `string` | The assistant's generated text |
| `promptTokens` | `integer` | Tokens consumed by the input |
| `responseTokens` | `integer` | Tokens consumed by the output |
| `totalTokens` | `integer` | Total tokens consumed (`promptTokens + responseTokens`) |

**Error Responses**

| Status | Condition |
|---|---|
| `401` | Missing or invalid JWT token |
| `402` | User has insufficient credits |
| `404` | Specified model not found or not assigned to the user |

---

### POST /chat/streamingCompletion

Sends a chat completion request and streams the response as **Server-Sent Events (SSE)**.

**Authentication:** Required

**Request Body**

Same structure as [`POST /chat/completion`](#post-chatcompletion).

```json
{
  "model": "gpt-4o",
  "request": {
    "messages": [
      {
        "role": "user",
        "content": [{ "type": "text", "text": "Count to 5." }]
      }
    ],
    "max_tokens": 64,
    "stream": true
  }
}
```

**Response**

Content-Type: `text/event-stream`

The server writes one SSE event per token chunk, followed by a final event. Each event body is a JSON-encoded `PartialChatResponse`:

```
data: {"data":"1","finishReason":null,"isEnd":false,"toolName":null,"toolParameters":null}

data: {"data":", 2","finishReason":null,"isEnd":false,"toolName":null,"toolParameters":null}

data: {"data":"","finishReason":"stop","isEnd":true,"toolName":null,"toolParameters":null}

```

**PartialChatResponse fields**

| Field | Type | Description |
|---|---|---|
| `data` | `string` | The incremental text chunk for this event |
| `finishReason` | `string \| null` | Stop reason on the final event, `null` otherwise |
| `isEnd` | `boolean` | `true` on the last event, `false` for all intermediate events |
| `toolName` | `string \| null` | Name of a tool call, if the model invoked a tool |
| `toolParameters` | `string \| null` | JSON-serialized parameters for the tool call |

**Error Responses**

| Status | Condition |
|---|---|
| `401` | Missing or invalid JWT token |
| `402` | User has insufficient credits |
| `404` | Specified model not found or not assigned to the user |

---

## Transactions

### GET /transaction/list

Returns the billing transaction history for the currently authenticated user.  
Each transaction corresponds to one chat completion and records token usage and cost.

**Authentication:** Required

**Request Body:** None

**Success Response `200`**

```json
{
  "isSuccess": true,
  "data": [
    {
      "id": 1001,
      "time": "2024-06-15T10:23:45Z",
      "userId": 42,
      "transactionId": "chatcmpl-abc123",
      "requestedService": "gpt-4o",
      "promptTokens": 25,
      "responseTokens": 10,
      "totalTokens": 35,
      "cost": 0.000650
    }
  ],
  "error": null
}
```

| Field | Type | Description |
|---|---|---|
| `id` | `integer` | Auto-incremented transaction ID |
| `time` | `string` | ISO 8601 timestamp when the transaction occurred |
| `userId` | `integer` | ID of the user who made the request |
| `transactionId` | `string` | Completion ID returned by the model (links to the chat response) |
| `requestedService` | `string` | Model identifier used for the request |
| `promptTokens` | `integer` | Number of prompt tokens consumed |
| `responseTokens` | `integer` | Number of response tokens generated |
| `totalTokens` | `integer` | Total tokens (`promptTokens + responseTokens`) |
| `cost` | `number` | Total credit cost of this transaction |

**Error Responses**

| Status | Condition |
|---|---|
| `401` | Missing or invalid JWT token |

---

## Chat History

### POST /chat-history/create

Creates a new chat history session, optionally pre-populated with an initial set of messages.

**Authentication:** Required

**Request Body**

```json
{
  "title": "My first conversation",
  "messages": [
    {
      "role": "user",
      "content": [{ "type": "text", "text": "Hi!" }]
    }
  ]
}
```

| Field | Type | Required | Description |
|---|---|---|---|
| `title` | `string \| null` | | Optional display title for the session |
| `messages` | `ChatMessage[]` | ✅ | Initial messages (may be an empty array `[]`) |

**Success Response `200`**

```json
{
  "isSuccess": true,
  "data": {
    "id": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
    "title": "My first conversation",
    "messages": [
      {
        "role": "user",
        "content": [{ "type": "text", "text": "Hi!" }]
      }
    ],
    "createdAt": "2024-06-15T10:00:00Z",
    "updatedAt": "2024-06-15T10:00:00Z"
  },
  "error": null
}
```

| Field | Type | Description |
|---|---|---|
| `id` | `string` | UUID identifying the session |
| `title` | `string \| null` | Session title |
| `messages` | `ChatMessage[]` | Full message list |
| `createdAt` | `string \| null` | ISO 8601 creation timestamp |
| `updatedAt` | `string \| null` | ISO 8601 last-update timestamp |

**Error Responses**

| Status | Condition |
|---|---|
| `401` | Missing or invalid JWT token |

---

### GET /chat-history/get/{id}

Retrieves a specific chat history session by its UUID.

**Authentication:** Required

**Path Parameters**

| Parameter | Type | Description |
|---|---|---|
| `id` | `string` | UUID of the chat history session |

**Request Body:** None

**Success Response `200`**

```json
{
  "isSuccess": true,
  "data": {
    "id": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
    "title": "My first conversation",
    "messages": [
      {
        "role": "user",
        "content": [{ "type": "text", "text": "Hi!" }]
      },
      {
        "role": "assistant",
        "content": [{ "type": "text", "text": "Hello! How can I help?" }]
      }
    ],
    "createdAt": "2024-06-15T10:00:00Z",
    "updatedAt": "2024-06-15T10:05:00Z"
  },
  "error": null
}
```

**Error Responses**

| Status | Condition |
|---|---|
| `401` | Missing or invalid JWT token |
| `404` | Session not found or does not belong to the current user |

---

### GET /chat-history/list

Returns a summary list of the current user's chat history sessions (no full message content, just counts).

Admins may pass a `userId` query parameter to list another user's sessions.

**Authentication:** Required

**Query Parameters**

| Parameter | Type | Required | Description |
|---|---|---|---|
| `userId` | `integer` | | *(Admin only)* List sessions belonging to this user. Ignored for non-admin callers. |

**Request Body:** None

**Success Response `200`**

```json
{
  "isSuccess": true,
  "data": [
    {
      "id": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
      "title": "My first conversation",
      "createdAt": "2024-06-15T10:00:00Z",
      "updatedAt": "2024-06-15T10:05:00Z",
      "messageCount": 4
    }
  ],
  "error": null
}
```

| Field | Type | Description |
|---|---|---|
| `id` | `string` | Session UUID |
| `title` | `string \| null` | Session title |
| `createdAt` | `string \| null` | ISO 8601 creation timestamp |
| `updatedAt` | `string \| null` | ISO 8601 last-update timestamp |
| `messageCount` | `integer` | Number of messages in the session |

**Error Responses**

| Status | Condition |
|---|---|
| `401` | Missing or invalid JWT token |

---

### POST /chat-history/update-title

Updates the title of an existing chat history session owned by the current user.

**Authentication:** Required

**Request Body**

```json
{
  "id": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
  "title": "Renamed conversation"
}
```

| Field | Type | Required | Description |
|---|---|---|---|
| `id` | `string` | ✅ | UUID of the session to update |
| `title` | `string` | ✅ | New title |

**Success Response `200`**

Returns the updated `ChatHistoryResponse` (same shape as [`POST /chat-history/create`](#post-chat-historycreate)).

**Error Responses**

| Status | Condition |
|---|---|
| `401` | Missing or invalid JWT token |
| `404` | Session not found or does not belong to the current user |

---

### POST /chat-history/update

Replaces the title and the **complete** message list of an existing chat history session.

**Authentication:** Required

**Request Body**

```json
{
  "id": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
  "title": "Updated conversation",
  "messages": [
    {
      "role": "user",
      "content": [{ "type": "text", "text": "Updated message" }]
    }
  ]
}
```

| Field | Type | Required | Description |
|---|---|---|---|
| `id` | `string` | ✅ | UUID of the session to update |
| `title` | `string \| null` | | New title (pass `null` to clear) |
| `messages` | `ChatMessage[]` | ✅ | Full replacement message list |

**Success Response `200`**

Returns the updated `ChatHistoryResponse` (same shape as [`POST /chat-history/create`](#post-chat-historycreate)).

**Error Responses**

| Status | Condition |
|---|---|
| `401` | Missing or invalid JWT token |
| `404` | Session not found or does not belong to the current user |

---

### POST /chat-history/append-messages

Appends one or more messages to the end of an existing chat history session.

**Authentication:** Required

**Request Body**

```json
{
  "id": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
  "messages": [
    {
      "role": "assistant",
      "content": [{ "type": "text", "text": "Here is the answer." }]
    }
  ]
}
```

| Field | Type | Required | Description |
|---|---|---|---|
| `id` | `string` | ✅ | UUID of the session to append to |
| `messages` | `ChatMessage[]` | ✅ | Messages to append |

**Success Response `200`**

Returns the updated `ChatHistoryResponse` (same shape as [`POST /chat-history/create`](#post-chat-historycreate)).

**Error Responses**

| Status | Condition |
|---|---|
| `401` | Missing or invalid JWT token |
| `404` | Session not found or does not belong to the current user |

---

### POST /chat-history/delete

Permanently deletes a chat history session owned by the current user.

**Authentication:** Required

**Request Body**

```json
{
  "id": "a1b2c3d4-e5f6-7890-abcd-ef1234567890"
}
```

| Field | Type | Required | Description |
|---|---|---|---|
| `id` | `string` | ✅ | UUID of the session to delete |

**Success Response `200`**

```json
{
  "isSuccess": true,
  "data": "Chat history a1b2c3d4-e5f6-7890-abcd-ef1234567890 deleted successfully",
  "error": null
}
```

**Error Responses**

| Status | Condition |
|---|---|
| `401` | Missing or invalid JWT token |
