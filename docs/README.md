# API Documentation

This directory contains detailed API reference documentation for the **Azure OpenAI Selfhost Backend**.

## Documents

| Document | Description |
|---|---|
| [api-user.md](api-user.md) | Endpoints available to all authenticated users (login, chat, model listing, transaction history, chat history) |
| [api-admin.md](api-admin.md) | Endpoints that require the **Admin** role (user management, model management, full transaction and chat-history access) |

## Common Conventions

### Base URL

```
http://<host>:<port>
```

The default development port is `5131`; the default production port is `5000`.

### Authentication

All protected endpoints require a **JWT Bearer token** in the `Authorization` header:

```
Authorization: Bearer <token>
```

Obtain a token by calling [`POST /user/auth`](api-user.md#post-userauth).

### Response Envelope

Every endpoint returns a JSON object with the following shape:

```json
{
  "isSuccess": true,
  "data": <endpoint-specific payload>,
  "error": null
}
```

| Field | Type | Description |
|---|---|---|
| `isSuccess` | `boolean` | `true` on success, `false` on error |
| `data` | any | The response payload; `null` on error |
| `error` | `string \| null` | Human-readable error message; `null` on success |

### Common HTTP Status Codes

| Code | Meaning |
|---|---|
| `200` | Success |
| `400` | Bad request / invalid payload |
| `401` | Missing or invalid JWT token, or wrong credentials |
| `402` | Insufficient credits |
| `403` | Authenticated but lacking the required role |
| `404` | Requested resource not found |
| `409` | Conflict (e.g., duplicate username) |
| `500` | Unexpected server or database error |
