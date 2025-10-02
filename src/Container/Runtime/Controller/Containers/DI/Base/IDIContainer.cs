using System;

namespace Nk7.Container
{
    public interface IDIContainer
    {
        int GetCurrentScope();
        int CreateScope();
        void SetCurrentScope(int scopeId);
        void ReleaseScope(int scopeId);

        void ResolveRegisteredInstances();
        T Resolve<T>();
        object Resolve(Type type);
        void ResolveImplementation(object implementation);

        void ReleaseAll();
        void Release<T>();
        void Release(Type realeaseType);
    }
}