namespace Nk7.Container
{
    public interface IDIService : IBaseDIService
    {
        DIContainer GenerateContainer();
    }
}