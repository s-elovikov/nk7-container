using UnityEngine.SceneManagement;
using UnityEngine;
using System;

namespace Nk7.Container
{
    [DefaultExecutionOrder(-7000)]
    public sealed class CompositionRoot : MonoBehaviour
    {
        private const int RESERVE_MEGABYTES_COUNT = 10;

        public static CompositionRoot Instance;
        private CompositionRoot[] _objects;

        [SerializeField] private RootContainer[] _rootContainers;

        private DIContainer _container;
        private IDIService _diService;

        public void SubContainerInit(SubContainer subContainer)
        {
            subContainer.Init(_diService, _container);
        }

        private void Initialize()
        {
            const string ROOT_CONTAINER_NULL_STRING = "Root container should not be null, check CompositionRoot in the inspector";

            _diService = new DIService();
            _container = _diService.GenerateContainer();

            if (_rootContainers.Length == 0)
            {
                throw new InvalidOperationException(ROOT_CONTAINER_NULL_STRING);
            }
            
            for (var i = 0; i < _rootContainers.Length; i++)
            {
                var rootContainer = _rootContainers[i];

                if (rootContainer == null)
                {
                    throw new InvalidOperationException(ROOT_CONTAINER_NULL_STRING);
                }

                rootContainer.Init(_diService, _container);
            }

            for (var i = 0; i < _rootContainers.Length; i++)
            {
                _rootContainers[i].ResolveContainer();
            }

            SceneManager.sceneLoaded += OnSceneLoaded;
            SceneManager.sceneUnloaded += OnSceneUnloaded;
        }

        private void OnApplicationPause(bool pause)
        {
            _container?.CallApplicationPause(pause);
        }

        private void OnApplicationFocus(bool focus)
        {
            _container?.CallApplicationFocus(focus);
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            _container?.CallSceneLoaded(scene.buildIndex);
        }

        private void OnSceneUnloaded(Scene scene)
        {
            _container?.CallSceneUnloaded(scene.buildIndex);
        }

        private void Awake()
        {
            NativeHeapUtils.ReserveMegabytes(RESERVE_MEGABYTES_COUNT);

#pragma warning disable CS0618 // Type or member is obsolete
            _objects = FindObjectsOfType<CompositionRoot>();
#pragma warning restore CS0618 // Type or member is obsolete

            if (Instance == null)
            {
                Instance = this;
            }

            if (_objects.Length > 1)
            {
                Destroy(gameObject);
                return;
            }

            DontDestroyOnLoad(gameObject);
            Initialize();
        }

        private void OnDestroy()
        {
            if (_objects.Length != 1)
            {
                return;
            }

            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneUnloaded -= OnSceneUnloaded;

            _container.ReleaseAll();
        }
    }
}