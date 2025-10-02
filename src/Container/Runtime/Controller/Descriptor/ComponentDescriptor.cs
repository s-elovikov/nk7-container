using System.Collections.Generic;
using UnityEngine;
using System;

namespace Nk7.Container
{
    public sealed class ComponentDescriptor : Descriptor
    {
        public readonly Component Prefab;
        public readonly Transform Parent;

        public ComponentDescriptor(Type serviceType, Type implementationType, object implementation,
            ServiceLifeType lifeType, List<Type> interfacesTypes, Component prefab,
            Transform parent, Func<object> getImplementation = null)
            : base(serviceType, implementationType, implementation, lifeType, interfacesTypes, getImplementation)
        {
            Prefab = prefab;
            Parent = parent;
        }
    }
}
