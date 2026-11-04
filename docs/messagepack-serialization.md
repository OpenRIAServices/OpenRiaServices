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
is included in the original values and the complex-object type has
`[RoundtripOriginal]`.

Extracted state includes a retained-data entry even when no unrecognized members
exist. Copying the state into an ordinary dictionary preserves this clearing
signal when applying state or updating original values.

This applies only to the MessagePack transport. Keep serialized member names
stable when evolving a contract.

## Model attributes

OpenRiaServices supports PolyType model attributes in addition to data contract attributes. Generated client models preserve these attributes so the server and client use the same MessagePack shape.

Server serialization surrogates preserve `[PropertyShape]` and the property-level Nerdbank.MessagePack attributes `[Key]`, `[MessagePackConverter]`, and `[UseComparer]`.

A property-level `[MessagePackConverter]` is not supported when the property's type is an exposed entity or complex type. `[MessagePackConverter]` on exposed entity or complex types and `[TypeShape(Marshaler = ...)]` on those types are also unsupported; MessagePack serializer setup rejects them. Only `Kind` is copied from `[TypeShape]`; method inclusion is not propagated. `[DerivedTypeShape]` is not copied to surrogates: `ObjectConverter` handles discriminators using the original model metadata. This does not change client model generation; client and server discriminator keys must still match.

These rules follow PolyType's [DataContract support](https://eiriktsarpalis.github.io/PolyType/docs/shape-providers.html#datacontract-support).

Member inclusion follows PolyType precedence:

1. `[PropertyShape]` controls the member when present. `Ignore = true` excludes it; otherwise it is included, and `Name` controls its MessagePack name.
2. On a `[DataContract]` type, a member without `[PropertyShape]` is included only when it has `[DataMember]`.
3. On other types, `[IgnoreDataMember]` excludes a member that has no `[PropertyShape]`.

Consequently, `[PropertyShape]` takes precedence when combined with `[DataMember]` or `[IgnoreDataMember]`. A null `PropertyShape.Name` uses the CLR member name rather than falling back to `DataMember.Name`.

For polymorphic models, direct `[DerivedTypeShape]` declarations on a type take precedence over `[KnownType]` declarations on that same type. Registrations on base types are still included when OpenRiaServices computes the inheritance closure. Explicit discriminator names are recommended for a stable wire format.
