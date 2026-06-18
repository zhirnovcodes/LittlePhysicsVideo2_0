---
name: adding-scene-visuals
description: Add a new visual scene script to the LittlePhysicsVideo Unity project. Use when the user asks to add a visual scene, create a new visual, implement a scene animation, or describe a new visual step/sequence using DOTween and UniTask.
---

# Adding a New Scene Visual

Each visual scene is a `VisualBase` subclass placed in `Assets/Scripts/Visual/`.

## Infrastructure (already in place)

| File | Purpose |
|------|---------|
| `VisualBase.cs` | Abstract MonoBehaviour. Override `Init()` and `Run(IVisualController)` |
| `IVisualController.cs` | `UniTask<bool> WaitNextClicked()` — Space→true, Esc→false |
| `VisualController.cs` | Input System implementation (`UnityEngine.InputSystem`, `com.unity.inputsystem` package required) |
| `VisualSceneBootstrapper.cs` | Inits all visuals on Start, runs them in sequence |

---

## Two-Step Workflow

**Never write method bodies until the user approves the skeleton.**

### Step 1 — Skeleton (send for approval)

When the user provides a visual prompt:
1. Create `Assets/Scripts/Visual/<VisualName>Visual.cs`
2. Populate only `[SerializeField] private` fields that represent the inputs: prefabs, transforms, cameras, speeds, durations, placeholder objects, materials, etc. — infer these entirely from the prompt
3. Leave `Init()` and `Run()` as empty method stubs (no body, just `{ }`)
4. Present the file and ask the user to approve before filling in the bodies

### Step 2 — Implementation (after approval)

Fill in `Init()` and `Run()` following all the rules below.

---

## Method Rules

### Init()
- Called **once per simulation** by `VisualSceneBootstrapper` on Start — it is **not** called again on re-run, so there is no need to destroy or clean up previously spawned objects
- Instantiates or caches scene objects needed for the visual
- Sets every animated object to its starting state (hidden, zero scale, starting position, etc.)

### Run(IVisualController controller)
- Written with UniTask + DOTween only — **no coroutines, no callbacks, no nested lambdas**
- All logic is **linear / inline** — read top to bottom like a script
- Every line has a comment above it describing what it does in plain English
- If a logical step requires more than 3 lines of code, extract it into a `private` helper method with a descriptive name
- If the script has many major steps (4+), extract each major step into its own `private async UniTask` method (e.g. `RunStep1Appear()`, `RunStep2FocusCamera()`). `Run()` then reads as a flat sequence of awaited calls separated by input gates
- After each meaningful beat, gate on input:

```csharp
// Wait for the user to press Space to continue
_  = await controller.WaitNextClicked();

```

---

## Full Example

```csharp
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

namespace LittlePhysics
{
    public class BallDropVisual : VisualBase
    {
        [SerializeField] private GameObject BallPrefab;
        [SerializeField] private Transform SpawnPoint;
        [SerializeField] private Transform GroundPoint;
        [SerializeField] private float FallDuration;
        [SerializeField] private float ScaleInDuration;

        private GameObject SpawnedBall;

        public override void Init()
        {
            // Destroy any previously spawned ball from a prior run
            if (SpawnedBall != null)
            {
                Destroy(SpawnedBall);
            }

            // Instantiate the ball at the spawn point, hidden at zero scale
            SpawnedBall = Instantiate(BallPrefab, SpawnPoint.position, Quaternion.identity);
            SpawnedBall.transform.localScale = Vector3.zero;
        }

        public override async UniTask Run(IVisualController controller)
        {
            // Scale the ball in so it appears on screen
            await SpawnedBall.transform.DOScale(Vector3.one, ScaleInDuration).ToUniTask();

            // Wait for the user to press Space to continue, or Esc to abort
            bool next = await controller.WaitNextClicked();
            if (!next)
            {
                return;
            }

            // Drop the ball down to the ground
            await SpawnedBall.transform.DOMove(GroundPoint.position, FallDuration).ToUniTask();

            // Wait for the user to press Space to continue, or Esc to abort
            next = await controller.WaitNextClicked();
            if (!next)
            {
                return;
            }

            // Squash the ball on impact
            await SquashOnImpact();
        }

        private async UniTask SquashOnImpact()
        {
            // Squash down on the Y axis and stretch on X
            await SpawnedBall.transform.DOScale(new Vector3(1.4f, 0.6f, 1.4f), 0.08f).ToUniTask();

            // Bounce back to normal scale
            await SpawnedBall.transform.DOScale(Vector3.one, 0.12f).ToUniTask();
        }
    }
}
```

