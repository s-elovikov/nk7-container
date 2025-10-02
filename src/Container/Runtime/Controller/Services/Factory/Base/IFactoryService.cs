using System;

namespace Nk7.Container
{
    public interface IFactoryService<TService>
    {
        TService GetService(Type serviceType);
    }
}
