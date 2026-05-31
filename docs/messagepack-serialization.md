# MessagePack serialization wire schema (draft)

OpenRiaServices supports MessagePack with MIME type `application/vnd.msgpack`.

## Request envelope (POST/QUERY/Submit)

Top-level object map:

- `parameters`: map<string, bin?>  
  Parameter name to MessagePack payload bytes for the declared parameter type. `nil` means `null`.
- `queryOptions`: array of `ServiceQueryPart` (optional)
- `includeTotalCount`: bool (optional)

## Success response envelope

Top-level object map:

- `result`: bin?  
  MessagePack payload bytes for the declared operation return type. `nil` means `null` / no value.

## Fault response envelope

Top-level object map:

- `fault`: `DomainServiceFault`

## Notes

- GET query behavior stays unchanged (URL-encoded query parameters).
- Envelopes are map-based for schema/version tolerance.
