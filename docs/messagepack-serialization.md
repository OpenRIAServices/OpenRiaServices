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

## Model attributes

OpenRiaServices supports PolyType model attributes in addition to data contract attributes. Generated client models preserve these attributes so the server and client use the same MessagePack shape.

These rules follow PolyType's [DataContract support](https://eiriktsarpalis.github.io/PolyType/docs/shape-providers.html#datacontract-support).

Member inclusion follows PolyType precedence:

1. `[PropertyShape]` controls the member when present. `Ignore = true` excludes it; otherwise it is included, and `Name` controls its MessagePack name.
2. On a `[DataContract]` type, a member without `[PropertyShape]` is included only when it has `[DataMember]`.
3. On other types, `[IgnoreDataMember]` excludes a member that has no `[PropertyShape]`.

Consequently, `[PropertyShape]` takes precedence when combined with `[DataMember]` or `[IgnoreDataMember]`. A null `PropertyShape.Name` uses the CLR member name rather than falling back to `DataMember.Name`.

For polymorphic models, direct `[DerivedTypeShape]` declarations on a type take precedence over `[KnownType]` declarations on that same type. Registrations on base types are still included when OpenRiaServices computes the inheritance closure. Explicit discriminator names are recommended for a stable wire format.
