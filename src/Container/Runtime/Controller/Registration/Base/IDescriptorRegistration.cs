using System.Collections.Generic;
using UnityEngine;
using System;

namespace Nk7.Container
{
    public interface IDescriptorRegistration
    {
        object Implementation { get; }

        Component Prefab { get; }
        Transform Parent { get; }

        Type ImplementationType { get; }
        List<Type> InterfacesTypes { get; }

        Func<object> GetImplementation { get; }
        
        ServiceLifeType LifeType { get; }
        RegistrationType RegistrationType { get; }

        DescriptorRegistration As<TInterface>();
        DescriptorRegistration As<TInterface1, TInterface2>();
        DescriptorRegistration As<TInterface1, TInterface2, TInterface3>();
        DescriptorRegistration As<TInterface1, TInterface2, TInterface3, TInterface4>();
        DescriptorRegistration AsImplementedInterfaces();
    }
}