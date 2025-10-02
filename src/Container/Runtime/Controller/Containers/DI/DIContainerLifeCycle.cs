using System.Runtime.CompilerServices;
using System;

namespace Nk7.Container
{
    public sealed partial class DIContainer : IContainerLifeCycle
    {
        public event Action<bool> OnApplicationFocusEvent;
        public event Action<bool> OnApplicationPauseEvent;

        public event Action<int> OnSceneUnloadedEvent;
        public event Action<int> OnSceneLoadedEvent;

        public event Action OnFixedUpdateEvent;
        public event Action OnUpdateEvent;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CallApplicationFocus(bool focus)
        {
            OnApplicationFocusEvent?.Invoke(focus);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CallApplicationPause(bool pause)
        {
            OnApplicationPauseEvent?.Invoke(pause);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CallSceneUnloaded(int sceneIndex)
        {
            OnSceneUnloadedEvent?.Invoke(sceneIndex);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CallSceneLoaded(int sceneIndex)
        {
            OnSceneLoadedEvent?.Invoke(sceneIndex);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CallFixedUpdate()
        {
            OnFixedUpdateEvent?.Invoke();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CallUpdate()
        {
            OnUpdateEvent?.Invoke();
        }
    }
}