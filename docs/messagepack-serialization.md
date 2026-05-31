# MessagePack Serialization Plan and Wire Format

This document proposes how to add MessagePack as an optional Open RIA Services wire format for the ASP.NET Core hosting stack and the `HttpDomainClient` stack. It is intentionally scoped to the pluggable serialization pipeline used by `OpenRiaServices.Hosting.AspNetCore` and `OpenRiaServices.Client.DomainClients.Http`.

## Goals

- Add an opt-in MessagePack provider without changing the existing binary XML or text XML behavior.
- Use `Nerdbank.MessagePack` for serialization.
- Use a version-tolerant envelope based on map keys rather than array positions.
- Keep GET query/invoke requests unchanged: parameters and query options remain URL-encoded strings.
- Preserve Open RIA Services metadata rules: `[Exclude]` members are never serialized, and navigation members marked with `[EntityAssociation]` are serialized only when included through `[Include]` for non-projection shapes.
- Support async-friendly streaming serialization/deserialization where the pipeline allows it.

## MIME type

Use `application/vnd.msgpack` for both request `Content-Type` and response `Accept` negotiation.

The server should accept media type parameters case-insensitively when matching, for example `application/vnd.msgpack; charset=utf-8`, but it should emit `application/vnd.msgpack`.

## Implementation plan

### 1. Shared wire contract decisions

1. Define the envelope maps described below as the compatibility contract between server and client.
2. Treat unknown map keys as extension data that must be ignored by readers.
3. Require readers to validate the `kind` value and the expected operation context before invoking service code.
4. Keep operation names in the envelope for diagnostics and validation, but route metadata remains authoritative.
5. Document that all domain values are encoded by `Nerdbank.MessagePack` using Open RIA Services type-shape filtering.

### 2. Server: `OpenRiaServices.Hosting.AspNetCore`

1. Add `MimeTypes.MessagePack = "application/vnd.msgpack"`.
2. Add a MessagePack serialization provider under `Framework/AspNetCore/Serialization/MessagePack`.
3. Add `OpenRiaServicesOptionsBuilder.AddMessagePackSerialization(bool defaultProvider = false)` and an overload that accepts MessagePack-specific options.
4. Construct the serializer with the Open RIA Services filtered `ITypeShapeProvider`, wrapping the existing branch-specific `FilteredTypeShapeProvider`.
5. Implement the `RequestSerializer` methods:
   - `ReadParametersFromBodyAsync`
   - `ReadSubmitRequestAsync`
   - `WriteResponseAsync`
   - `WriteSubmitResponseAsync`
   - `WriteErrorAsync`
6. Reuse existing content negotiation and default-provider behavior so MessagePack can be enabled alongside binary XML and text XML.
7. Return `BadHttpRequestException` for invalid envelope shape, invalid operation kind, missing required members, or non-nullable null parameters.

### 3. Client: `OpenRiaServices.Client.DomainClients.Http`

1. Add `MessagePackHttpDomainClientFactory` beside `BinaryHttpDomainClientFactory` and `XmlHttpDomainClientFactory`.
2. Add `MessagePackHttpDomainClient` beside the data-contract HTTP clients.
3. Reuse existing URI composition for GET requests.
4. Build POST request envelopes for query/invoke and submit requests using the schema below.
5. Parse success and fault envelopes from MessagePack responses.
6. Preserve the current HTTP status mapping behavior when the response body cannot be parsed as the selected wire format.

### 4. Tests

Add focused tests at the same layers as the existing ASP.NET Core and HTTP client serialization tests:

- MIME negotiation selects MessagePack when `Content-Type`/`Accept` is `application/vnd.msgpack`.
- Query POST can round-trip parameters and query options.
- Invoke POST can round-trip nullable and non-null parameters.
- Submit can round-trip a list of `ChangeSetEntry` values and return submit results.
- Fault responses deserialize to `DomainServiceFault` and keep the existing client exception behavior.
- GET requests remain URL encoded and do not use a MessagePack request body.
- Unknown envelope keys are ignored.
- Missing required keys, wrong `kind`, and null for non-nullable parameters fail before service invocation.
- `[Exclude]` members are absent from serialized payloads.
- `[EntityAssociation]` navigation members are absent unless allowed by `[Include]` for non-projection serialization.

## Wire format

