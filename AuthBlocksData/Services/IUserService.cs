using System.Linq.Expressions;
using AuthBlocksModels.Entities.Identity;
using AuthBlocksModels.Models;
using Data.Shared.Managers;
using Models.Shared.Common;
using NetBlocks.Models;

namespace AuthBlocksData.Services;

public interface IUserService : IManager<ApplicationUser, UserModel>
{
    Task<ResultContainer<PagedResult<UserModel>>> GetPage(long userId, Expression<Func<ApplicationUser, bool>> predicate, PagingParameters<ApplicationUser> pagingParameters);
    public Task<ResultContainer<UserModel>> Add(UserModel model, string password);
    
    // Standard Identity operations
    Task<UserModel?> FindByEmailAsync(string email);
    Task<UserModel?> FindByNameAsync(string userName);
    
    // Password operations
    Task<bool> CheckPassword(UserModel user, string password);
    Task UpdatePassword(UserModel user, string password);
    
} 