using System;

namespace Nk7.Container
{
    public interface IBaseDIService
    {
        T Register<T>(T descriptorRegistration) where T : IDescriptorRegistration;

        IDescriptorRegistration RegisterSingleton<TService, TImplementation>()
            where TImplementation : class, TService;
        IDescriptorRegistration RegisterTransient<TService, TImplementation>()
            where TImplementation : class, TService;
        IDescriptorRegistration RegisterScoped<TService, TImplementation>()
            where TImplementation : class, TService;

        IDescriptorRegistration RegisterSingleton<TService>();
        IDescriptorRegistration RegisterTransient<TService>();
        IDescriptorRegistration RegisterScoped<TService>();

        IDescriptorRegistration RegisterSingleton(Type serviceType);
        IDescriptorRegistration RegisterTransient(Type serviceType);
        IDescriptorRegistration RegisterScoped(Type serviceType);
    }
}