namespace Bewegdeal.Data.Base
{
    /// <summary>
    /// Represents a seedable repository — implementation should populate
    /// required initial data via <see cref="Seed"/>.
    /// </summary>
    public interface IRepositorySeedable : IRepository
    {
        Task Seed();
    }
}
