using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace RealEstateAI.Api.Tests;

public sealed class ApiFactory : WebApplicationFactory<Program>;

public sealed class ApiFixture : IAsyncLifetime
{
    public ApiFactory Factory { get; } = new();
    public HttpClient Client { get; private set; } = null!;

    public Task InitializeAsync()
    {
        Client = Factory.CreateClient();
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        Client.Dispose();
        await Factory.DisposeAsync();
    }
}

[CollectionDefinition("API")]
public sealed class ApiCollection : ICollectionFixture<ApiFixture>;
