using System;

namespace Nk7.Container
{
    public abstract class AbstractFactoryService : IFactoryService
    {
        protected readonly IDIContainer _container;

        public AbstractFactoryService(IDIContainer container)
        {
            _container = container;
        }

        public abstract TService GetService<TService>(Type serviceType);
	}
}