---

## Key Patterns

- **Sequences**: use `DOTween.Sequence().Append(...).Join(...).AppendInterval(...).ToUniTask()` for parallel/chained tweens — still one awaited line
- **Gating**: always `bool next = await controller.WaitNextClicked(); if (!next) { return; }` — never skip the null check
- **No coroutines**: never use `IEnumerator`, `StartCoroutine`, or `yield return` in visual scripts
- **No callbacks**: never use `.OnComplete(...)` — await the tween instead

---

## Angle and Rotation Input Convention

All angle and rotation-speed fields exposed in `[SerializeField]` input data **must be expressed in degrees** (matching Unity Inspector conventions). Inside method bodies, they must be converted to radians before use in any calculation:

```csharp
[SerializeField] private float RotationSpeed = 10f;   // degrees/sec — inspector value
[SerializeField] private float StartAngle    = 45f;   // degrees — inspector value

// Inside Run() / Init():
float rotationRadPerSec = RotationSpeed * Mathf.Deg2Rad;
float startAngleRad     = StartAngle    * Mathf.Deg2Rad;
```

- Never pass a raw degree value to `Mathf.Sin`, `Mathf.Cos`, `Quaternion.AngleAxis` (which already takes degrees — exception, see below), or any API that expects radians.
- `Quaternion.AngleAxis(angle, axis)` is the **only** Unity API that takes degrees natively; you may pass the degree field to it directly without converting.
- Always keep the `* Mathf.Deg2Rad` conversion at the point of use, not pre-stored in another `[SerializeField]`.

---

## Position Input Patterns

### Predefined Position — Transform Placeholder

When a visual needs a fixed, designer-controlled position (spawn point, target, anchor, etc.), expose a `Transform` field and read its `.position` at runtime. The field is wired to an empty GameObject in the scene whose position is set in the Inspector.

```csharp
[SerializeField] private Transform SpawnPoint;
[SerializeField] private Transform TargetPoint;

// Usage inside Init() or Run():
SpawnedObject.transform.position = SpawnPoint.position;
await SpawnedObject.transform.DOMove(TargetPoint.position, MoveDuration).ToUniTask();
```

- Name the field after its semantic role (`SpawnPoint`, `TargetPoint`, `LandingPoint`, etc.)
- Never hard-code world-space coordinates — always delegate to the placeholder Transform

### Grid Cell Layout — Unity Grid Component

When the user asks for a "grid" to spawn cells into, use Unity's `Grid` component instead of a plain `Transform`. Expose it as:

```csharp
[SerializeField] private Grid MapGrid;
```

Position each cell using `MapGrid.CellToLocal(new Vector3Int(i, j, 0))` and parent it to `MapGrid.transform`:

```csharp
for (int j = 0; j < MapSize; j++)
{
    for (int i = 0; i < MapSize; i++)
    {
        Vector3 localPos = MapGrid.CellToLocal(new Vector3Int(i, j, 0));
        GameObject cell = Instantiate(CellPrefab, MapGrid.transform);
        cell.transform.localPosition = localPos;
    }
}
```

- `Grid` is a Unity built-in component (`UnityEngine`) — no extra imports needed.
- `CellToLocal` returns the bottom-left corner of the cell in the grid's local space, matching Unity's Tilemap conventions.
- The `Grid` GameObject's cell size and gap are configured in the Inspector.

