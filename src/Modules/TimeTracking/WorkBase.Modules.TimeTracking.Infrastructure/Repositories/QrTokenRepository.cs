using Microsoft.EntityFrameworkCore;
using WorkBase.Infrastructure.Persistence;
using WorkBase.Modules.TimeTracking.Application.Contracts;
using WorkBase.Modules.TimeTracking.Domain.Entities;

namespace WorkBase.Modules.TimeTracking.Infrastructure.Repositories;

public sealed class QrTokenRepository(WorkBaseDbContext dbContext) : IQrTokenRepository
{
    public async Task<QrToken?> GetByTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        return await dbContext.Set<QrToken>()
            .FirstOrDefaultAsync(q => q.Token == token, cancellationToken);
    }

    public async Task AddAsync(QrToken qrToken, CancellationToken cancellationToken = default)
    {
        await dbContext.Set<QrToken>().AddAsync(qrToken, cancellationToken);
    }

    public void Update(QrToken qrToken)
    {
        dbContext.Set<QrToken>().Update(qrToken);
    }
}
