namespace Nk7.Container
{
    public sealed class ScopeService : IScopeService
    {
        private readonly IDIContainer _container;

        public ScopeService(IDIContainer container)
        {
            _container = container;
        }

        public int GetCurrentScope()
        {
            return _container.GetCurrentScope();
        }

        public int CreateScope()
        {
            return _container.CreateScope();
        }

        public void SetCurrentScope(int scopeId)
        {
            _container.SetCurrentScope(scopeId);
        }

        public void ReleaseScope(int scopeId)
        {
            _container.ReleaseScope(scopeId);
        }
    }
}