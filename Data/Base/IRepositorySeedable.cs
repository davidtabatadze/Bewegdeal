namespace Bewegdeal.Data.Base
{
    public interface IRepositorySeedable : IRepository
    {
        Task Seed();
    }
}
