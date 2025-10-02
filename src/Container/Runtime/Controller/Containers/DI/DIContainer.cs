using System.Runtime.CompilerServices;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Reflection;
using UnityEngine;
using System;

namespace Nk7.Container
{
    public sealed partial class DIContainer : IDIContainer
    {
        private readonly ConcurrentDictionary<int, Dictionary<Type, Descriptor>> _scopes;

        private readonly ConcurrentDictionary<Type, FieldInfo[]> _fieldsCache;
        private readonly ConcurrentDictionary<Type, MethodInfo[]> _methodsCache;
        private readonly ConcurrentDictionary<Type, PropertyInfo[]> _propertiesCache;
        private readonly ConcurrentDictionary<Type, ConstructorInfo[]> _constructorsCache;

        private readonly HashSet<int> _objectGraphHashCodesCache;
        private ConstructorInfo _cachedConstructorInfo;

        private readonly List<IDescriptorRegistration> _descriptorRegistrations;

        private readonly object _scopeLock;
        private readonly Type _ignoredType;

        private int _currentScopeId;

        private ConcurrentDictionary<int, Dictionary<Type, Descriptor>> Scopes
        {
            get
            {
                Register();
                return _scopes;
            }
        }

        public DIContainer(List<IDescriptorRegistration> descriptorRegistrations)
        {
            _scopes = new ConcurrentDictionary<int, Dictionary<Type, Descriptor>>();

            _fieldsCache = new ConcurrentDictionary<Type, FieldInfo[]>(4, 128);
            _methodsCache = new ConcurrentDictionary<Type, MethodInfo[]>(4, 128);
            _propertiesCache = new ConcurrentDictionary<Type, PropertyInfo[]>(4, 128);
            _constructorsCache = new ConcurrentDictionary<Type, ConstructorInfo[]>(4, 128);

            _objectGraphHashCodesCache = new HashSet<int>(16);

            _descriptorRegistrations = descriptorRegistrations;
            _ignoredType = typeof(IDisposable);
            _scopeLock = new object();
            _currentScopeId = -1;

            CreateScope();
        }

