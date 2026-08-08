using Users.Application.DTOs;
using Users.Application.Exceptions;
using Users.Application.Interfaces.Identity;
using Users.Application.Interfaces.Repositories;
using Users.Application.Interfaces.Services;
using Users.Domain.Entities;
using Users.Domain.Enums;

namespace Users.Application.Services;

internal class UserService : IAuthService
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
    public async Task<string> RegisterAsync(UserRegisterDto userRequestDto, CancellationToken ct = default)
    {
        var existUser = await _repository.GetByUserNameAsync(userRequestDto.UserName, ct);
        if (existUser != null) throw new UserAlreadyExistsException(userRequestDto.UserName);

        var user = User.Create(
            userName: userRequestDto.UserName,
            passwordHash: _hasher.Hash(userRequestDto.Password),
            role: nameof(RoleName.User)
        );

        _repository.Add(user);
        await _repository.SaveChangesAsync(ct);

        var token = new TokenPayload(user.Id, user.Role);

        return _token.GenerateToken(token);
    }

    public async Task<string> LoginAsync(UserLoginDto userLoginDto, CancellationToken ct = default)
    {
        var user = await _repository.GetByUserNameAsync(userLoginDto.UserName, ct);
        if (user == null) throw new InvalidCredentialsException();

        if (!_hasher.Verify(password: userLoginDto.Password, hash: user.PasswordHash)) throw new InvalidCredentialsException();

        var token = new TokenPayload(user.Id, user.Role);
        return _token.GenerateToken(token);
    }
}
