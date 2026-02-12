using Honeydew.Data;
using Honeydew.Entities;
using Honeydew.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Honeydew.Tests.Services;

[TestClass]
public class AuthServiceTest
{
    private Mock<IJwtService> _mockJwt = null!;

    [TestInitialize]
    public void Initialize()
    {
        _mockJwt = CreateMockJwt();
    }

    private static HoneydewDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<HoneydewDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new HoneydewDbContext(options);
    }

    private static Mock<IJwtService> CreateMockJwt()
    {
        var mock = new Mock<IJwtService>();
        mock.Setup(j => j.CreateUserToken(It.IsAny<User>(), It.IsAny<Tenant>())).Returns("fake-user-token");
        mock.Setup(j => j.CreateClientToken(It.IsAny<ApiClient>(), It.IsAny<Tenant>())).Returns("fake-client-token");
        return mock;
    }

    private AuthService CreateAuthService(HoneydewDbContext db)
    {
        var authData = new AuthDataAccess(db);
        var tenantData = new TenantDataAccess(db);
        return new AuthService(authData, tenantData, _mockJwt.Object);
    }

    [TestMethod]
    public async Task RegisterTenant_ValidInput_CreatesTenantAndUser_ReturnsToken()
    {
        // Arrange
        await using var db = CreateDb();
        var sut = CreateAuthService(db);

        // Act
        var result = await sut.RegisterTenant(
            "Acme", "owner@acme.com", "Secret123!", "Owner", CancellationToken.None);

        // Assert
        Assert.IsTrue(result.Success);
        Assert.IsNotNull(result.Token);
        Assert.IsNull(result.Error);
        var tenant = await db.Tenants.SingleAsync();
        Assert.AreEqual("Acme", tenant.Name);
        var user = await db.Users.Include(u => u.Tenant).SingleAsync();
        Assert.AreEqual("owner@acme.com", user.Email);
        Assert.AreEqual(UserRole.Owner, user.Role);
        Assert.IsTrue(user.CanViewAllTodos);
        Assert.IsTrue(user.CanEditAllTodos);
    }

    [TestMethod]
    public async Task RegisterTenant_DuplicateEmail_ReturnsError()
    {
        // Arrange
        await using var db = CreateDb();
        var tenant = new Tenant { Id = Guid.NewGuid(), Name = "T", CreatedAt = DateTime.UtcNow, IsActive = true };
        var user = new User
        {
            Id = Guid.NewGuid(),
            TenantId = tenant.Id,
            Email = "existing@test.com",
            DisplayName = "U",
            PasswordHash = "h",
            PasswordSalt = "s",
            Role = UserRole.Owner,
            CanViewAllTodos = true,
            CanEditAllTodos = true,
            CanCreateUser = true,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        db.Tenants.Add(tenant);
        db.Users.Add(user);
        await db.SaveChangesAsync();
        var sut = CreateAuthService(db);

        // Act
        var result = await sut.RegisterTenant(
            "Other", "existing@test.com", "Secret123!", "Other", CancellationToken.None);

        // Assert
        Assert.IsFalse(result.Success);
        Assert.IsNull(result.Token);
        Assert.IsTrue(result.Error!.Contains("already exists", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public async Task RegisterTenant_EmptyEmailOrPassword_ReturnsError()
    {
        // Arrange
        await using var db = CreateDb();
        var sut = CreateAuthService(db);

        // Act
        var r1 = await sut.RegisterTenant("T", "", "pwd", "U", CancellationToken.None);
        var r2 = await sut.RegisterTenant("T", "e@e.com", "", "U", CancellationToken.None);

        // Assert
        Assert.IsFalse(r1.Success);
        Assert.IsFalse(r2.Success);
        Assert.IsNotNull(r1.Error);
        Assert.IsNotNull(r2.Error);
    }

    [TestMethod]
    public async Task Login_ValidCredentials_ReturnsToken_UpdatesLastLoginAt()
    {
        // Arrange
        await using var db = CreateDb();
        var tenant = new Tenant { Id = Guid.NewGuid(), Name = "T", CreatedAt = DateTime.UtcNow, IsActive = true };
        var (hash, salt) = PasswordHasher.HashPassword("Secret123!");
        var user = new User
        {
            Id = Guid.NewGuid(),
            TenantId = tenant.Id,
            Email = "user@test.com",
            DisplayName = "U",
            PasswordHash = hash,
            PasswordSalt = salt,
            Role = UserRole.Member,
            CanViewAllTodos = false,
            CanEditAllTodos = false,
            CanCreateUser = false,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        db.Tenants.Add(tenant);
        db.Users.Add(user);
        await db.SaveChangesAsync();
        var sut = CreateAuthService(db);

        // Act
        var result = await sut.Login("user@test.com", "Secret123!", null, CancellationToken.None);

        // Assert
        Assert.IsTrue(result.Success);
        Assert.IsNotNull(result.Token);
        await db.Entry(user).ReloadAsync();
        Assert.IsNotNull(user.LastLoginAt);
    }

    [TestMethod]
    public async Task Login_WrongPassword_ReturnsError()
    {
        // Arrange
        await using var db = CreateDb();
        var tenant = new Tenant { Id = Guid.NewGuid(), Name = "T", CreatedAt = DateTime.UtcNow, IsActive = true };
        var (hash, salt) = PasswordHasher.HashPassword("CorrectPassword");
        var user = new User
        {
            Id = Guid.NewGuid(),
            TenantId = tenant.Id,
            Email = "user@test.com",
            DisplayName = "U",
            PasswordHash = hash,
            PasswordSalt = salt,
            Role = UserRole.Member,
            CanViewAllTodos = false,
            CanEditAllTodos = false,
            CanCreateUser = false,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        db.Tenants.Add(tenant);
        db.Users.Add(user);
        await db.SaveChangesAsync();
        var sut = CreateAuthService(db);

        // Act
        var result = await sut.Login("user@test.com", "WrongPassword", null, CancellationToken.None);

        // Assert
        Assert.IsFalse(result.Success);
        Assert.IsNull(result.Token);
        Assert.IsNotNull(result.Error);
    }

    [TestMethod]
    public async Task Login_MultipleTenants_NoTenantId_ReturnsError()
    {
        // Arrange
        await using var db = CreateDb();
        var t1 = new Tenant { Id = Guid.NewGuid(), Name = "T1", CreatedAt = DateTime.UtcNow, IsActive = true };
        var t2 = new Tenant { Id = Guid.NewGuid(), Name = "T2", CreatedAt = DateTime.UtcNow, IsActive = true };
        var (hash, salt) = PasswordHasher.HashPassword("pwd");
        var u1 = new User
        {
            Id = Guid.NewGuid(),
            TenantId = t1.Id,
            Email = "same@test.com",
            DisplayName = "U1",
            PasswordHash = hash,
            PasswordSalt = salt,
            Role = UserRole.Member,
            CanViewAllTodos = false,
            CanEditAllTodos = false,
            CanCreateUser = false,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        var u2 = new User
        {
            Id = Guid.NewGuid(),
            TenantId = t2.Id,
            Email = "same@test.com",
            DisplayName = "U2",
            PasswordHash = hash,
            PasswordSalt = salt,
            Role = UserRole.Member,
            CanViewAllTodos = false,
            CanEditAllTodos = false,
            CanCreateUser = false,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        db.Tenants.AddRange(t1, t2);
        db.Users.AddRange(u1, u2);
        await db.SaveChangesAsync();
        var sut = CreateAuthService(db);

        // Act
        var result = await sut.Login("same@test.com", "pwd", null, CancellationToken.None);

        // Assert
        Assert.IsFalse(result.Success);
        Assert.IsNotNull(result.Error);
        Assert.IsTrue(result.Error.Contains("tenant", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public async Task Login_MultipleTenants_WithCorrectTenantId_ReturnsToken()
    {
        // Arrange
        await using var db = CreateDb();
        var t1 = new Tenant { Id = Guid.NewGuid(), Name = "T1", CreatedAt = DateTime.UtcNow, IsActive = true };
        var t2 = new Tenant { Id = Guid.NewGuid(), Name = "T2", CreatedAt = DateTime.UtcNow, IsActive = true };
        var (hash, salt) = PasswordHasher.HashPassword("pwd");
        var u1 = new User
        {
            Id = Guid.NewGuid(),
            TenantId = t1.Id,
            Email = "same@test.com",
            DisplayName = "U1",
            PasswordHash = hash,
            PasswordSalt = salt,
            Role = UserRole.Member,
            CanViewAllTodos = false,
            CanEditAllTodos = false,
            CanCreateUser = false,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        var u2 = new User
        {
            Id = Guid.NewGuid(),
            TenantId = t2.Id,
            Email = "same@test.com",
            DisplayName = "U2",
            PasswordHash = hash,
            PasswordSalt = salt,
            Role = UserRole.Member,
            CanViewAllTodos = false,
            CanEditAllTodos = false,
            CanCreateUser = false,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        db.Tenants.AddRange(t1, t2);
        db.Users.AddRange(u1, u2);
        await db.SaveChangesAsync();
        var sut = CreateAuthService(db);

        // Act
        var result = await sut.Login("same@test.com", "pwd", t1.Id, CancellationToken.None);

        // Assert
        Assert.IsTrue(result.Success);
        Assert.IsNotNull(result.Token);
    }

    [TestMethod]
    public async Task GetClientToken_ValidClient_ReturnsToken()
    {
        // Arrange
        await using var db = CreateDb();
        var tenant = new Tenant { Id = Guid.NewGuid(), Name = "T", CreatedAt = DateTime.UtcNow, IsActive = true };
        var hash = ClientSecretHasher.Hash("client-secret", "client-id");
        var client = new ApiClient
        {
            Id = Guid.NewGuid(),
            TenantId = tenant.Id,
            ClientId = "client-id",
            ClientSecretHash = hash,
            Name = "Client",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        db.Tenants.Add(tenant);
        db.ApiClients.Add(client);
        await db.SaveChangesAsync();
        var sut = CreateAuthService(db);

        // Act
        var result = await sut.GetClientToken("client-id", "client-secret", CancellationToken.None);

        // Assert
        Assert.IsTrue(result.Success);
        Assert.IsNotNull(result.Token);
    }

    [TestMethod]
    public async Task GetClientToken_InvalidSecret_ReturnsError()
    {
        // Arrange
        await using var db = CreateDb();
        var tenant = new Tenant { Id = Guid.NewGuid(), Name = "T", CreatedAt = DateTime.UtcNow, IsActive = true };
        var hash = ClientSecretHasher.Hash("correct-secret", "client-id");
        var client = new ApiClient
        {
            Id = Guid.NewGuid(),
            TenantId = tenant.Id,
            ClientId = "client-id",
            ClientSecretHash = hash,
            Name = "Client",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        db.Tenants.Add(tenant);
        db.ApiClients.Add(client);
        await db.SaveChangesAsync();
        var sut = CreateAuthService(db);

        // Act
        var result = await sut.GetClientToken("client-id", "wrong-secret", CancellationToken.None);

        // Assert
        Assert.IsFalse(result.Success);
        Assert.IsNull(result.Token);
        Assert.IsNotNull(result.Error);
    }

    [TestMethod]
    public async Task CreateApiClient_ValidInput_CreatesClient_ReturnsClientId()
    {
        // Arrange
        await using var db = CreateDb();
        var tenant = new Tenant { Id = Guid.NewGuid(), Name = "T", CreatedAt = DateTime.UtcNow, IsActive = true };
        db.Tenants.Add(tenant);
        await db.SaveChangesAsync();
        var sut = CreateAuthService(db);

        // Act
        var (success, clientId, error) = await sut.CreateApiClient(
            tenant.Id, "My Client", "my-client-id", "my-secret", CancellationToken.None);

        // Assert
        Assert.IsTrue(success);
        Assert.AreEqual("my-client-id", clientId);
        Assert.IsNull(error);
        var client = await db.ApiClients.SingleAsync();
        Assert.AreEqual("my-client-id", client.ClientId);
        Assert.AreEqual(tenant.Id, client.TenantId);
    }

    [TestMethod]
    public async Task CreateApiClient_DuplicateClientId_ReturnsError()
    {
        // Arrange
        await using var db = CreateDb();
        var tenant = new Tenant { Id = Guid.NewGuid(), Name = "T", CreatedAt = DateTime.UtcNow, IsActive = true };
        var existing = new ApiClient
        {
            Id = Guid.NewGuid(),
            TenantId = tenant.Id,
            ClientId = "taken-id",
            ClientSecretHash = "h",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        db.Tenants.Add(tenant);
        db.ApiClients.Add(existing);
        await db.SaveChangesAsync();
        var sut = CreateAuthService(db);

        // Act
        var (success, clientId, error) = await sut.CreateApiClient(
            tenant.Id, "New", "taken-id", "secret", CancellationToken.None);

        // Assert
        Assert.IsFalse(success);
        Assert.IsNull(clientId);
        Assert.IsNotNull(error);
        Assert.IsTrue(error.Contains("already exists", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public async Task CreateApiClient_TenantNotFound_ReturnsError()
    {
        // Arrange
        await using var db = CreateDb();
        var sut = CreateAuthService(db);

        // Act
        var (success, clientId, error) = await sut.CreateApiClient(
            Guid.NewGuid(), "New", "new-id", "secret", CancellationToken.None);

        // Assert
        Assert.IsFalse(success);
        Assert.IsNull(clientId);
        Assert.IsNotNull(error);
    }
}
