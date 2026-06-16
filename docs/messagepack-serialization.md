# MessagePack serialization wire schema (draft)

OpenRiaServices supports MessagePack with MIME type `application/vnd.msgpack`.

## Request envelope (POST/QUERY/Submit)

Top-level object map:

- `Parameters`: map<string, value?>  
  Parameter name to a MessagePack value serialized with the declared parameter type converter. `nil` means `null`.
- `QueryOptions`: array of `ServiceQueryPart` (optional)
- `IncludeTotalCount`: bool (optional)

## Success response envelope

Top-level object map:

- `Result`: value?  
  MessagePack value for the declared operation return type. `nil` means `null` / no value.

## Fault response envelope

Top-level object map:

- `Fault`: `DomainServiceFault`
## Notes

- GET query behavior stays unchanged (URL-encoded query parameters).
- Envelopes are map-based for schema/version tolerance.