        public int GetCurrentScope()
        {
            return _currentScopeId;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int CreateScope()
        {
            var newScope = new Dictionary<Type, Descriptor>(32);

            ++_currentScopeId;
            _scopes[_currentScopeId] = newScope;

            LogsUtils.Log($"New scope has been created with ID: {_currentScopeId}");

            return _currentScopeId;
        }

        public void SetCurrentScope(int scopeId)
        {
            if (Scopes.ContainsKey(scopeId))
            {
                _currentScopeId = scopeId;
            }
            else
            {
                throw new InvalidOperationException($"Scope with ID {scopeId} doesn't exist");
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ReleaseScope(int scopeId)
        {
            if (scopeId <= 0)
            {
                LogsUtils.LogWarning("You can't release the main scope");
                return;
            }

            if (_scopes.TryGetValue(scopeId, out var scope))
            {
                foreach (var scopedDependencyObject in scope.Values)
                {
                    scopedDependencyObject.Dispose();
                }

                scope.Clear();

                LogsUtils.Log($"Scope has been released with ID {scopeId}");
            }

            if (scopeId == _currentScopeId)
            {
                _currentScopeId = 0;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ResolveRegisteredInstances()
        {
            var mainScope = Scopes[0];

#if UNITY_WEBGL && !UNITY_EDITOR
            foreach (var descriptor in mainScope.Values)
            {
                if (descriptor is not InstanceDescriptor)
                {
                    continue;
                }

                ResolveObject(descriptor.Implementation);
            }

            return;
#else
            var instances = new List<object>();

            foreach (var descriptor in mainScope.Values)
            {
                if (descriptor is not InstanceDescriptor)
                {
                    continue;
                }

                instances.Add(descriptor.Implementation);
            }

            if (instances.Count <= 0)
            {
                return;
            }

            Parallel.ForEach(instances, ResolveImplementation);
#endif
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Resolve<T>()
        {
            return (T)Resolve(typeof(T));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public object Resolve(Type type)
        {
            if (TryResolveComponent(type, out var componentImplementation))
            {
                return componentImplementation;
            }

            var resolvedType = ResolveType(type);
            var implementation = resolvedType.Implementation;

            if (resolvedType.LifeType == ServiceLifeType.Transient)
            {
                resolvedType.Implementation = null;
            }

            return implementation;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ResolveImplementation(object implementation)
        {
            ResolveFields(implementation);
            ResolveMethods(implementation);
            ResolveProperties(implementation);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ReleaseAll()
        {
            foreach (var scopes in Scopes)
            {
                foreach (var scope in scopes.Value.Values)
                {
                    if (scope.Implementation == null || scope.Implementation is UnityEngine.Object)
                    {
                        continue;
                    }

                    scope.Dispose();
                }
            }

            Scopes.Clear();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Release<T>()
        {
            Release(typeof(T));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Release(Type realeaseType)
        {
            if (Scopes.Count <= 0)
            {
                return;
            }

            var mainScope = Scopes[0];

            if (!mainScope.TryGetValue(realeaseType, out var descriptor))
            {
                return;
            }

            if (descriptor.Implementation == null)
            {
                return;
            }

            descriptor.Dispose();

            lock (_scopeLock)
            {
                mainScope.Remove(realeaseType);
            }
        }

        private void Register()
        {
            if (_descriptorRegistrations.Count <= 0)
            {
                return;
            }

            if (!_scopes.TryGetValue(0, out var mainScope))
            {
                int estimatedCapacity = _descriptorRegistrations.Count * 3;

                mainScope = new Dictionary<Type, Descriptor>(estimatedCapacity);
                _scopes[0] = mainScope;
            }

            int totalInterfacesCount = default;

            for (int i = 0; i < _descriptorRegistrations.Count; ++i)
            {
                var registration = _descriptorRegistrations[i];

                totalInterfacesCount += registration.InterfacesTypes.Count;
            }

            int targetCapacity = Math.Max(totalInterfacesCount, mainScope.Count + _descriptorRegistrations.Count * 2);

            if (mainScope.Count < targetCapacity)
            {
                mainScope.EnsureCapacity(targetCapacity);
            }

            for (int i = 0; i < _descriptorRegistrations.Count; ++i)
            {
                var registration = _descriptorRegistrations[i];

                for (int j = 0; j < registration.InterfacesTypes.Count; ++j)
                {
                    var serviceType = registration.InterfacesTypes[j];

                    if (CheckIsIgnoreType(serviceType))
                    {
                        continue;
                    }

                    var descriptor = GetDescriptor(i, serviceType);

                    lock (_scopeLock)
                    {
                        if (!mainScope.TryAdd(descriptor.ServiceType, descriptor))
                        {
                            throw new InvalidOperationException($"Service type already registered - {descriptor.ServiceType}");
                        }
                    }
                }
            }

            _descriptorRegistrations.Clear();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool CheckIsIgnoreType(Type checkedType)
        {
            return _ignoredType == checkedType;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Descriptor GetDescriptor(int index, Type serviceType)
        {
            var registration = _descriptorRegistrations[index];
            Descriptor result = null;

            switch (registration.RegistrationType)
            {
                case RegistrationType.Default:
                    result = new Descriptor(
                        serviceType,
                        registration.ImplementationType,
                        registration.Implementation,
                        registration.LifeType,
                        registration.InterfacesTypes,
                        registration.GetImplementation);

                    break;

                case RegistrationType.Component:
                    result = new ComponentDescriptor(
                        serviceType,
                        registration.ImplementationType,
                        registration.Implementation,
                        registration.LifeType,
                        registration.InterfacesTypes,
                        registration.Prefab,
                        registration.Parent);

                    break;

                case RegistrationType.Instance:
                    result = new InstanceDescriptor(
                        serviceType,
                        registration.ImplementationType,
                        registration.Implementation,
                        registration.LifeType,
                        registration.InterfacesTypes);

                    break;

                default:
                    throw new NotImplementedException($"Unknown register type: {registration.RegistrationType}");
            }

            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool TryResolveComponent(Type serviceType, out Component implementation)
        {
            implementation = null;

            var mainScope = Scopes[0];

            if (!mainScope.TryGetValue(serviceType, out var descriptor))
            {
                return false;
            }

            if (descriptor is not ComponentDescriptor componentDescriptor)
            {
                return false;
            }

            if (componentDescriptor.Prefab == null)
            {
                throw new InvalidOperationException($"Service prefab isn't exist - {componentDescriptor.ServiceType}");
            }

            bool prefabWasActive = componentDescriptor.Prefab.gameObject.activeSelf;

            if (prefabWasActive)
            {
                componentDescriptor.Prefab.gameObject.SetActive(false);
            }

            implementation = componentDescriptor.Parent != null
                ? UnityEngine.Object.Instantiate(componentDescriptor.Prefab, componentDescriptor.Parent)
                : UnityEngine.Object.Instantiate(componentDescriptor.Prefab);

            ResolveImplementation(implementation);

            if (prefabWasActive)
            {
                implementation.gameObject.SetActive(true);
                componentDescriptor.Prefab.gameObject.SetActive(true);
            }

            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ResolveFields(object implementation)
        {
            var implementationType = implementation.GetType();
            var fields = _fieldsCache.GetOrAdd(implementationType, t =>
                t.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance));

            for (int i = 0; i < fields.Length; ++i)
            {
                var field = fields[i];
                var resolveAttribute = field.GetCustomAttribute<ResolveAttribute>();

                if (resolveAttribute == null)
                {
                    continue;
                }

                var resolvedType = ResolveType(field.FieldType).Implementation;
                var setter = Activator.GetFieldSetter(field);

                setter(implementation, resolvedType);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ResolveMethods(object implementation)
        {
            var implementationType = implementation.GetType();
            var methods = _methodsCache.GetOrAdd(implementationType, t =>
                t.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance));

            for (int i = 0; i < methods.Length; ++i)
            {
                var method = methods[i];
                var resolveAttribute = method.GetCustomAttribute<ResolveAttribute>();

                if (resolveAttribute == null)
                {
                    continue;
                }

                var parameters = method.GetParameters();
                var resolvedParameters = ResolveParameters(parameters);
                var invoker = Activator.GetMethodInvoker(method);

                invoker(implementation, resolvedParameters);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ResolveProperties(object implementation)
        {
            var implementationType = implementation.GetType();
            var properties = _propertiesCache.GetOrAdd(implementationType, t =>
                t.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance));

            for (int i = 0; i < properties.Length; ++i)
            {
                var property = properties[i];
                var resolveAttribute = property.GetCustomAttribute<ResolveAttribute>();

                if (resolveAttribute == null)
                {
                    continue;
                }

                var resolvedType = ResolveType(property.PropertyType).Implementation;
                var setter = Activator.GetPropertySetter(property);

                setter(implementation, resolvedType);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Descriptor ResolveType(Type serviceType)
        {
            var descriptor = GetDescriptorForType(serviceType);

            if (TryGetCachedImpl(descriptor, out var descriptorImpl))
            {
                return descriptorImpl;
            }

            if (descriptorImpl != null && descriptorImpl.LifeType == ServiceLifeType.Scoped)
            {
                descriptor = descriptorImpl;
            }

            if (descriptor.TryGetImplementation(out var implementation))
            {
                descriptor.Implementation = implementation;
            }
            else
            {
                var implementationType = descriptor.ImplementationType;

                if (implementationType.IsAbstract || implementationType.IsInterface)
                {
                    throw new InvalidOperationException($"Cannot instantiate abstract classes or interfaces {implementationType}");
                }

                _cachedConstructorInfo = GetConstructorInfo(implementationType);

                if (_cachedConstructorInfo != null)
                {
                    int hashCode = _cachedConstructorInfo.GetHashCode();

                    if (_objectGraphHashCodesCache.Contains(hashCode))
                    {
                        throw new InvalidOperationException($"{_cachedConstructorInfo.DeclaringType} has circular dependency");
                    }

                    _objectGraphHashCodesCache.Add(hashCode);

                    var objectActivator = Activator.GetActivator(_cachedConstructorInfo);
                    var parameters = _cachedConstructorInfo.GetParameters();
                    var resolvedParameters = ResolveParameters(parameters);

                    descriptor.Implementation = objectActivator.Invoke(resolvedParameters);
                }
                else
                {
                    descriptor.Implementation = Activator.GetDefaultConstructor(implementationType).Invoke();
                }

                ResolveImplementation(descriptor.Implementation);
                SetDisposable(descriptor);
                SetImplementationTypes(descriptor);
                InitializeImplementation(descriptor);
            }

            _cachedConstructorInfo = null;
            _objectGraphHashCodesCache.Clear();

            return descriptor;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private object[] ResolveParameters(ParameterInfo[] parameters)
        {
            var instances = new object[parameters.Length];

            for (int i = 0; i < parameters.Length; ++i)
            {
                var parameter = parameters[i];

                instances[i] = ResolveType(parameter.ParameterType).Implementation;
            }

            return instances;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SetDisposable(Descriptor descriptor)
        {
            if (descriptor.LifeType == ServiceLifeType.Transient)
            {
                return;
            }

            descriptor.SetDisposable();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SetImplementationTypes(Descriptor descriptor)
        {
            if (descriptor.LifeType == ServiceLifeType.Transient)
            {
                return;
            }

            var interfacesTypes = descriptor.InterfacesTypes;
            int interfacesTypesCount = interfacesTypes.Count;

            for (int i = 0; i < interfacesTypesCount; ++i)
            {
                var interfaceType = interfacesTypes[i];

                switch (descriptor.LifeType)
                {
                    case ServiceLifeType.Singleton:
                        var mainScope = Scopes[0];

                        if (mainScope.TryGetValue(interfaceType, out var singletonDescriptor))
                        {
                            singletonDescriptor.Implementation ??= descriptor.Implementation;
                        }

                        break;

                    case ServiceLifeType.Scoped:
                        var currentScope = _scopes[_currentScopeId];

                        lock (_scopeLock)
                        {
                            if (currentScope.TryGetValue(interfaceType, out var scopedDescriptor))
                            {
                                continue;
                            }

                            descriptor.ServiceType = interfaceType;

                            scopedDescriptor = new Descriptor(descriptor);
                            currentScope[interfaceType] = scopedDescriptor;
                        }
                        
                        break;
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void InitializeImplementation(Descriptor descriptor)
        {
            if (descriptor.Implementation is not IContainerInitializable initializable)
            {
                return;
            }

            initializable.Initialize();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Descriptor GetDescriptorForType(Type serviceType)
        {
            if (_scopes.TryGetValue(0, out var mainScope)
                && mainScope.TryGetValue(serviceType, out var descriptor))
            {
                return descriptor;
            }

            throw new InvalidOperationException(_cachedConstructorInfo == null
                ? $"There is no such a service {serviceType} registered"
                : $"{_cachedConstructorInfo.DeclaringType} tried to find {serviceType} but dependency is not found.");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ConstructorInfo GetConstructorInfo(Type implementationType)
        {
            var constructors = _constructorsCache.GetOrAdd(implementationType, t =>
                t.GetConstructors());

            if (constructors.Length <= 0)
            {
                LogsUtils.LogWarning($"{implementationType} hasn't public constructors");
                return null;
            }

            var bestConstructor = constructors[0];

            for (int i = 0; i < constructors.Length; ++i)
            {
                var constructor = constructors[i];
                var constructorParameters = constructor.GetParameters();

                if (constructorParameters.Length > 0)
                {
                    bestConstructor = constructor;
                    break;
                }
            }

            return bestConstructor;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetCachedImpl(Descriptor descriptor, out Descriptor descriptorImpl)
        {
            switch (descriptor.LifeType)
            {
                case ServiceLifeType.Scoped:
                    if (_scopes.TryGetValue(_currentScopeId, out var scope))
                    {
                        lock (_scopeLock)
                        {
                            if (scope.TryGetValue(descriptor.ServiceType, out var scopeDescriptor))
                            {
                                if (scopeDescriptor.Implementation != null)
                                {
                                    descriptorImpl = scopeDescriptor;
                                    return true;
                                }
                            }

                            scopeDescriptor = new Descriptor(descriptor);
                            scope[scopeDescriptor.ServiceType] = scopeDescriptor;
                            descriptorImpl = scopeDescriptor;
                        }

                        return false;
                    }

                    scope = new Dictionary<Type, Descriptor>(32);
                    _scopes[_currentScopeId] = scope;
                    
                    var newScopedDescriptor = new Descriptor(descriptor);

                    scope[newScopedDescriptor.ServiceType] = newScopedDescriptor;
                    descriptorImpl = newScopedDescriptor;

                    return false;

                case ServiceLifeType.Singleton:
                    if (descriptor.Implementation != null)
                    {
                        descriptorImpl = descriptor;
                        return true;
                    }

                    break;
            }

            descriptorImpl = null;
            return false;
        }
    }
}