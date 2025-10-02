using UnityEngine;

namespace Nk7.Container
{
    [DefaultExecutionOrder(-6500)]
    public abstract class RootContainer : Container
    {
        public void Init(IBaseDIService builder, IDIContainer container)
        {
            DIContainer = container;
            
            AutoRegisterAll(builder);
            Register(builder);
        }

        public void ResolveContainer()
        {
            DIContainer.ResolveRegisteredInstances();

            Resolve();
            AutoResolveAll();
        }
    }
}