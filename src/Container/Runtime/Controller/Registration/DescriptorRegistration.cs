using System.Runtime.CompilerServices;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace Nk7.Container
{
    public sealed class DescriptorRegistration : IDescriptorRegistration
    {
        public object Implementation { get; internal set; }

        public Component Prefab { get; internal set; }
        public Transform Parent { get; internal set; }

        public Type ImplementationType { get; internal set; }
        public List<Type> InterfacesTypes { get; private set; }

        public Func<object> GetImplementation { get; internal set; }
        
        public ServiceLifeType LifeType { get; internal set; }
        public RegistrationType RegistrationType { get; internal set; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public DescriptorRegistration As(Type interfaceType)
        {
            return AddInterfaces(interfaceType);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public DescriptorRegistration As<TInterface>()
        {
            return AddInterfaces(typeof(TInterface));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public DescriptorRegistration As<TInterface1, TInterface2>()
        {
            return AddInterfaces(typeof(TInterface1), typeof(TInterface2));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public DescriptorRegistration As<TInterface1, TInterface2, TInterface3>()
        {
            return AddInterfaces(typeof(TInterface1), typeof(TInterface2), typeof(TInterface3));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public DescriptorRegistration As<TInterface1, TInterface2, TInterface3, TInterface4>()
        {
            return AddInterfaces(typeof(TInterface1), typeof(TInterface2), typeof(TInterface3), typeof(TInterface4));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public DescriptorRegistration AsImplementedInterfaces()
        {
            var interfaces = ImplementationType.GetInterfaces();

            return AddInterfaces(interfaces);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private DescriptorRegistration AddInterfaces(params Type[] interfaceTypes)
        {
            if (InterfacesTypes == null)
            {
                InterfacesTypes = new List<Type>();
            }

            for (int i = 0; i < interfaceTypes.Length; ++i)
            {
                var interfaceType = interfaceTypes[i];

                if (HasDuplicateInterface(interfaceType))
                {
                    continue;
                }

                if (!interfaceType.IsAssignableFrom(ImplementationType))
                {
                    throw new InvalidOperationException($"{ImplementationType} is not assignable from {interfaceType}");
                }

                InterfacesTypes.Add(interfaceType);
            }

            return this;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool HasDuplicateInterface(Type checkedInterfaceType)
        {
            return InterfacesTypes.Contains(checkedInterfaceType);
        }
    }
}