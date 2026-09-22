using Microsoft.EntityFrameworkCore;
using Users.Application.Interfaces.Repositories;
using Users.Domain.Entities;
using Users.Infrastructure.Data;

namespace Users.Infrastructure.Repositories;

internal sealed class RefreshTokenRepository : IRefreshTokenRepository
{
    private readonly UsersDbContext _context;

    public RefreshTokenRepository(UsersDbContext context)
    {
        _context = context;
    }

    public async Task<RefreshToken?> GetByTokenHashAsync(string tokenHash, CancellationToken ct = default)
    {
        return await _context.RefreshTokens
                             .FirstOrDefaultAsync(x => x.TokenHash == tokenHash, ct);
    }

    public void Add(RefreshToken token)
    {
        _context.RefreshTokens.Add(token);
    }

    public async Task RevokeAllActiveByUserIdAsync(Guid userId, CancellationToken ct = default)
    {
        var activeTokens = await _context.RefreshTokens
                                         .Where(x => x.UserId == userId && x.RevokedAt == null)
                                         .ToListAsync(ct);

        foreach (var token in activeTokens)
        {
            token.Revoke();
        }
    }

    public async Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        return await _context.SaveChangesAsync(ct);
    }
}
