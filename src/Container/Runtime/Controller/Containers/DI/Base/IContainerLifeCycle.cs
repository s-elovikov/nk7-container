using System;

namespace Nk7.Container
{
    public interface IContainerLifeCycle
    {
        event Action<bool> OnApplicationFocusEvent;
        event Action<bool> OnApplicationPauseEvent;

        event Action<int> OnSceneUnloadedEvent;
        event Action<int> OnSceneLoadedEvent;

        event Action OnFixedUpdateEvent;
        event Action OnUpdateEvent;
    }
}