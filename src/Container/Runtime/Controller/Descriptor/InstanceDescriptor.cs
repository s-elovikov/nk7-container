using System.Collections.Generic;
using System;

namespace Nk7.Container
{
    public sealed class InstanceDescriptor : Descriptor
    {
        internal InstanceDescriptor(Type serviceType, Type implementationType, object implementation,
            ServiceLifeType lifeType, List<Type> interfacesTypes, Func<object> getImplementation = null)
            : base(serviceType, implementationType, implementation, lifeType, interfacesTypes, getImplementation)
        {
        }
    }
}