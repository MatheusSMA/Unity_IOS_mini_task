# T05 — Create assembly definitions + empty test assemblies

**Phase**: 1 (P0 Bootstrap) | **Requirement**: BOOT-01 | **Depends on**: T01 | **Tests**: none | **Gate**: build

## Functional description

Establishes the architecture boundary in the compiler: the plain-C# domain layer compiles apart from the MonoBehaviour presentation layer (AD-002), and two empty test assemblies make the Test Runner green with zero tests (BOOT-01 AC4) — the proof the test pipeline works before any real test exists.

## Technical description

Four asmdefs (context7-verified layout for Unity 6):

| asmdef | Location | Key settings |
| ------ | -------- | ------------ |
| `Domain` | `Assets/Scripts/Domain/` | No engine-heavy references; keeps domain EditMode-testable (AD-002) |
| `Presentation` | `Assets/Scripts/Presentation/` (covers `UI/` too or a sibling `UI` asmdef referencing it) | references Domain, Unity.InputSystem, Unity.XR.ARFoundation, Unity.TextMeshPro |
| `Tests.EditMode` | `Assets/Tests/EditMode/` | `includePlatforms: ["Editor"]`, `defineConstraints: ["UNITY_INCLUDE_TESTS"]`, references Domain, Presentation, nunit |
| `Tests.PlayMode` | `Assets/Tests/PlayMode/` | references `UnityEngine.TestRunner`, `Unity.InputSystem`, `Unity.InputSystem.TestFramework` (for InputTestFixture), Domain, Presentation; `UNITY_INCLUDE_TESTS` constraint |

- `overrideReferences: true` + `precompiledReferences: ["nunit.framework.dll"]` on test asmdefs as needed.

## Done when

- [ ] 4 asmdefs compile; Test Runner shows both assemblies
- [ ] Test Runner green with zero tests in EditMode AND PlayMode (build gate)

**Tools**: none (text assets) | **Commit**: `[chore] add assembly definitions and empty test assemblies`
