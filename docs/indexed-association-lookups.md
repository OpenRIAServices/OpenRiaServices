# Indexed association lookups

OpenRiaServices keeps internal in-memory indexes for `EntitySet` identity and metadata-backed association lookups.

- `EntitySet` primary-key lookup uses a dedicated index instead of repeatedly scanning entities.
- `EntityRef<TEntity>` and `EntityCollection<TEntity>` automatically use association indexes when the relationship can be described from metadata.
- Single-member association keys use typed accessors, which reduces boxing and temporary allocations on lookup and index maintenance.
- Existing predicate-based association behavior is still preserved; when metadata cannot describe the relationship, the existing fallback path is used.

## Performance characteristics

The main benefit is improved worst-case work per lookup for normal relationship navigation:

- Before indexing, resolving a relationship typically required an `O(n)` scan of the target `EntitySet`.
- With the internal indexes, the key lookup is typically `O(1)` on average, followed by `O(k)` work to inspect only the matching bucket (`k` = number of entities sharing the same association key).
- Incremental updates also avoid rebuilding the whole index by tracking the previously indexed key per entity.

The worst-case remains `O(n)` when many entities share the same key, or when a relationship must fall back to custom predicate evaluation, but the common path avoids repeated full-set scans and significantly reduces per-lookup allocations.
