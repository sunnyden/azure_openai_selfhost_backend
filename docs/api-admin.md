# Admin API Reference

Endpoints that require the **Admin** role.  
All requests must include a valid JWT Bearer token for an admin account:

```
Authorization: Bearer <admin-jwt-token>
```

Non-admin requests to these endpoints will receive a `403 Forbidden` response.

> **Note:** Admin users can also call all endpoints listed in [api-user.md](api-user.md).

---

## Table of Contents

- [User Management](#user-management)
  - [POST /user/create](#post-usercreate)
  - [POST /user/update](#post-userupdate)
  - [GET /user/list](#get-userlist)
  - [POST /user/delete](#post-userdelete)
- [Model Management](#model-management)
  - [GET /model/all](#get-modelall)
  - [POST /model/add](#post-modeladd)
  - [POST /model/update](#post-modelupdate)
  - [POST /model/delete](#post-modeldelete)
  - [POST /model/assign](#post-modelassign)
  - [POST /model/unassign](#post-modelunassign)
- [Transactions](#transactions)
  - [GET /transaction/all](#get-transactionall)
- [Chat History](#chat-history)
  - [GET /chat-history/all](#get-chat-historyall)
  - [GET /chat-history/list?userId=\<id\>](#get-chat-historylistuseridid)
  - [POST /chat-history/delete-all](#post-chat-historydelete-all)

---

## User Management

### POST /user/create

Creates a new user account.

**Authentication:** Required — Admin only

**Request Body**

```json
{
  "userName": "bob",
  "password": "initial_password",
  "isAdmin": false,
  "remainingCredit": 5.00,
  "creditQuota": 10.00
}
```

| Field | Type | Required | Description |
|---|---|---|---|
| `userName` | `string` | ✅ | Unique username for the new account |
| `password` | `string` | ✅ | Initial plaintext password |
| `isAdmin` | `boolean` | ✅ | Grant admin role when `true` |
| `remainingCredit` | `number` | ✅ | Starting credit balance |
| `creditQuota` | `number` | ✅ | Periodic credit allowance refilled automatically |

**Success Response `200`**

```json
{
  "isSuccess": true,
  "data": {
    "id": 43,
    "userName": "bob",
    "isAdmin": false,
    "remainingCredit": 5.00,
    "creditQuota": 10.00,
    "lastCreditReset": null
  },
  "error": null
}
```

| Field | Type | Description |
|---|---|---|
| `id` | `integer` | Auto-assigned unique user identifier |
| `userName` | `string` | Username |
| `isAdmin` | `boolean` | Whether the user has the admin role |
| `remainingCredit` | `number` | Current credit balance |
| `creditQuota` | `number` | Periodic credit allowance |
| `lastCreditReset` | `string \| null` | ISO 8601 timestamp of the last credit reset, or `null` |

> **Note:** The `password` field is never returned in any response.

**Error Responses**

| Status | Condition |
|---|---|
| `401` | Missing or invalid JWT token |
| `403` | Authenticated user is not an admin |
| `409` | A user with the same username already exists |

---

### POST /user/update

Updates an existing user's profile. Supply `id` to identify the user.  
Omit or leave `password` empty to keep the existing password.

**Authentication:** Required — Admin only

**Request Body**

```json
{
  "id": 43,
  "userName": "bob_updated",
  "password": "new_password",
  "isAdmin": true,
  "remainingCredit": 7.50,
  "creditQuota": 15.00
}
```

| Field | Type | Required | Description |
|---|---|---|---|
| `id` | `integer` | ✅ | ID of the user to update |
| `userName` | `string` | ✅ | New username |
| `password` | `string \| null` | | New plaintext password. Leave empty/`null` to keep the current password |
| `isAdmin` | `boolean` | ✅ | Grant or revoke admin role |
| `remainingCredit` | `number` | ✅ | New credit balance |
| `creditQuota` | `number` | ✅ | New periodic credit allowance |

**Success Response `200`**

Returns the updated user profile (same shape as [`POST /user/create`](#post-usercreate)).

**Error Responses**

| Status | Condition |
|---|---|
| `401` | Missing or invalid JWT token |
| `403` | Authenticated user is not an admin |
| `404` | No user with the specified `id` was found |

---

### GET /user/list

Returns a list of all user accounts registered in the system.

**Authentication:** Required — Admin only

**Request Body:** None

**Success Response `200`**

```json
{
  "isSuccess": true,
  "data": [
    {
      "id": 1,
      "userName": "alice",
      "isAdmin": true,
      "remainingCredit": 8.50,
      "creditQuota": 10.00,
      "lastCreditReset": "2024-06-01T00:00:00Z"
    },
    {
      "id": 43,
      "userName": "bob",
      "isAdmin": false,
      "remainingCredit": 5.00,
      "creditQuota": 10.00,
      "lastCreditReset": null
    }
  ],
  "error": null
}
```

Each element has the same fields as the user profile described in [`POST /user/create`](#post-usercreate).

**Error Responses**

| Status | Condition |
|---|---|
| `401` | Missing or invalid JWT token |
| `403` | Authenticated user is not an admin |

---

### POST /user/delete

Permanently deletes a user account.

**Authentication:** Required — Admin only

**Request Body**

```json
{
  "userId": 43
}
```

| Field | Type | Required | Description |
|---|---|---|---|
| `userId` | `integer` | ✅ | ID of the user to delete |

**Success Response `200`**

Returns the deleted user's profile (same shape as [`POST /user/create`](#post-usercreate)).

**Error Responses**

| Status | Condition |
|---|---|
| `401` | Missing or invalid JWT token |
| `403` | Authenticated user is not an admin |
| `404` | No user with the specified `userId` was found |

---

## Model Management

### GET /model/all

Returns **all** AI models registered in the system, regardless of user assignment.

**Authentication:** Required — Admin only

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
| `identifier` | `string` | Unique model identifier (used in chat requests and assignments) |
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
| `403` | Authenticated user is not an admin |

---

### POST /model/add

Registers a new AI model.

**Authentication:** Required — Admin only

**Request Body**

```json
{
  "identifier": "gpt-4o-mini",
  "friendlyName": "GPT-4o Mini",
  "endpoint": "https://my-resource.openai.azure.com/",
  "deployment": "gpt-4o-mini",
  "key": "your-azure-openai-access-key",
  "costPromptToken": 0.0000001,
  "costResponseToken": 0.0000004,
  "isVision": false,
  "maxTokens": 128000,
  "supportTool": true,
  "apiVersionOverride": null,
  "reasoningModel": false
}
```

| Field | Type | Required | Description |
|---|---|---|---|
| `identifier` | `string` | ✅ | Unique model identifier (must be unique across all models) |
| `friendlyName` | `string` | ✅ | Human-readable display name |
| `endpoint` | `string` | ✅ | Azure OpenAI resource endpoint URL |
| `deployment` | `string` | ✅ | Azure deployment name |
| `key` | `string` | ✅ | Azure OpenAI access key (required; never returned in responses) |
| `costPromptToken` | `number` | ✅ | Cost per prompt token (in credit units) |
| `costResponseToken` | `number` | ✅ | Cost per response token (in credit units) |
| `isVision` | `boolean` | ✅ | Set `true` if the model supports image inputs |
| `maxTokens` | `integer` | ✅ | Maximum context window in tokens |
| `supportTool` | `boolean` | ✅ | Set `true` if the model supports tool/function calling |
| `apiVersionOverride` | `string \| null` | | Custom Azure API version; `null` uses the default |
| `reasoningModel` | `boolean` | ✅ | Set `true` for reasoning models (e.g. o-series) |

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
| `400` | `key` is missing or the payload is otherwise invalid |
| `401` | Missing or invalid JWT token |
| `403` | Authenticated user is not an admin |

---

### POST /model/update

Updates the configuration of an existing model identified by `identifier`.  
Omit or leave `key` empty/`null` to keep the existing access key.

**Authentication:** Required — Admin only

**Request Body**

```json
{
  "identifier": "gpt-4o-mini",
  "friendlyName": "GPT-4o Mini (Updated)",
  "endpoint": "https://my-resource.openai.azure.com/",
  "deployment": "gpt-4o-mini",
  "key": null,
  "costPromptToken": 0.0000002,
  "costResponseToken": 0.0000006,
  "isVision": true,
  "maxTokens": 128000,
  "supportTool": true,
  "apiVersionOverride": "2024-02-01",
  "reasoningModel": false
}
```

| Field | Type | Required | Description |
|---|---|---|---|
| `identifier` | `string` | ✅ | Identifier of the model to update |
| `friendlyName` | `string` | ✅ | New display name |
| `endpoint` | `string` | ✅ | Updated Azure resource endpoint |
| `deployment` | `string` | ✅ | Updated Azure deployment name |
| `key` | `string \| null` | | New access key. Omit or pass `null`/empty string to keep the current key |
| `costPromptToken` | `number` | ✅ | Updated prompt token cost |
| `costResponseToken` | `number` | ✅ | Updated response token cost |
| `isVision` | `boolean` | ✅ | Updated vision capability flag |
| `maxTokens` | `integer` | ✅ | Updated max context window |
| `supportTool` | `boolean` | ✅ | Updated tool-calling capability flag |
| `apiVersionOverride` | `string \| null` | | Updated API version override |
| `reasoningModel` | `boolean` | ✅ | Updated reasoning model flag |

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
| `401` | Missing or invalid JWT token |
| `403` | Authenticated user is not an admin |
| `404` | No model with the specified `identifier` was found |

---

### POST /model/delete

Removes a model from the system. This also removes all user assignments to that model.

**Authentication:** Required — Admin only

**Request Body**

```json
{
  "model": "gpt-4o-mini"
}
```

| Field | Type | Required | Description |
|---|---|---|---|
| `model` | `string` | ✅ | Identifier of the model to delete |

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
| `401` | Missing or invalid JWT token |
| `403` | Authenticated user is not an admin |

---

### POST /model/assign

Grants a user access to a specific model. After assignment the model appears in that user's [`GET /model/list`](api-user.md#get-modellist) response.

**Authentication:** Required — Admin only

**Request Body**

```json
{
  "userId": 43,
  "modelIdentifier": "gpt-4o"
}
```

| Field | Type | Required | Description |
|---|---|---|---|
| `userId` | `integer` | ✅ | ID of the user to grant access to |
| `modelIdentifier` | `string` | ✅ | Identifier of the model to assign |

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
| `401` | Missing or invalid JWT token |
| `403` | Authenticated user is not an admin |

---

### POST /model/unassign

Revokes a user's access to a specific model.

**Authentication:** Required — Admin only

**Request Body**

```json
{
  "userId": 43,
  "modelIdentifier": "gpt-4o"
}
```

| Field | Type | Required | Description |
|---|---|---|---|
| `userId` | `integer` | ✅ | ID of the user to revoke access from |
| `modelIdentifier` | `string` | ✅ | Identifier of the model to unassign |

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
| `401` | Missing or invalid JWT token |
| `403` | Authenticated user is not an admin |

---

## Transactions

### GET /transaction/all

Returns the billing transaction history for **all users** in the system.

**Authentication:** Required — Admin only

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
    },
    {
      "id": 1002,
      "time": "2024-06-15T11:00:00Z",
      "userId": 43,
      "transactionId": "chatcmpl-def456",
      "requestedService": "gpt-4o-mini",
      "promptTokens": 100,
      "responseTokens": 50,
      "totalTokens": 150,
      "cost": 0.000080
    }
  ],
  "error": null
}
```

| Field | Type | Description |
|---|---|---|
| `id` | `integer` | Auto-incremented transaction ID |
| `time` | `string` | ISO 8601 timestamp of the request |
| `userId` | `integer` | ID of the user who made the request |
| `transactionId` | `string` | Completion ID returned by the model |
| `requestedService` | `string` | Model identifier used |
| `promptTokens` | `integer` | Prompt tokens consumed |
| `responseTokens` | `integer` | Response tokens generated |
| `totalTokens` | `integer` | Total tokens (`promptTokens + responseTokens`) |
| `cost` | `number` | Total credit cost |

**Error Responses**

| Status | Condition |
|---|---|
| `401` | Missing or invalid JWT token |
| `403` | Authenticated user is not an admin |

---

## Chat History

### GET /chat-history/all

Returns a summary list of **all** chat history sessions across all users.

**Authentication:** Required — Admin only

**Request Body:** None

**Success Response `200`**

```json
{
  "isSuccess": true,
  "data": [
    {
      "id": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
      "title": "Alice's conversation",
      "createdAt": "2024-06-15T10:00:00Z",
      "updatedAt": "2024-06-15T10:05:00Z",
      "messageCount": 6
    },
    {
      "id": "b2c3d4e5-f6a7-8901-bcde-f12345678901",
      "title": null,
      "createdAt": "2024-06-15T11:00:00Z",
      "updatedAt": "2024-06-15T11:02:00Z",
      "messageCount": 2
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
| `403` | Authenticated user is not an admin |

---

### GET /chat-history/list?userId=\<id\>

The shared [`GET /chat-history/list`](api-user.md#get-chat-historylist) endpoint accepts an optional `userId` query parameter **when called by an admin**, allowing retrieval of another user's session list.

**Authentication:** Required — Admin role required to use the `userId` parameter

**Query Parameters**

| Parameter | Type | Required | Description |
|---|---|---|---|
| `userId` | `integer` | | ID of the user whose chat histories to list. If omitted, the caller's own histories are returned. |

**Example Request**

```
GET /chat-history/list?userId=43
Authorization: Bearer <admin-jwt-token>
```

**Success Response `200`**

Same shape as [`GET /chat-history/list`](api-user.md#get-chat-historylist).

**Error Responses**

| Status | Condition |
|---|---|
| `401` | Missing or invalid JWT token |

---

### POST /chat-history/delete-all

Permanently deletes **all** chat history sessions belonging to a specific user.

**Authentication:** Required — Admin only

**Request Body**

```json
{
  "userId": 43
}
```

| Field | Type | Required | Description |
|---|---|---|---|
| `userId` | `integer` | ✅ | ID of the user whose chat histories should be deleted |

**Success Response `200`**

```json
{
  "isSuccess": true,
  "data": "Deleted 5 chat histories for user 43",
  "error": null
}
```

`data` is a human-readable confirmation string indicating how many sessions were deleted.

**Error Responses**

| Status | Condition |
|---|---|
| `401` | Missing or invalid JWT token |
| `403` | Authenticated user is not an admin |
