namespace Nk7.Container
{
    public interface IScopeService
    {
        int GetCurrentScope();
        int CreateScope();
        void SetCurrentScope(int scopeId);
        void ReleaseScope(int scopeId);
    }
}