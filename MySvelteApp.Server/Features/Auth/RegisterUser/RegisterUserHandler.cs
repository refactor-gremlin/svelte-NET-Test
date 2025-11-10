using MySvelteApp.Server.Features.Auth.RegisterUser;
using MySvelteApp.Server.Shared.Common.Interfaces;
using MySvelteApp.Server.Shared.Common.Results;
using MySvelteApp.Server.Shared.Common.DTOs;
using MySvelteApp.Server.Shared.Domain.Events;
using MySvelteApp.Server.Shared.Domain.Services;
using MySvelteApp.Server.Shared.Domain.ValueObjects;

namespace MySvelteApp.Server.Features.Auth.RegisterUser;

public class RegisterUserHandler
{
    private readonly IUserDomainService _userDomainService;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDomainEventPublisher _eventPublisher;

    public RegisterUserHandler(
        IUserDomainService userDomainService,
        IPasswordHasher passwordHasher,
        IJwtTokenGenerator jwtTokenGenerator,
        IUserRepository userRepository,
        IUnitOfWork unitOfWork,
        IDomainEventPublisher eventPublisher)
    {
        _userDomainService = userDomainService;
        _passwordHasher = passwordHasher;
        _jwtTokenGenerator = jwtTokenGenerator;
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _eventPublisher = eventPublisher;
    }

    public async Task<ApiResult<RegisterUserResponse>> HandleAsync(
        RegisterUserRequest request,
        CancellationToken cancellationToken = default)
    {
        // Create value objects from request (validation happens in validator)
        Username username;
        Email email;

        try
        {
            username = Username.Create(request.Username);
            email = Email.Create(request.Email);
        }
        catch (ArgumentException ex)
        {
            return ApiResult<RegisterUserResponse>.ValidationError(ex.Message);
        }

        // Check if user can be registered
        var (canRegister, errorMessage) = await _userDomainService.CanRegisterUserAsync(
            username, email, cancellationToken);

        if (!canRegister)
        {
            return ApiResult<RegisterUserResponse>.Conflict(errorMessage!);
        }

        // Hash password
        var (hash, salt) = _passwordHasher.HashPassword(request.Password);

        // Create user using domain service
        var user = _userDomainService.CreateUser(username, email, hash, salt);

        // Persist user
        await _userRepository.AddAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Generate token
        var token = _jwtTokenGenerator.GenerateToken(user);

        // Publish domain event
        await _eventPublisher.PublishAsync(
            new UserRegisteredEvent(user.Id, user.Username.Value, user.Email.Value),
            cancellationToken);

        return ApiResult<RegisterUserResponse>.Success(new RegisterUserResponse
        {
            Token = token,
            User = new UserDto
            {
                Id = user.Id,
                Username = user.Username.Value,
                Email = user.Email.Value
            }
        });
    }
}

