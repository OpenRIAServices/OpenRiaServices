# MessagePack serialization wire schema (draft)

OpenRiaServices supports MessagePack with MIME type `application/vnd.msgpack`.

## Request envelope (POST/QUERY/Submit)

Top-level object map:

- `Parameters`: map<string, value?>\
  Parameter name to a MessagePack value serialized with the declared parameter type converter. `nil` means `null`.
- `QueryOptions`: array of `ServiceQueryPart` (optional)
- `IncludeTotalCount`: bool (optional)

## Success response envelope

Top-level object map:

- `Result`: value?\
  MessagePack value for the declared operation return type. `nil` means `null` / no value.

## Fault response envelope

Top-level object map:

- `Fault`: `DomainServiceFault`
## Notes

- GET query behavior stays unchanged (URL-encoded query parameters).
- Envelopes are map-based for schema/version tolerance.

## Forward versioning

MessagePack clients automatically retain unrecognized members on `Entity` and
`ComplexObject`. An older client can load, edit, and submit data from a newer
server without discarding properties that the client does not yet define.
No changes to generated client types are required.

Retained data follows entity state copies, edit cancellation, and load behavior:
`KeepCurrent` keeps the existing data, while `MergeIntoCurrent` and
`RefreshCurrent` use the incoming data. Entity-level `[RoundtripOriginal]`
also retains unrecognized members in the original entity submitted to the server.
Unrecognized complex-object members are retained when their containing member
is included in the original values.

This applies only to the MessagePack transport. Keep serialized member names
stable when evolving a contract.
