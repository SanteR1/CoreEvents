using CoreEvents.Application.DTOs;
using CoreEvents.Application.Interfaces.Identity;
using CoreEvents.Application.Interfaces.Repositories;
using CoreEvents.Domain.Entities;
using CoreEvents.Domain.Exceptions;

namespace CoreEvents.Application.Services;

internal class UserService : IUserService
{
    private readonly IUserRepository _repository;
    private readonly ITokenProvider _token;
    private readonly IPasswordHasher _hasher;

    public UserService(IUserRepository repository, ITokenProvider token, IPasswordHasher hasher)
    {
        _repository = repository;
        _token = token;
        _hasher = hasher;
    }
    public async Task<string> RegisterAsync(UserRequestDto userRequestDto, CancellationToken ct = default)
    {
        var existUser = await _repository.GetByUserNameAsync(userRequestDto.UserName, ct);
        if (existUser != null) throw new DomainUserAlreadyExistsException(userRequestDto.UserName);

        var user = User.Create(
            userName: userRequestDto.UserName,
            passwordHash: _hasher.Hash(userRequestDto.Password),
            role: userRequestDto.Role
        );

        _repository.Add(user);
        await _repository.SaveChangesAsync(ct);

        var token = new TokenPayload(user.Id, user.Role);

        return _token.GenerateToken(token);
    }

    public async Task<string> LoginAsync(UserLoginDto userLoginDto, CancellationToken ct = default)
    {
        var user = await _repository.GetByUserNameAsync(userLoginDto.UserName, ct);
        if (user == null) throw new DomainAuthorizationException();

        if (!_hasher.Verify(password: userLoginDto.Password, hash: user.PasswordHash)) throw new DomainAuthorizationException();

        var token = new TokenPayload(user.Id, user.Role);
        return _token.GenerateToken(token);
    }
}
