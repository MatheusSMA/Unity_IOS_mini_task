# T07 — Create domain data types

**Phase**: 2 (Domain core) | **Requirement**: ROOM-01 (types), WIN-04 (WindowSpec.id) | **Depends on**: none | **Tests**: none (entity layer) | **Gate**: build

## Functional description

The vocabulary of the whole feature. Every later task speaks in these types: what a surface is (wall/floor/ceiling), what a window is, what a room is, which rejection reasons exist, which app modes exist.

## Technical description

- **File**: `Assets/Scripts/Domain/DomainTypes.cs` (Domain asmdef, plain C#).
- Types exactly per design Data Models:

```csharp
enum SurfaceKind { Wall, Floor, Ceiling }
struct Rect2D { float x, y, w, h; }          // surface-local meters, origin bottom-left
class SurfaceDefinition { int id; string name; SurfaceKind kind; Vector3 origin, right, up; float width, height; float thickness = 0.15f; }
class WindowSpec { int id; int surfaceId; Rect2D rect; }   // id needed for deletion (WIN-04)
class RoomDefinition { List<SurfaceDefinition> surfaces; }
class MeshData { Vector3[] vertices; int[] triangles; Vector2[] uvs; }
enum WindowRejection { None, Overlap, TooSmall, TooLarge, MarginViolation, OutOfBounds, InvalidSurfaceKind }
enum Mode { Orbit, WindowDraw, Ar, TopDown }
```

- `Vector3`/`Vector2` from UnityEngine are acceptable in Domain (value types, no scene dependency); everything else engine-free (AD-002).
- Thickness serialized default 0.15 m (AD-009).

## Done when

- [ ] All 8 types compile in the Domain assembly
- [ ] Thickness default 0.15f (build gate)

**Tools**: none | **Commit**: `[feat] add domain data types`