---

### Random Position in a Range — Transform Range

When a visual needs to pick a random position within a rectangular region, expose a single `Transform` field. Wire it to an empty GameObject in the scene whose **position** is the region center and whose **scale** is the full extents of the region. Use `MathHelpers.GetRandomPosition(range)` to sample from it.

```csharp
[SerializeField] private Transform SpawnRange;

// Usage inside Init() or Run():
Vector3 randomPos = MathHelpers.GetRandomPosition(SpawnRange);
```

- `SpawnRange` — empty GameObject; set its position to the region center and its scale to the desired width/height/depth
- `MathHelpers.GetRandomPosition` reads `lossyScale * 0.5f` as half-extents and returns a uniform random point inside the box
- Call it inside `Init()` to fix positions per run, or inside `Run()` to pick a fresh position each time
- For 2-D randomness (e.g. XY plane only), set the range GameObject's Z scale to 0

## Continuous Y-Axis Camera Rotation

To spin a camera rotation handle continuously around its Y axis (e.g. orbit effect), read the handle's current euler angles first so X and Z are preserved, then tween only Y with `RotateMode.FastBeyond360` and `SetLoops(-1)`:

```csharp
[SerializeField] private Transform CameraRotationHandle;
[SerializeField] private float RotationSpeed = 5f;   // degrees/sec, sign = direction

float rotationDuration = 360f / Mathf.Abs(RotationSpeed);
Vector3 startEuler = CameraRotationHandle.localEulerAngles;
CameraRotationHandle.DOLocalRotate(
    new Vector3(startEuler.x, startEuler.y + Mathf.Sign(RotationSpeed) * 360f, startEuler.z),
    rotationDuration,
    RotateMode.FastBeyond360
).SetLoops(-1);
```

- **Do not** use `new Vector3(0f, ..., 0f)` — that resets X and Z to zero each loop.
- Always snapshot `localEulerAngles` before the tween and build the target vector from it.
- Do **not** await this tween; fire-and-forget so the rest of `Run()` continues while the rotation plays.

---

## Camera Movement with CameraHandler

`CameraHandler` drives the camera rig through four transforms and exposes `GetData()` / `SetData()` / `CameraHandlerData.Lerp()`.

### Pattern — lerp from current to a target position

1. Keep one **active** `CameraHandler` that always reflects the live camera state.
2. Place a second **inactive** `CameraHandler` in the scene (`GameObject.SetActive(false)`) whose transforms are positioned where the camera should end up. Wire it to the visual's `[SerializeField]` field.
3. In `Run()`, snapshot both ends and drive the interpolation with `DOVirtual.Float`:

```csharp
[SerializeField] private CameraHandler ActiveCamera;
[SerializeField] private CameraHandler TargetCameraHandler; // inactive, positioned in scene

// Snapshot the start and end states
CameraHandlerData startData  = ActiveCamera.GetData();
CameraHandlerData targetData = TargetCameraHandler.GetData();

// Lerp the camera from its current state to the target over MoveDuration seconds
await DOVirtual.Float(0f, 1f, MoveDuration, t =>
{
    ActiveCamera.SetData(startData.Lerp(targetData, t));
}).ToUniTask();
```

- The target `CameraHandler` is **never activated** — it exists only as a data source.
- `CameraHandlerData.Lerp` uses `Vector3.Lerp` for position, `Quaternion.Slerp` for rotation, and `Mathf.Lerp` for zoom and ortho size.
- To ease the motion, set the tween's ease before awaiting: `.SetEase(Ease.InOutQuad)`.

---

## Adding to the Scene

1. Attach the new component to a GameObject in the scene
2. Wire up all `[SerializeField]` fields in the Inspector
3. Add the component to the **Visuals** list on `VisualSceneBootstrapper` in the correct order
