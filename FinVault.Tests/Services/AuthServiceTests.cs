using FinVault.API.DTOs.Auth;
using FinVault.API.Helpers;
using FinVault.API.Models;
using FinVault.API.Services;
using FinVault.Tests.Helpers;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;

namespace FinVault.Tests.Services;

public class AuthServiceTests
{
    private readonly Mock<IConfiguration> _configMock;
    private readonly JwtHelper _jwtHelper;

    public AuthServiceTests()
    {
        // set up fake config for jwt
        _configMock = new Mock<IConfiguration>();
        _configMock.Setup(c => c["Jwt:Key"]).Returns("TestSecretKey_AtLeast32Characters_2025!!");
        _configMock.Setup(c => c["Jwt:Issuer"]).Returns("TestIssuer");
        _configMock.Setup(c => c["Jwt:Audience"]).Returns("TestAudience");

        _jwtHelper = new JwtHelper(_configMock.Object);
    }

    [Fact]
    public async Task Register_WithValidData_ReturnsToken()
    {
        var db = TestDbContextFactory.Create("auth_register_ok");
        var service = new AuthService(db, _jwtHelper);

        var request = new RegisterRequest
        {
            FirstName = "Jane",
            LastName = "Doe",
            Email = "jane@example.com",
            Password = "SecurePass1!",
            PhoneNumber = "1234567890",
            DateOfBirth = new DateTime(1995, 1, 1),
            Address = "123 Main St"
        };

        var result = await service.RegisterAsync(request);

        Assert.NotEmpty(result.Token);
        Assert.Equal("jane@example.com", result.Email);
        Assert.Equal("Jane Doe", result.FullName);
    }

    [Fact]
    public async Task Register_WithDuplicateEmail_ThrowsException()
    {
        var db = TestDbContextFactory.Create("auth_register_dup");
        var service = new AuthService(db, _jwtHelper);

        // add existing user
        db.Users.Add(new User
        {
            Email = "existing@example.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("pass"),
            FirstName = "Old",
            LastName = "User"
        });
        await db.SaveChangesAsync();

        var request = new RegisterRequest
        {
            Email = "existing@example.com",
            Password = "NewPass1!",
            FirstName = "New",
            LastName = "User",
            PhoneNumber = "0987654321",
            DateOfBirth = new DateTime(1990, 5, 10)
        };

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.RegisterAsync(request));
    }

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsToken()
    {
        var db = TestDbContextFactory.Create("auth_login_ok");
        var service = new AuthService(db, _jwtHelper);

        // register first
        await service.RegisterAsync(new RegisterRequest
        {
            Email = "login@example.com",
            Password = "MyPassword1!",
            FirstName = "Test",
            LastName = "User",
            PhoneNumber = "1112223333",
            DateOfBirth = new DateTime(1992, 3, 15)
        });

        var result = await service.LoginAsync(new LoginRequest
        {
            Email = "login@example.com",
            Password = "MyPassword1!"
        });

        Assert.NotEmpty(result.Token);
    }

    [Fact]
    public async Task Login_WithWrongPassword_ThrowsUnauthorized()
    {
        var db = TestDbContextFactory.Create("auth_login_wrong_pass");
        var service = new AuthService(db, _jwtHelper);

        await service.RegisterAsync(new RegisterRequest
        {
            Email = "user@example.com",
            Password = "CorrectPass1!",
            FirstName = "A",
            LastName = "B",
            PhoneNumber = "9998887777",
            DateOfBirth = new DateTime(1988, 8, 8)
        });

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.LoginAsync(new LoginRequest
            {
                Email = "user@example.com",
                Password = "WrongPassword!"
            }));
    }

    [Fact]
    public async Task Login_WithNonExistentEmail_ThrowsUnauthorized()
    {
        var db = TestDbContextFactory.Create("auth_login_no_user");
        var service = new AuthService(db, _jwtHelper);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.LoginAsync(new LoginRequest
            {
                Email = "nobody@example.com",
                Password = "SomePass1!"
            }));
    }
}
