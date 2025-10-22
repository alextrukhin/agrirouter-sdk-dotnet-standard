# Agrirouter REST API

A simple HTTP REST API for encoding and decoding agrirouter messages.

## Overview

This REST API provides two endpoints:
1. **Encode** - Encodes a message payload into an agrirouter-compatible format
2. **Decode** - Decodes a message received from agrirouter and automatically parses common payload types

## Running the API

### Option 1: Using .NET CLI (Development)

```bash
cd agrirouter-rest-api
dotnet run
```

The API will start on `http://localhost:5000`

### Option 2: Using Docker (Recommended for Production)

```bash
# Using Docker Compose (easiest)
docker-compose up -d

# Or using Docker directly
docker build -t agrirouter-rest-api .
docker run -d -p 5000:5000 --name agrirouter-api agrirouter-rest-api
```

See **[DOCKER_QUICKSTART.md](DOCKER_QUICKSTART.md)** for quick Docker commands or **[DOCKER.md](DOCKER.md)** for comprehensive Docker documentation.

## API Endpoints

### 1. Encode Message

**Endpoint:** `POST /api/message/encode`

**Content-Type:** `multipart/form-data`

**Parameters:**
- `PayloadFile` (required): File containing the message payload (binary data)
- `TechnicalMessageType` (required): The technical message type (e.g., "iso:11783:-10:device_description:protobuf")
- `ApplicationMessageId` (optional): Custom application message ID (will be generated if not provided)
- `TeamSetContextId` (optional): Team set context ID
- `TypeUrl` (optional): Type URL for the message payload
- `Mode` (optional): Message mode - "direct" or "publish" (default: "direct")
- `Recipients` (optional): Comma-separated list of recipient IDs (required for direct mode)

**Example using cURL:**
```bash
curl -X POST http://localhost:5000/api/message/encode \
  -F "PayloadFile=@/path/to/payload.bin" \
  -F "TechnicalMessageType=iso:11783:-10:device_description:protobuf" \
  -F "Mode=direct" \
  -F "Recipients=recipient-id-1,recipient-id-2"
```

**Response:**
```json
{
  "encodedMessage": "Base64EncodedMessageString...",
  "applicationMessageId": "generated-or-provided-id",
  "error": null
}
```

### 2. Decode Message

**Endpoint:** `POST /api/message/decode`

**Content-Type:** `multipart/form-data`

**Parameters:**
- `EncodedMessage` (optional): Base64 encoded message string
- `MessageFile` (optional): File containing the base64 encoded message

*Note: Either `EncodedMessage` or `MessageFile` must be provided.*

**Example using cURL with string:**
```bash
curl -X POST http://localhost:5000/api/message/decode \
  -F "EncodedMessage=XgjJARACGiQ5MD..."
```

**Example using cURL with file:**
```bash
curl -X POST http://localhost:5000/api/message/decode \
  -F "MessageFile=@/path/to/encoded-message.txt"
```

**Response:**
```json
{
  "responseCode": 201,
  "applicationMessageId": "message-id",
  "responseBodyType": "AckForFeedMessage",
  "timestamp": "1234567890",
  "payloadTypeUrl": "types.agrirouter.com/agrirouter.commons.Messages",
  "payloadValueRaw": "Base64EncodedPayloadValue...",
  "decodedPayload": {
    "messages": [
      {
        "message": "Feed message confirmation confirmed.",
        "messageCode": "VAL_000206",
        "args": []
      }
    ]
  },
  "error": null
}
```

#### Automatic Payload Decoding

The decode endpoint automatically parses the following protobuf message types:

1. **agrirouter.commons.Messages** - Error/success messages from agrirouter
   ```json
   "decodedPayload": {
     "messages": [
 {
         "message": "Success message",
         "messageCode": "VAL_000206",
         "args": []
       }
     ]
   }
   ```

2. **agrirouter.feed.response.HeaderQueryResponse** - Feed header query responses
   ```json
   "decodedPayload": {
  "queryMetrics": {
       "totalMessagesInQuery": 5,
 "maxCountRestriction": 100
     },
     "feed": [
       {
         "senderId": "endpoint-id",
"headers": [...]
     }
]
   }
   ```

3. **agrirouter.feed.response.MessageQueryResponse** - Feed message query responses
   ```json
   "decodedPayload": {
     "queryMetrics": {...},
     "messages": [
       {
         "header": {...},
      "content": "...",
     "contentBase64": "..."
       }
     ]
   }
   ```

For unknown message types, the raw base64 value is returned.

## Error Handling

All endpoints return appropriate HTTP status codes:
- `200 OK` - Successful operation
- `400 Bad Request` - Missing or invalid parameters (including truncated or malformed base64)
- `500 Internal Server Error` - Processing error

