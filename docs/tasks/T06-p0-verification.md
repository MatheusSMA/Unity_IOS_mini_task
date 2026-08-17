# T06 — P0 Editor verification checkpoint

**Phase**: 1 (P0 Bootstrap) | **Requirement**: BOOT-01 | **Depends on**: T04, T05 | **Tests**: none | **Gate**: build

## Functional description

The BOOT-01 independent test, executed: proves the environment is real before any P1 work starts. This is the only task in the project that produces no file — it produces evidence.

## Technical description

Run in the Unity Editor via Unity MCP (fallback AD-006: human performs the same steps and reports):

1. Open project → read console → **zero errors** (BOOT-01 AC3).
2. `run_tests` EditMode → green, 0 tests; `run_tests` PlayMode → green, 0 tests (AC4).
3. Switch build target to **iOS** → no build-settings errors (AC5); switch back.
4. Confirm URP asset active (a scene object renders through URP, AC1) and packages resolved (AC2).

Record the result in the commit body (which checks ran, via MCP or human).

## Done when

- [ ] Console zero errors on open
- [ ] Test Runner green (0 tests) in both modes
- [ ] iOS target switch clean

**Tools**: unity-mcp + unity-mcp-skill (fallback: human) | **Commit**: `[chore] record P0 verification result`
