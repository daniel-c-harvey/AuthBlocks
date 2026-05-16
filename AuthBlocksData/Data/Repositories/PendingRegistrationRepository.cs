using System.Linq.Expressions;
using AuthBlocksModels.Entities;
using AuthBlocksModels.Entities.Identity;
using Data.Shared.Data.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Models.Shared.Common;

namespace AuthBlocksData.Data.Repositories;

public class PendingRegistrationRepository : Repository<AuthDbContext, PendingRegistration>, IPendingRegistrationRepository
{
    private readonly DbSet<ApplicationRole> _roles;

    public PendingRegistrationRepository(AuthDbContext context,
        ILogger<Repository<AuthDbContext, PendingRegistration>> logger)
        : base(context, logger)
    {
        _roles = context.Set<ApplicationRole>();
    }
    
    protected override void UpdateEntity(PendingRegistration target, PendingRegistration source)
    {
        base.UpdateEntity(target, source);
        target.IsConsumed = source.IsConsumed;
        target.ConsumedAt = source.ConsumedAt;
        target.ExpiresAt = source.ExpiresAt;
        target.RoleIds = source.RoleIds;
    }

    public override async Task<PendingRegistration?> GetByIdAsync(long id)
    {
        var pendingRegistration = await base.GetByIdAsync(id);
        await PopulatePendingRegistrationRoles(pendingRegistration);
        return pendingRegistration;
    }

    public override async Task<IEnumerable<PendingRegistration>> GetAllAsync()
    {
        var pendingRegistrations = await base.GetAllAsync();
        await PopulatePendingRegistrationRoles(pendingRegistrations);
        return pendingRegistrations;
    }

    public override async Task<IEnumerable<PendingRegistration>> FindAsync(Expression<Func<PendingRegistration, bool>> predicate)
    {
        var pendingRegistrations = await base.FindAsync(predicate);
        await PopulatePendingRegistrationRoles(pendingRegistrations);
        return pendingRegistrations;
    }

    public override async Task<PagedResult<PendingRegistration>> GetPagedAsync(PagingParameters<PendingRegistration> pagingParameters)
    {
        var page = await base.GetPagedAsync(pagingParameters);
        await PopulatePendingRegistrationRoles(page.Items);
        return page;
    }

    public override async Task<PagedResult<PendingRegistration>> GetPagedAsync(Expression<Func<PendingRegistration, bool>> predicate, PagingParameters<PendingRegistration> pagingParameters)
    {
        var page = await base.GetPagedAsync(predicate, pagingParameters);
        await PopulatePendingRegistrationRoles(page.Items);
        return page;
    }

    public override async Task<PendingRegistration> AddAsync(PendingRegistration entity)
    {
        var pendingRegistration = await base.AddAsync(entity);
        await PopulatePendingRegistrationRoles(pendingRegistration);
        return pendingRegistration;
    }

    private async Task PopulatePendingRegistrationRoles(IEnumerable<PendingRegistration>? pendingRegistrations)
    {
        foreach (var pendingRegistration in pendingRegistrations ?? [])
        {
            await PopulatePendingRegistrationRoles(pendingRegistration);
        }
    }
    
    private async Task PopulatePendingRegistrationRoles(PendingRegistration? pendingRegistration)
    {
        if (pendingRegistration is { RoleIds: not null })
            pendingRegistration.Roles = await _roles
                .Where(r => pendingRegistration.RoleIds.Contains(r.Id))
                .ToListAsync();
    }
}