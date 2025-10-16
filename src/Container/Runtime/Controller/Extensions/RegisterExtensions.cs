using System.Runtime.CompilerServices;
using UnityEngine;
using System;

namespace Nk7.Container
{
    public static class RegisterExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void RegisterAbstractFactory<TService, TImplementation>(this IBaseDIService diService)
            where TImplementation : IFactoryService, TService
        {
            var registration = new DescriptorRegistration();

            registration.RegistrationType = RegistrationType.Default;
            registration.ImplementationType = typeof(TImplementation);
            registration.LifeType = ServiceLifeType.Singleton;

            registration.As<TService>();

            diService.Register(registration);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void RegisterFactory<TService>(
            this IBaseDIService diService, IDIContainer container)
        {
            var factory = new FactoryService<TService>(container);
            var registration = new DescriptorRegistration();

            registration.RegistrationType = RegistrationType.Instance;
            registration.ImplementationType = factory.GetType();
            registration.LifeType = ServiceLifeType.Singleton;
            registration.Implementation = factory;

            registration.As<IFactoryService<TService>>();

            diService.Register(registration);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IDescriptorRegistration RegisterComponent<TService>(
            this IBaseDIService diService, TService prefab, Transform parent = null)
            where TService : Component
        {
            var registration = new DescriptorRegistration();
            var prefabType = prefab.GetType();

            registration.RegistrationType = RegistrationType.Component;
            registration.LifeType = ServiceLifeType.Transient;
            registration.ImplementationType = prefabType;
            registration.Parent = parent;
            registration.Prefab = prefab;

            registration.As(prefabType);

            return diService.Register(registration);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IDescriptorRegistration RegisterInstance<TService>(
            this IBaseDIService diService, TService implementation)
            where TService : class
        {
            var registration = new DescriptorRegistration();

            registration.RegistrationType = RegistrationType.Instance;
            registration.ImplementationType = typeof(TService);
            registration.LifeType = ServiceLifeType.Singleton;
            registration.Implementation = implementation;

            registration.As<TService>();

            return diService.Register(registration);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IDescriptorRegistration RegisterInstanceAsSelf(
            this IBaseDIService diService, object implementation)
        {
            var registration = new DescriptorRegistration();
            var implementationType = implementation.GetType();

            registration.RegistrationType = RegistrationType.Instance;
            registration.ImplementationType = implementationType;
            registration.LifeType = ServiceLifeType.Singleton;
            registration.Implementation = implementation;

            registration.As(implementationType);

            return diService.Register(registration);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IDescriptorRegistration RegisterSingletonByFunc<TService>(
            this IBaseDIService diService, Func<TService> implementationConfiguration)
        {
            return RegisterByFunc(diService, implementationConfiguration, ServiceLifeType.Singleton);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IDescriptorRegistration RegisterTransientByFunc<TService>(
            this IBaseDIService diService, Func<TService> implementationConfiguration)
        {
            return RegisterByFunc(diService, implementationConfiguration, ServiceLifeType.Transient);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IDescriptorRegistration RegisterScopedByFunc<TService>(
            this IBaseDIService diService, Func<TService> implementationConfiguration)
        {
            return RegisterByFunc(diService, implementationConfiguration, ServiceLifeType.Scoped);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static IDescriptorRegistration RegisterByFunc<TService>(
            IBaseDIService diService, Func<TService> implementationConfiguration, ServiceLifeType serviceLifeType)
        {
            var registration = new DescriptorRegistration();
            
            registration.GetImplementation = () => implementationConfiguration.Invoke();
            registration.RegistrationType = RegistrationType.Default;
            registration.ImplementationType = typeof(TService);
            registration.LifeType = serviceLifeType;

            registration.As<TService>();

            return diService.Register(registration);
        }
    }
}