All top-level payloads are MessagePack maps. Key names are strings. Readers must ignore keys they do not understand. Writers should emit keys in the order shown for readability, but readers must not depend on ordering.

`protocolVersion` starts at `1`. A future reader may accept higher versions only if it can safely ignore all unknown additions.

### Common domain value encoding

Domain values are encoded by `Nerdbank.MessagePack` using the Open RIA Services filtered type-shape provider.

Rules:

- CLR null is encoded as MessagePack nil.
- Primitive values use the native MessagePack representation selected by `Nerdbank.MessagePack`.
- Entity, complex, and projection values use map-based object encoding from the filtered type shape.
- Collections use MessagePack arrays for item order.
- The envelope uses maps for version tolerance; collection contents may use arrays where the collection semantics require ordering.

### Query and invoke POST request

Used when a query or invoke operation uses POST. GET behavior is unchanged and continues to use URL-encoded URI parameters and query options.

```text
{
  "protocolVersion": 1,
  "kind": "query" | "invoke",
  "operation": "OperationName",
  "parameters": {
    "parameterName": <domain-value-or-nil>
  },
  "query": {
    "includeTotalCount": true | false,
    "parts": [
      { "operator": "where", "expression": "..." },
      { "operator": "orderby", "expression": "..." },
      { "operator": "skip", "expression": "10" },
      { "operator": "take", "expression": "25" }
    ]
  }
}
```

Required keys: `protocolVersion`, `kind`, `operation`, `parameters`.

Optional keys:

- `query`: only meaningful for query operations. Omitted is equivalent to no query options and `includeTotalCount = false`.
- `query.parts`: omitted is equivalent to an empty list.

Validation:

- `kind` must match the domain operation being invoked.
- `operation` must match the routed operation name.
- Each required operation parameter must be present.
- Nil is allowed only for nullable parameters.
- Unknown parameter names should be rejected to catch client/server contract drift.

### Submit request

```text
{
  "protocolVersion": 1,
  "kind": "submit",
  "operation": "Submit",
  "changeSet": [
    <ChangeSetEntry>,
    <ChangeSetEntry>
  ]
}
```

Required keys: `protocolVersion`, `kind`, `operation`, `changeSet`.

`changeSet` is an ordered MessagePack array of `ChangeSetEntry` values encoded through the filtered type-shape provider.

### Success response

Used for query, invoke, and submit success responses.

```text
{
  "protocolVersion": 1,
  "kind": "response",
  "operation": "OperationName",
  "result": <domain-value-or-nil>
}
```

For submit responses, `operation` is `Submit` and `result` is the ordered collection of resulting `ChangeSetEntry` values.

For query responses, `result` is the existing query result shape for the operation, including total count data when requested by the service pipeline.

For void invoke operations, `result` is nil.

### Fault response

A failed domain operation response uses the same HTTP status behavior as the existing serializers, but the body is a MessagePack fault envelope when the negotiated response format is MessagePack.

```text
{
  "protocolVersion": 1,
  "kind": "fault",
  "operation": "OperationName",
  "fault": <DomainServiceFault>
}
```

Required keys: `protocolVersion`, `kind`, `operation`, `fault`.

`fault` is a `DomainServiceFault` encoded through the filtered type-shape provider. Readers should construct the same client-side exception type and `OperationErrorStatus` mapping that the current HTTP client uses for XML fault responses.

## Compatibility notes

- Existing `application/msbin1` and `application/xml` clients remain unaffected unless MessagePack is explicitly configured as the default provider.
- The MessagePack provider should be additive and removable through `ClearSerializationProviders`, matching XML provider behavior.
- The wire format intentionally does not mimic the XML element names such as `MessageRoot` or `SubmitChangesResponse`; the operation name is retained in a stable map key instead.
- Envelope keys are stable public protocol names. Renaming CLR properties or internal DTOs must not change the emitted key names.

## Open questions before implementation

- Confirm `application/vnd.msgpack` as the final MIME type, or choose a vendor-specific Open RIA Services subtype such as `application/vnd.openriaservices.msgpack`.
- Decide whether to reject higher `protocolVersion` values by default or allow them with best-effort unknown-key skipping.
- Decide whether the first implementation should expose all `Nerdbank.MessagePack` options or only `ITypeShapeProvider` and serializer configuration needed by Open RIA Services.
