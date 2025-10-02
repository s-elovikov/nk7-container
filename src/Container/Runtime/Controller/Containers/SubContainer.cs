using UnityEngine;

namespace Nk7.Container
{
    [DefaultExecutionOrder(-6000)]
    public class SubContainer : Container
    {
        public void Init(IBaseDIService builder, IDIContainer container)
        {
            DIContainer = container;

            AutoRegisterAll(builder);
            Register(builder);

            container.ResolveRegisteredInstances();

            Resolve();
            AutoResolveAll();
        }

        protected virtual void Awake()
        {
            CompositionRoot.Instance.SubContainerInit(this);
        }
    }
}