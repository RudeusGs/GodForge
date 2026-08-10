# Dependency Graph and Impact Analysis

## Purpose

Build and visualize directed relationships among Godot scenes, scripts, resources and assets.

## Actors

Viewer and higher roles; Worker generates graph.

## Requirements

| ID | Requirement | Priority |
|---|---|---|
| FR-11 | Generate versioned dependency graph | Must |
| FR-11.1 | Filter, search and traverse direct/multi-hop relationships | Must |
| FR-11.2 | Detect missing and cyclic relationships | Must |
| FR-11.3 | Calculate reverse impact for changed files | Must |

## Main flow

1. Parser emits normalized entities/references.
2. Graph builder creates typed nodes for scenes, resources, assets, GDScript and Godot C# scripts, then preserves script edge semantics (`extends`, `preload`, `load`).
3. API returns a bounded subgraph based on root/filter/depth.
4. UI supports zoom, pan, search and detail navigation.
5. Incremental analysis uses reverse graph to expand affected scope.

## Error and edge cases

- Node/edge limit exceeded.
- Missing target reference.
- Cycle detected.
- Baseline graph unavailable for impact analysis.

## Authorization and security

- Graph queries are project/revision scoped.
- Never return protected asset download URLs in graph payload.
- Limit depth and node count to prevent denial of service.

## Async processing and idempotency

- No module-specific durable job is created by read operations.
- Writes, where present, use normal transaction/concurrency rules and do not perform hidden heavy work.

## Acceptance criteria

- `AC-FR-11-01`: Same metadata version yields same canonical graph.
- `AC-FR-11-02`: Missing/cyclic edges are represented with evidence.
- `AC-FR-11-03`: Impact results are validated against full-analysis corpus.

## Related API

- `GET /revisions/{sha}/graph`, impact and export endpoints

## Related data

- `analysis.dependency_graph_snapshots`, `analysis.dependency_graph_nodes`, `analysis.dependency_graph_edges`

## Tests and observability

- Test suite: `TC-GRAPH-*`, including canonical output, cycles, missing edges, impact and bounds.
- Metrics: node/edge count, graph build/query latency, truncation and impact-analysis fallback.
- Large-graph tests enforce depth/node limits.
