# nk7-container

Lightweight DI container for Unity built around a static `CompositionRoot`, focused on minimal allocations and straightforward integration with MonoBehaviour projects.

## Features
- Register dependencies with `Singleton`, `Scoped`, and `Transient` lifetimes
- Constructor, field, method, and property injection via the `[Resolve]` attribute
- Automatic registration/resolution of scene components through the inspector without extra code
- Prefab and factory support (`RegisterComponent`, `RegisterFactory`, `RegisterAbstractFactory`)
- Scope management via `IScopeService` and resource disposal
- Application and scene lifecycle events exposed through `IContainerLifeCycle`
- Editor utilities: container creation menu, `DefaultExecutionOrder` setup

## Contents
- [Installation](#installation)
- [Core Elements](#core-elements)
  - [CompositionRoot](#compositionroot)
  - [RootContainer](#rootcontainer)
  - [SubContainer](#subcontainer)
- [Registering Dependencies](#registering-dependencies)
  - [Lifetimes](#lifetimes)
  - [Registering Instances and Components](#registering-instances-and-components)
  - [Factories and Delegates](#factories-and-delegates)
- [Resolving Dependencies](#resolving-dependencies)
  - [Container Methods](#container-methods)
  - [Resolve Attribute](#resolve-attribute)
- [Automatic MonoBehaviour Registration and Resolution](#automatic-monobehaviour-registration-and-resolution)
- [Scopes](#scopes)
- [Lifecycle and Disposal](#lifecycle-and-disposal)
- [Editor Tools and Utilities](#editor-tools-and-utilities)

## Installation

### Unity Package Manager

1. Open Unity Package Manager (`Window → Package Manager`).
2. Click `+ → Add package from git URL…`.
3. Enter `https://github.com/lsd7nk/nk7-container.git?path=/src/Container`.

UPM packages pulled from Git are not updated automatically, so refresh the URL manually when needed.  
Alternatively, use the [UPM Git Extension](https://github.com/mob-sakai/UpmGitExtension).

### Manual Installation

Copy the `src/Container` folder into your Unity project and reference the `Nk7.Container` asmdef.

## Core Elements

### CompositionRoot

`CompositionRoot` is a singleton that creates the base DI container, stores references to `RootContainer`, and listens to application events.  
Add the component to the starting scene and configure the `Root Containers` array in the inspector. The object is marked `DontDestroyOnLoad` and removes duplicates.

### RootContainer

Inherit from `RootContainer` to describe dependency registrations and post-initialization logic.

```csharp
using Nk7.Container;
using UnityEngine;

public sealed class GameRoot : RootContainer
{
    public override void Register(IBaseDIService builder)
    {
        builder.RegisterSingleton<IGameService, GameService>();
        builder.RegisterScoped<ILevelContext, LevelContext>();
        builder.RegisterTransient<IEnemyFactory, EnemyFactory>();

        builder.RegisterInstance(new GameConfig("prod"))
               .AsImplementedInterfaces();
    }

    public override void Resolve()
    {
        var gameService = DIContainer.Resolve<IGameService>();
        gameService.Initialize();
    }
}
```

Add the new MonoBehaviour to the `Root Containers` list in `CompositionRoot`.

### SubContainer

`SubContainer` targets scene-local containers and specific GameObjects. It is automatically initialized in `Awake` via `CompositionRoot`.

```csharp
using Nk7.Container;
using UnityEngine;

public sealed class LevelContainer : SubContainer
{
    [SerializeField] private EnemyController enemyPrefab;
    [SerializeField] private Transform enemyParent;

    public override void Register(IBaseDIService builder)
    {
        builder.RegisterScoped<LevelState>();
        builder.RegisterComponent(enemyPrefab, enemyParent)
               .AsImplementedInterfaces();
    }

    public override void Resolve()
    {
        DIContainer.Resolve<LevelState>().Setup();
    }
}
```

Place the `SubContainer` on the required GameObject in the scene and it will gain access to the shared DI container.

## Registering Dependencies

### Lifetimes

`IBaseDIService` exposes methods for registering dependencies with different lifetimes:

```csharp
builder.RegisterSingleton<IAudioService, AudioService>();
builder.RegisterScoped<ISessionService, SessionService>();
builder.RegisterTransient<ILogger, DebugLogger>();

builder.RegisterSingleton<GameState>();              // register type as itself
builder.RegisterTransient(typeof(InputService));     // overload accepting System.Type
```

- `Singleton` — a single instance for the entire container.
- `Scoped` — an instance per current scope (defaults to `0`).
- `Transient` — a new object on every resolution.

### Registering Instances and Components

```csharp
var settings = new GameSettings();

builder.RegisterInstance<ISettings>(settings);
builder.RegisterInstanceAsSelf(settings); // access by concrete type

builder.RegisterSingleton<PlayerService>()
       .AsImplementedInterfaces(); // adds all implemented interfaces

builder.RegisterComponent(hudViewPrefab, uiRoot)
       .As<IHudView>();
```

`DescriptorRegistration` supports `As<T>()`, `As(Type)`, and `AsImplementedInterfaces()` for assigning additional service types.

### Factories and Delegates

`RegisterExtensions` provides helpers for working with factories and factory methods.

```csharp
builder.RegisterSingletonByFunc(() => new SaveService(savePath));
builder.RegisterScopedByFunc(() => new SessionService());
builder.RegisterTransientByFunc(CreateEnemy);

builder.RegisterFactory<IEnemy>(DIContainer);
var factory = DIContainer.Resolve<IFactoryService<IEnemy>>();
var rangedEnemy = factory.GetService(typeof(RangedEnemy));

builder.RegisterAbstractFactory<IWeaponFactory, WeaponFactory>();
// WeaponFactory must inherit from AbstractFactoryService and implement IFactoryService
```

## Resolving Dependencies

### Container Methods

`IDIContainer` is registered automatically as a singleton, so you can request it through DI or use the `DIContainer` property inside containers.

```csharp
var gameService = DIContainer.Resolve<IGameService>();
var behaviour = (IMenu)DIContainer.Resolve(typeof(IMenu));

DIContainer.ResolveImplementation(existingMonoBehaviour); // inject into an existing object
```

The container also registers `IContainerLifeCycle` and `IScopeService`; they can be resolved as needed.

### Resolve Attribute

Decorate fields, properties, and methods with `[Resolve]` — the container will populate dependencies when the object is created or when `ResolveImplementation` is invoked manually.

```csharp
public class PlayerController : MonoBehaviour
{
    [Resolve] private IInputService _input;
    [Resolve] private IWeaponService WeaponService { get; set; }

    [Resolve]
    private void Init(IGameService gameService)
    {
        gameService.RegisterPlayer(this);
    }
}
```

## Automatic MonoBehaviour Registration and Resolution

Every `Container` exposes two lists in the inspector:

- `Auto Register Game Objects` — components on these objects that implement `IContainerRegistrable` are registered automatically via `RegisterInstanceAsSelf` and will be released when the container is destroyed.
- `Auto Resolve Game Objects` — the container calls `ResolveImplementation` for all components on these objects, filling `[Resolve]` dependencies without extra code.

```csharp
public class UiRoot : MonoBehaviour, IContainerRegistrable
{
    [Resolve] private IDialogService _dialogs;
}
```

Add the GameObject with `UiRoot` to `Auto Register Game Objects` to load the service into the container.

## Scopes

Services registered with the `Scoped` lifetime are created for the current scope. Manage them through `IScopeService` or directly via `IDIContainer`.

```csharp
public class LevelManager
{
    private readonly IScopeService _scope;
    private int _levelScope;

    public LevelManager(IScopeService scope)
    {
        _scope = scope;
    }

    public void LoadLevel()
    {
        _levelScope = _scope.CreateScope();
        _scope.SetCurrentScope(_levelScope);
    }

    public void UnloadLevel()
    {
        _scope.ReleaseScope(_levelScope);
    }
}
```

Available methods: `CreateScope()`, `SetCurrentScope(int)`, `GetCurrentScope()`, `ReleaseScope(int)`. Scope `0` is created automatically when the container starts.

## Lifecycle and Disposal

Implement `IContainerInitializable` to have the container automatically call `Initialize()` after creation and dependency injection.

```csharp
public class ConfigService : IContainerInitializable
{
    public void Initialize()
    {
        // called once after registration
    }
}
```

Subscribe to `IContainerLifeCycle` to receive application and scene event notifications:

```csharp
public class SceneLogger
{
    public SceneLogger(IContainerLifeCycle lifeCycle)
    {
        lifeCycle.OnSceneLoadedEvent += index => Debug.Log($"Scene {index} loaded");
        lifeCycle.OnSceneUnloadedEvent += index => Debug.Log($"Scene {index} unloaded");
        lifeCycle.OnApplicationFocusEvent += hasFocus => Debug.Log($"Focus: {hasFocus}");
        lifeCycle.OnApplicationPauseEvent += isPaused => Debug.Log($"Pause: {isPaused}");
    }
}
```

For manual cleanup use `IDIContainer.Release<T>()`, `Release(Type)`, and `ReleaseAll()`. `CompositionRoot` automatically invokes `ReleaseAll()` on shutdown.

## Editor Tools and Utilities

- `Tools → Nk7 → Container` contains commands for quickly creating `CompositionRoot` and `SubContainer`.
- `ScriptOrderUtils` synchronizes `DefaultExecutionOrder` values so containers initialize before user scripts.
- On IL2CPP platforms the container reserves a small memory block via `NativeHeapUtils.ReserveMegabytes` to reduce startup allocations.
- `LogsUtils` provides colored wrappers around `Debug.Log*` for container messages.
