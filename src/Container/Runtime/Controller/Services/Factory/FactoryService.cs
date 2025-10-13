using System;

namespace Nk7.Container
{
    public sealed class FactoryService<TService> : IFactoryService<TService>
    {
        private readonly IDIContainer _container;

        public FactoryService(IDIContainer container)
        {
            _container = container;
        }

        public TService GetService(Type serviceType)
        {
            return (TService)_container.Resolve(serviceType);
        }
	}
}