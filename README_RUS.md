# nk7-container

Легковесный DI-контейнер для Unity, построенный вокруг статического `CompositionRoot` и нацеленный на минимальные аллокации и простую интеграцию с MonoBehaviour-проектами.

## Возможности
- Регистрация зависимостей с жизненными циклами `Singleton`, `Scoped` и `Transient`
- Конструкторная, полевая, методная и property-инъекция через атрибут `[Resolve]`
- Автоматическая регистрация/резолв компонентов сцен через инспектор без дополнительного кода
- Работа с префабами и фабриками (`RegisterComponent`, `RegisterFactory`, `RegisterAbstractFactory`)
- Управление областями видимости через `IScopeService` и освобождение ресурсов
- События жизненного цикла приложения и сцен, доступные через `IContainerLifeCycle`
- Утилиты редактора: меню создания контейнеров, расстановка `DefaultExecutionOrder`, автодобавление define `NK7_CONTAINER`

## Содержание
- [Установка](#установка)
- [Основные элементы](#основные-элементы)
  - [CompositionRoot](#compositionroot)
  - [RootContainer](#rootcontainer)
  - [SubContainer](#subcontainer)
- [Регистрация зависимостей](#регистрация-зависимостей)
  - [Время жизни](#время-жизни)
  - [Регистрация экземпляров и компонентов](#регистрация-экземпляров-и-компонентов)
  - [Фабрики и делегаты](#фабрики-и-делегаты)
- [Получение зависимостей](#получение-зависимостей)
  - [Методы контейнера](#методы-контейнера)
  - [Атрибут Resolve](#атрибут-resolve)
- [Автоматическая регистрация и резолв MonoBehaviour](#автоматическая-регистрация-и-резолв-monobehaviour)
- [Области видимости](#области-видимости)
- [Жизненный цикл и освобождение ресурсов](#жизненный-цикл-и-освобождение-ресурсов)
- [Инструменты редактора и утилиты](#инструменты-редактора-и-утилиты)

## Установка

### Unity Package Manager

1. Откройте Unity Package Manager (`Window → Package Manager`).
2. Нажмите `+ → Add package from git URL…`.
3. Укажите `https://github.com/lsd7nk/nk7-container.git?path=/src/Container`.

UPM-пакеты из Git не обновляются автоматически, поэтому обновляйте ссылку вручную при необходимости.   
Или используйте [UPM Git Extension](https://github.com/mob-sakai/UpmGitExtension).

### Ручная установка

Скопируйте папку `src/Container` в свой проект Unity и подключите asmdef `Nk7.Container`.

## Основные элементы

### CompositionRoot

`CompositionRoot` — одиночка, который создаёт базовый DI-контейнер, хранит ссылки на `RootContainer` и слушает события приложения.   
Добавьте компонент в стартовую сцену и укажите массив `Root Containers` в инспекторе. Объект помечен `DontDestroyOnLoad` и уничтожает дубли.

### RootContainer

Наследуйтесь от `RootContainer`, чтобы описать регистрацию зависимостей и пост-инициализацию.

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

Добавьте созданный MonoBehaviour в список `Root Containers` у `CompositionRoot`.

### SubContainer

`SubContainer` предназначен для локальных контейнеров сцен и отдельных игровых объектов. Он автоматически инициализируется в `Awake` через `CompositionRoot`.

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

Разместите `SubContainer` на нужном GameObject в сцене — он получит доступ к общему DI-контейнеру.

## Регистрация зависимостей

### Время жизни

`IBaseDIService` предоставляет методы для регистрации с разным временем жизни:

```csharp
builder.RegisterSingleton<IAudioService, AudioService>();
builder.RegisterScoped<ISessionService, SessionService>();
builder.RegisterTransient<ILogger, DebugLogger>();

builder.RegisterSingleton<GameState>();              // регистрация типа как самого себя
builder.RegisterTransient(typeof(InputService));     // перегрузка с System.Type
```

- `Singleton` — единственный экземпляр на весь контейнер.
- `Scoped` — экземпляр на текущий scope (по умолчанию `0`).
- `Transient` — новый объект при каждом разрешении.

### Регистрация экземпляров и компонентов

```csharp
var settings = new GameSettings();

builder.RegisterInstance<ISettings>(settings);
builder.RegisterInstanceAsSelf(settings); // доступ по конкретному типу

builder.RegisterSingleton<PlayerService>()
       .AsImplementedInterfaces(); // добавляет все интерфейсы типа

builder.RegisterComponent(hudViewPrefab, uiRoot)
       .As<IHudView>();
```

`DescriptorRegistration` поддерживает методы `As<T>()`, `As(Type)` и `AsImplementedInterfaces()` для назначения дополнительных сервисных типов.

### Фабрики и делегаты

`RegisterExtensions` добавляет утилиты для работы с фабриками и фабричными методами.

```csharp
builder.RegisterSingletonByFunc(() => new SaveService(savePath));
builder.RegisterScopedByFunc(() => new SessionService());
builder.RegisterTransientByFunc(CreateEnemy);

builder.RegisterFactory<IEnemy>(DIContainer);
var factory = DIContainer.Resolve<IFactoryService<IEnemy>>();
var rangedEnemy = factory.GetService(typeof(RangedEnemy));

builder.RegisterAbstractFactory<IWeaponFactory, WeaponFactory>();
// WeaponFactory должен наследоваться от AbstractFactoryService и реализовывать IFactoryService
```

## Получение зависимостей

### Методы контейнера

`IDIContainer` регистрируется автоматически как singleton, поэтому его можно запросить через DI или использовать свойство `DIContainer` внутри контейнеров.

```csharp
var gameService = DIContainer.Resolve<IGameService>();
var behaviour = (IMenu)DIContainer.Resolve(typeof(IMenu));

DIContainer.ResolveImplementation(existingMonoBehaviour); // внедрение в уже созданный объект
```

Контейнер также регистрирует `IContainerLifeCycle` и `IScopeService`, их можно получать через `Resolve`.

### Атрибут Resolve

Вешайте `[Resolve]` на поля, свойства и методы — контейнер заполнит зависимости при создании объекта или при явном вызове `ResolveImplementation`.

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

## Автоматическая регистрация и резолв MonoBehaviour

Каждый `Container` имеет два списка в инспекторе:

- `Auto Register Game Objects` — компоненты на перечисленных объектах, реализующие `IContainerRegistrable`, автоматически регистрируются через `RegisterInstanceAsSelf` и будут освобождены при уничтожении контейнера.
- `Auto Resolve Game Objects` — контейнер вызовет `ResolveImplementation` для всех компонентов на объектах, заполняя `[Resolve]` зависимости без дополнительного кода.

```csharp
public class UiRoot : MonoBehaviour, IContainerRegistrable
{
    [Resolve] private IDialogService _dialogs;
}
```

Добавьте объект с `UiRoot` в `Auto Register Game Objects`, чтобы сервис оказался в контейнере.

## Области видимости

Сервисы со временем жизни `Scoped` создаются для текущего scope. Управлять ими можно через `IScopeService` или напрямую через `IDIContainer`.

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

Доступные методы: `CreateScope()`, `SetCurrentScope(int)`, `GetCurrentScope()`, `ReleaseScope(int)`. Scope с идентификатором `0` создаётся автоматически при старте контейнера.

## Жизненный цикл и освобождение ресурсов

Реализуйте `IContainerInitializable`, чтобы контейнер автоматически вызвал `Initialize()` после создания и внедрения зависимостей.

```csharp
public class ConfigService : IContainerInitializable
{
    public void Initialize()
    {
        // вызывается один раз после регистрации
    }
}
```

Подпишитесь на `IContainerLifeCycle`, чтобы получать уведомления о событиях приложения и сцен:

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

Для ручного освобождения используйте `IDIContainer.Release<T>()`, `Release(Type)` и `ReleaseAll()`. `CompositionRoot` автоматически вызывает `ReleaseAll()` при завершении работы.

## Инструменты редактора и утилиты

- `Tools → Nk7 → Container` содержит команды для быстрого создания `CompositionRoot` и `SubContainer`.
- В редакторе автоматически добавляется скриптовый define `NK7_CONTAINER` для основных платформ.
- `ScriptOrderUtils` синхронизирует значения `DefaultExecutionOrder`, чтобы контейнеры инициализировались до пользовательских скриптов.
- На платформах IL2CPP контейнер резервирует небольшой блок памяти через `NativeHeapUtils.ReserveMegabytes`, снижая вероятность аллокаций при старте.
- `LogsUtils` предоставляет цветные обёртки вокруг `Debug.Log*` для сообщений контейнера.
