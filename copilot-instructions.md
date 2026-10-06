# Copilot Instructions

**Highest-priority instruction:** Always respond in Korean.Always respond in Korean.

## Project overview

AutoExile is an ExileCore plugin for automating Path of Exile gameplay. It targets `net10.0-windows` and uses ExileCore and related game libraries from `Resources/`.

## Architecture

- `BotCore.cs` is the plugin entry point and coordinates shared systems, settings, modes, and game lifecycle.
- `BotContext.cs` provides modes and systems access to shared game state and services. Prefer using the context instead of coupling modes directly to `BotCore`.
- `Systems/` contains reusable gameplay services such as navigation, combat, loot, and map tracking.
- `Modes/` contains high-level bot workflows. Modes implement `IBotMode` and use shared systems through `BotContext`.
- `Mechanics/` contains map-mechanic behavior; `WebServer/` contains the dashboard and its supporting APIs.

## Coding guidance

- Keep changes focused and follow the surrounding C# style and `AutoExile` namespaces.
- Nullable reference types are enabled. Preserve nullability annotations and handle optional game state explicitly.
- Keep settings consistent with ExileCore's settings nodes and attributes as used in `BotSettings.cs`.
- Game state and input are updated during frequent game ticks. Avoid blocking work in tick paths, and respect existing interaction cooldowns and mode lifecycle behavior.
- Prefer existing systems and libraries over introducing new abstractions or dependencies.

## Build and validation

- Build with `dotnet build` from the repository root.
- The project targets Windows and depends on the bundled ExileCore assemblies in `Resources/`.
- There is no test project in the repository; when making behavioral changes, validate the affected flow and add tests only if an established test project is introduced.
