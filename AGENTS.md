# Contra-Like educational project

Unity version: 6000.3 LTS. The project is a teaching kit for students who build
games by placing prefabs and changing Inspector settings without editing code.
Unity Playground has been removed. Implement the game with project-owned code.
Do not use Computer Use, desktop input automation, or the computer-use skill.
Use filesystem edits and programmatic Unity Editor or batch-mode workflows.

## Student-facing Inspector

- Expose only obvious, useful gameplay settings: health, damage, movement speed,
  fire interval, wave composition, and the artwork or sound being selected.
- Public fields must represent such settings or meaningful UnityEvents. Do not
  expose internal state, cached components, counters, coroutine handles, timers,
  save internals, or bookkeeping identifiers.
- Private fields with SerializeField are also visible in the default Inspector.
  Use them only for intentional student-facing settings. Keep internal fields
  private and nonserialized. If a service reference must be serialized inside a
  prefab, hide it with HideInInspector and wire it during prefab authoring.
- Prefer GetComponent, RequireComponent, and prefab-local dependencies for
  mandatory service references. Avoid asking students to wire these manually.
- Give exposed fields clear names, short Russian Tooltips, meaningful units,
  sensible defaults, and relevant Min or Range constraints. Use small Header
  groups when they improve readability. Avoid decorative custom inspectors.
- Exposed data describes configuration. Runtime health, cooldowns, completion
  state, and campaign progress must not mutate shared ScriptableObject assets.

## Components and prefabs

- Keep each component responsible for one role: movement, weapon firing, health,
  wave spawning, arena flow, or displaying UI. Compose them into ready prefabs.
- Provide working base prefabs. Use prefab variants for enemies, bosses, and
  other gameplay variations instead of duplicating scripts.
- Keep view layout in authored scenes and prefabs. Create runtime UI by code
  only when the feature actually needs dynamic content.
- Use standard Unity components when they already solve the task. A static
  Canvas background does not need a custom MonoBehaviour.
- Add reusable ScriptableObject configuration types when implementing their
  actual feature; do not create speculative managers, frameworks, or stubs.
- Keep campaign progress, the current level run, and view presentation separate.
  Save progress through a dedicated persistence boundary when that feature is
  implemented.

## Events

- Use UnityEvents for discrete, understandable composition points students can
  connect in the Inspector: defeated, collected, arena cleared, or level won.
- Connect authored UI buttons through Button.onClick to clear public methods.
- Optional effects such as sounds, particles, doors, and visual feedback can be
  connected through these events in prefabs or scenes.
- Keep required internal invariants in code or typed C# events: one death per
  enemy, wave bookkeeping, damage validation, and valid state transitions must
  work even when an optional Inspector event has no listeners.
- Avoid using UnityEvents for per-frame movement or building a global event bus
  for this small project. Expose only events useful to the current exercise.

## Project layout and assets

- Main-menu scene: Assets/Scenes/Main Menu.unity.
- Reusable UI prefabs: Assets/Prefabs/UI. Runtime code goes under Assets/Scripts
  grouped by implemented feature; editor-only code belongs in Editor folders.
- Preserve existing asset GUIDs, sprite slicing, and Pixels Per Unit unless the
  user requests changes. Art import settings are Point filtering and no texture
  compression, including relevant platform settings.
- Preserve unrelated work. Verify scene and prefab changes in Unity and check
  compilation. Do not add runtime scripts or tests solely for a static visual.
