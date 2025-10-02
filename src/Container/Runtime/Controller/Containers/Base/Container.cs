using System.Collections.Generic;
using UnityEngine;

namespace Nk7.Container
{
    public abstract class Container : MonoBehaviour
    {
        public IDIContainer DIContainer { get; protected set; }

        [SerializeField] private GameObject[] _autoRegisterGameObjects;
        [SerializeField] private GameObject[] _autoResolveGameObjects;

        private List<IContainerRegistrable> _autoRegistrables;
        
        protected virtual void Register(IBaseDIService builder) { }
        protected virtual void Resolve() { }

        protected void OnDestroy()
        {
            if (_autoRegistrables == null)
            {
                return;
            }

            for (int i = 0; i < _autoRegistrables.Count; ++i)
            {
                var registrable = _autoRegistrables[i];

                DIContainer.Release(registrable.GetType());
            }
        }

        protected void AutoRegisterAll(IBaseDIService diService)
        {
            if (_autoRegisterGameObjects.Length <= 0)
            {
                return;
            }

            _autoRegistrables = new List<IContainerRegistrable>(_autoRegisterGameObjects.Length);

            for (int i = 0; i < _autoRegisterGameObjects.Length; ++i)
            {
                var registerGameObject = _autoRegisterGameObjects[i];

                if (registerGameObject == null)
                {
                    continue;
                }

                RegisterGameObject(diService, registerGameObject);
            }
        }

        protected void AutoResolveAll()
        {
            for (int i = 0; i < _autoResolveGameObjects.Length; ++i)
            {
                var resolveGameObject = _autoResolveGameObjects[i];

                if (resolveGameObject == null)
                {
                    continue;
                }

                ResolveGameObject(resolveGameObject);
            }
        }

        private void RegisterGameObject(IBaseDIService diService, GameObject registerGameObject)
        {
            using var bufferScope = MonoBehavioursBuffer.GetScoped(out var buffer);

            registerGameObject.GetComponents(buffer);

            for (var i = 0; i < buffer.Count; i++)
            {
                var monoBehaviour = buffer[i];

                if (monoBehaviour is IContainerRegistrable registrable)
                {
                    _autoRegistrables.Add(registrable);
                    diService.RegisterInstanceAsSelf(registrable);
                }
            }
        }

        private void ResolveGameObject(GameObject resolveGameObject)
        {
            using var bufferScope = MonoBehavioursBuffer.GetScoped(out var buffer);

            resolveGameObject.GetComponents(buffer);

            for (var i = 0; i < buffer.Count; i++)
            {
                var monoBehaviour = buffer[i];
                DIContainer.ResolveImplementation(monoBehaviour);
            }
        }
    }
}