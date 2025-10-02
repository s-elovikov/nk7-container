using System.Runtime.CompilerServices;
using System.Collections.Generic;
using System;

namespace Nk7.Container
{
    public class Descriptor : IDisposable
    {
        internal object Implementation { get; set; }
        internal Type ServiceType { get; set; }

        internal readonly Type ImplementationType;
        internal readonly List<Type> InterfacesTypes;

        internal readonly ServiceLifeType LifeType;

        private readonly Func<object> _getImplementation;
        private IDisposable _implementationDisposable;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal bool TryGetImplementation(out object implementation)
        {
            if (_getImplementation != null)
            {
                implementation = _getImplementation();
                return true;
            }

            implementation = null;
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal Descriptor(Type serviceType, Type implementationType, object implementation,
            ServiceLifeType lifeType, List<Type> interfacesTypes, Func<object> getImplementation = null)
        {
            ServiceType = serviceType;
            LifeType = lifeType;
            ImplementationType = implementationType;
            Implementation = implementation;
            InterfacesTypes = interfacesTypes;

            _getImplementation = getImplementation;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal Descriptor(Descriptor descriptor)
        {
            ServiceType = descriptor.ServiceType;
            LifeType = descriptor.LifeType;
            ImplementationType = descriptor.ImplementationType;
            Implementation = descriptor.Implementation;
            InterfacesTypes = descriptor.InterfacesTypes;

            _getImplementation = descriptor._getImplementation;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Dispose()
        {
            if (_implementationDisposable != null)
            {
                _implementationDisposable.Dispose();
            }

            _implementationDisposable = null;
            Implementation = null;

            if (InterfacesTypes != null && InterfacesTypes.Count > 0)
            {
                InterfacesTypes.Clear();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetDisposable()
        {
            if (Implementation is not IDisposable disposable)
            {
                return;
            }

            _implementationDisposable = disposable;
        }
    }
}