Error responses include an `error` field with details:
```json
{
  "error": "Error description here"
}
```

### Common Decode Errors

| Error Message | Cause | Solution |
|--------------|-------|----------|
| "Invalid base64 format: ..." | Truncated or corrupted message | Ensure complete message is provided |
| "Encoded message is empty or contains only whitespace" | Empty input | Provide valid base64 string |
| "Invalid base64 string format. Length: X characters..." | Invalid base64 format | Check message integrity |
| "Decoding failed: There was an error during decoding of the message" | Invalid protobuf structure | Message may be incomplete or corrupted |

**See [TROUBLESHOOTING.md](TROUBLESHOOTING.md) for detailed guidance on handling decode errors.**

## Testing with Postman

1. Create a new POST request
2. Set URL to `http://localhost:5000/api/message/encode` or `decode`
3. In Body tab, select `form-data`
4. Add the required parameters
5. For file uploads, select "File" type in the dropdown for the parameter
6. Send the request

## Architecture

This API is a thin wrapper around the existing agrirouter SDK services:
- Uses `EncodeMessageService` from the SDK for encoding
- Uses `DecodeMessageService` from the SDK for decoding
- Automatically parses common protobuf payload types into JSON objects
- Uses **Newtonsoft.Json** for JSON serialization (supports dynamic objects better than System.Text.Json)
- Enhanced input validation and error handling for better debugging
- No additional business logic - pure I/O interface via HTTP

### JSON Serialization

The API uses Newtonsoft.Json (Json.NET) with the following settings:
- **ReferenceLoopHandling**: Ignore - Prevents circular reference errors
- **NullValueHandling**: Ignore - Omits null properties for cleaner responses
- **Formatting**: Indented - Human-readable JSON output

## Configuration

The server configuration can be modified in `appsettings.json`:
```json
{
  "Kestrel": {
    "Endpoints": {
      "Http": {
        "Url": "http://localhost:5000"
      }
    }
  }
}
```

## Security Note

This API runs on HTTP only (no SSL/TLS). It is intended for local development and testing purposes. 
For production use, implement appropriate security measures including:
- HTTPS/TLS encryption
- Authentication and authorization
- Input validation and sanitization
- Rate limiting
- Logging and monitoring

## Troubleshooting

### Diagnostic Tool

A PowerShell diagnostic tool is included to help identify message issues:

```powershell
cd agrirouter-rest-api
.\test-decode.ps1 -MessageFile "your-message.txt"
```

The tool will:
- ? Validate Base64 format
- ? Check message length
- ? Test with the API
- ? Provide specific error diagnosis
- ? Suggest solutions

See **[DIAGNOSTIC_TOOL.md](DIAGNOSTIC_TOOL.md)** for detailed usage guide.

### Message Decoding Issues

If you encounter decode errors:
1. **Use the diagnostic tool first:** `.\test-decode.ps1 -MessageFile "message.txt"`
2. Verify the message is complete (not truncated)
3. Check for proper base64 formatting
4. Remove any extra whitespace or line breaks
5. See **[TROUBLESHOOTING.md](TROUBLESHOOTING.md)** for detailed solutions

### JSON Serialization Errors

If you encounter serialization errors with `DecodedPayload`:
1. Ensure the `Microsoft.AspNetCore.Mvc.NewtonsoftJson` package is installed (v3.1.32)
2. Verify `Startup.cs` is configured to use Newtonsoft.Json serializer
3. See **[SERIALIZATION_FIX.md](SERIALIZATION_FIX.md)** for more details

### Build Issues

If the build fails with file lock errors:
```bash
# Stop any running instances
Stop-Process -Name "agrirouter-rest-api" -Force -ErrorAction SilentlyContinue
Start-Sleep -Seconds 2

# Then rebuild
dotnet build
```

## Documentation

- **[README.md](README.md)** - This file, main documentation
- **[DOCKER_QUICKSTART.md](../DOCKER_QUICKSTART.md)** - Quick Docker commands (NEW!)
- **[DOCKER.md](../DOCKER.md)** - Comprehensive Docker deployment guide (NEW!)
- **[EXAMPLES.md](Examples/EXAMPLES.md)** - Detailed usage examples
- **[DIAGNOSTIC_TOOL.md](DIAGNOSTIC_TOOL.md)** - PowerShell diagnostic tool guide
- **[TROUBLESHOOTING.md](TROUBLESHOOTING.md)** - Handling decode errors and message issues
- **[SERIALIZATION_FIX.md](SERIALIZATION_FIX.md)** - Fix for JSON serialization issues
- **[QUICK_REFERENCE.md](QUICK_REFERENCE.md)** - Quick reference for common issues
