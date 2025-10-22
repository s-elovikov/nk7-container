using System.Runtime.CompilerServices;
using System.Collections.Generic;
using System;

namespace Nk7.Container
{
    public sealed class DIService : IDIService
    {
        private readonly List<IDescriptorRegistration> _descriptorRegistrations;

        public DIService()
        {
            _descriptorRegistrations = new List<IDescriptorRegistration>(128);
        }

        public DIContainer GenerateContainer()
        {
            var container = new DIContainer(_descriptorRegistrations);

            this.RegisterInstance<IDIContainer>(container);
            this.RegisterInstance<IContainerLifeCycle>(container);
            this.RegisterInstance(new ScopeService(container))
                .AsImplementedInterfaces();

            return container;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Register<T>(T descriptorRegistration) where T : IDescriptorRegistration
        {
            _descriptorRegistrations.Add(descriptorRegistration);

            return descriptorRegistration;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IDescriptorRegistration RegisterSingleton<TService, TImplementation>()
            where TImplementation : class, TService
        {
            return Register<TService, TImplementation>(ServiceLifeType.Singleton);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IDescriptorRegistration RegisterTransient<TService, TImplementation>()
            where TImplementation : class, TService
        {
            return Register<TService, TImplementation>(ServiceLifeType.Transient);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IDescriptorRegistration RegisterScoped<TService, TImplementation>()
            where TImplementation : class, TService
        {
            return Register<TService, TImplementation>(ServiceLifeType.Scoped);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IDescriptorRegistration RegisterSingleton<TService>()
        {
            return Register<TService>(ServiceLifeType.Singleton);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IDescriptorRegistration RegisterTransient<TService>()
        {
            return Register<TService>(ServiceLifeType.Transient);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IDescriptorRegistration RegisterScoped<TService>()
        {
            return Register<TService>(ServiceLifeType.Scoped);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IDescriptorRegistration RegisterSingleton(Type serviceType)
        {
            return Register(serviceType, ServiceLifeType.Singleton);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IDescriptorRegistration RegisterTransient(Type serviceType)
        {
            return Register(serviceType, ServiceLifeType.Transient);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IDescriptorRegistration RegisterScoped(Type serviceType)
        {
            return Register(serviceType, ServiceLifeType.Scoped);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private IDescriptorRegistration Register<TService, TImplementation>(ServiceLifeType serviceLifeType)
            where TImplementation : class, TService
        {
            var registration = new DescriptorRegistration();

            registration.ImplementationType = typeof(TImplementation);
            registration.RegistrationType = RegistrationType.Default;
            registration.LifeType = serviceLifeType;

            registration.As<TService>();

            return Register(registration);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private IDescriptorRegistration Register<TService>(ServiceLifeType serviceLifeType)
        {
            var registration = new DescriptorRegistration();

            registration.RegistrationType = RegistrationType.Default;
            registration.ImplementationType = typeof(TService);
            registration.LifeType = serviceLifeType;

            registration.As<TService>();

            return Register(registration);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private IDescriptorRegistration Register(Type serviceType, ServiceLifeType serviceLifeType)
        {
            var registration = new DescriptorRegistration();

            registration.RegistrationType = RegistrationType.Default;
            registration.ImplementationType = serviceType;
            registration.LifeType = serviceLifeType;

            registration.As(serviceType);

            return Register(registration);
        }
    }
}