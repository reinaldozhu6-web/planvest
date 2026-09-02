using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

namespace PlanVest.Api.Tests;

public sealed class ApiIntegrationTests : IClassFixture<PlanVestApiHost>
{
    private readonly HttpClient client;
    private readonly PlanVestApiHost host;

    public ApiIntegrationTests(PlanVestApiHost host)
    {
        this.host = host;
        client = host.Client;
    }

    [Fact]
    public async Task ProtectedPortfolioWorkflow_IsAuthenticatedAndUserIsolated()
    {
        client.DefaultRequestHeaders.Authorization = null;
        Assert.Equal(HttpStatusCode.Unauthorized,
            (await client.GetAsync("/api/dashboard")).StatusCode);

        var userAToken = await Register("user-a@example.test");
        var userBToken = await Register("user-b@example.test");

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", userAToken);
        var accountResponse = await client.PostAsJsonAsync("/api/accounts",
            new { name = "Interview TFSA", accountType = "Tfsa" });
        Assert.Equal(HttpStatusCode.Created, accountResponse.StatusCode);
        var accountId = (await accountResponse.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        var invalidAccountType = await client.PostAsJsonAsync("/api/accounts",
            new { name = "Invalid account", accountType = 999 });
        Assert.Equal(HttpStatusCode.BadRequest, invalidAccountType.StatusCode);

        var unknownAccountType = await client.PostAsJsonAsync("/api/accounts",
            new { name = "Unknown account", accountType = "DefinitelyNotAnAccountType" });
        Assert.Equal(HttpStatusCode.BadRequest, unknownAccountType.StatusCode);

        var accountsAfterInvalidRequests = await client.GetFromJsonAsync<JsonElement>("/api/accounts");
        Assert.Equal(1, accountsAfterInvalidRequests.GetArrayLength());

        var holdingResponse = await client.PostAsJsonAsync($"/api/accounts/{accountId}/holdings", new
        {
            symbol = "TEST",
            assetName = "Synthetic equity",
            assetClass = "CanadianEquity",
            quantity = 4.5m,
            averageCost = 95m,
            currentPrice = 100m
        });
        Assert.Equal(HttpStatusCode.Created, holdingResponse.StatusCode);
        var holdingId = (await holdingResponse.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        var invalidAssetClass = await client.PostAsJsonAsync($"/api/accounts/{accountId}/holdings", new
        {
            symbol = "BAD",
            assetName = "Invalid asset class",
            assetClass = 999,
            quantity = 1m,
            averageCost = 1m,
            currentPrice = 1m
        });
        Assert.Equal(HttpStatusCode.BadRequest, invalidAssetClass.StatusCode);

        var invalidTransactionType = await client.PostAsJsonAsync(
            $"/api/accounts/{accountId}/transactions", new
            {
                type = 999,
                holdingId,
                quantity = 1m,
                price = 1m,
                amount = 1m,
                transactionDate = DateOnly.FromDateTime(DateTime.UtcNow).ToString("yyyy-MM-dd")
            });
        Assert.Equal(HttpStatusCode.BadRequest, invalidTransactionType.StatusCode);

        var dashboardResponse = await client.GetAsync("/api/dashboard");
        var dashboardBody = await dashboardResponse.Content.ReadAsStringAsync();
        Assert.True(dashboardResponse.IsSuccessStatusCode,
            $"Dashboard returned {(int)dashboardResponse.StatusCode}: {dashboardBody}{Environment.NewLine}{host.Output}");
        var dashboard = JsonSerializer.Deserialize<JsonElement>(dashboardBody);
        Assert.Equal(450m, dashboard.GetProperty("portfolio").GetProperty("totalMarketValue").GetDecimal());

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", userBToken);
        Assert.Equal(HttpStatusCode.NotFound,
            (await client.GetAsync($"/api/accounts/{accountId}")).StatusCode);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", userAToken);
        Assert.Equal(HttpStatusCode.NoContent,
            (await client.PostAsync("/api/auth/logout", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized,
            (await client.GetAsync("/api/dashboard")).StatusCode);
    }

    [Fact]
    public async Task DemoRiskGoalAndSimulationWorkflow_RunsEndToEnd()
    {
        client.DefaultRequestHeaders.Authorization = null;
        var demoResponse = await client.PostAsync("/api/auth/demo-session", null);
        Assert.Equal(HttpStatusCode.Created, demoResponse.StatusCode);
        var demo = await demoResponse.Content.ReadFromJsonAsync<JsonElement>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
            demo.GetProperty("accessToken").GetString());

        var dashboard = await client.GetFromJsonAsync<JsonElement>("/api/dashboard");
        Assert.Equal(2, dashboard.GetProperty("portfolio").GetProperty("accountCount").GetInt32());
        Assert.True(dashboard.GetProperty("portfolio").GetProperty("totalMarketValue").GetDecimal() > 0);
        Assert.Equal("Growth", dashboard.GetProperty("latestRiskAssessment").GetProperty("riskProfile").GetString());

        var riskResponse = await client.PostAsJsonAsync("/api/risk/assessments", new
        {
            answers = new Dictionary<string, string>
            {
                ["timeHorizon"] = "overSeven",
                ["incomeStability"] = "veryStable",
                ["emergencyFund"] = "overThree",
                ["knowledge"] = "experienced",
                ["lossReaction"] = "add",
                ["liquidity"] = "low",
                ["objective"] = "growth"
            }
        });
        Assert.Equal(HttpStatusCode.Created, riskResponse.StatusCode);

        var invalidGoalType = await client.PostAsJsonAsync("/api/goals", new
        {
            name = "Invalid type",
            goalType = 999,
            targetAmount = 1_000m,
            currentAmount = 0m,
            targetDate = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(1)).ToString("yyyy-MM-dd"),
            monthlyContribution = 0m,
            assumedAnnualReturn = 0m,
            status = "Active"
        });
        Assert.Equal(HttpStatusCode.BadRequest, invalidGoalType.StatusCode);

        var invalidGoalStatus = await client.PostAsJsonAsync("/api/goals", new
        {
            name = "Invalid status",
            goalType = "Other",
            targetAmount = 1_000m,
            currentAmount = 0m,
            targetDate = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(1)).ToString("yyyy-MM-dd"),
            monthlyContribution = 0m,
            assumedAnnualReturn = 0m,
            status = 999
        });
        Assert.Equal(HttpStatusCode.BadRequest, invalidGoalStatus.StatusCode);

        var goalResponse = await client.PostAsJsonAsync("/api/goals", new
        {
            name = "Interview goal",
            goalType = "Other",
            targetAmount = 25_000m,
            currentAmount = 5_000m,
            targetDate = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(5)).ToString("yyyy-MM-dd"),
            monthlyContribution = 250m,
            assumedAnnualReturn = 5m,
            status = "Active"
        });
        Assert.Equal(HttpStatusCode.Created, goalResponse.StatusCode);

        var simulation = await client.PostAsJsonAsync("/api/simulations/future-value", new
        {
            principal = 10_000m,
            monthlyContribution = 500m,
            annualRatePercent = 0m,
            months = 12
        });
        Assert.Equal(HttpStatusCode.OK, simulation.StatusCode);
        Assert.Equal(16_000m,
            (await simulation.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("value").GetDecimal());
    }

    private async Task<string> Register(string email)
    {
        var response = await client.PostAsJsonAsync("/api/auth/register",
            new { displayName = "Test User", email, password = "Test-password-2026" });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("accessToken").GetString()!;
    }
}

public sealed class PlanVestApiHost : IAsyncLifetime
{
    private readonly Process process;
    private readonly string databasePath;
    private readonly StringBuilder output = new();
    public HttpClient Client { get; }
    public string Output => output.ToString();

    public PlanVestApiHost() : this(null) { }

    internal PlanVestApiHost(int? authPermitLimit)
    {
        var port = AvailablePort();
        var repositoryRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,
            "..", "..", "..", "..", ".."));
        var projectPath = Path.Combine(repositoryRoot, "apps", "api", "PlanVest.Api.csproj");
        databasePath = Path.Combine(Path.GetTempPath(), $"planvest-tests-{Guid.NewGuid():N}.db");

        var start = new ProcessStartInfo("dotnet")
        {
            WorkingDirectory = repositoryRoot,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        start.ArgumentList.Add("run");
        start.ArgumentList.Add("--no-build");
        start.ArgumentList.Add("--no-launch-profile");
        start.ArgumentList.Add("--configuration");
        start.ArgumentList.Add("Release");
        start.ArgumentList.Add("--project");
        start.ArgumentList.Add(projectPath);
        start.Environment["ASPNETCORE_ENVIRONMENT"] = "Testing";
        start.Environment["ASPNETCORE_URLS"] = $"http://127.0.0.1:{port}";
        start.Environment["ConnectionStrings__DefaultConnection"] = $"Data Source={databasePath}";
        start.Environment["Jwt__Key"] = "testing-only-signing-key-never-use-in-production-2026";
        start.Environment["DOTNET_ROLL_FORWARD"] = "Major";
        if (authPermitLimit is not null)
            start.Environment["RateLimiting__AuthPermitLimit"] = authPermitLimit.Value.ToString();

        process = new Process { StartInfo = start };
        process.OutputDataReceived += (_, args) => { if (args.Data is not null) output.AppendLine(args.Data); };
        process.ErrorDataReceived += (_, args) => { if (args.Data is not null) output.AppendLine(args.Data); };
        Client = new HttpClient
        {
            BaseAddress = new Uri($"http://127.0.0.1:{port}"),
            Timeout = TimeSpan.FromSeconds(3)
        };
    }

    public async Task InitializeAsync()
    {
        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        for (var attempt = 0; attempt < 100; attempt++)
        {
            if (process.HasExited)
                throw new InvalidOperationException($"PlanVest API exited during test startup.{Environment.NewLine}{output}");
            try
            {
                var response = await Client.GetAsync("/api/health");
                if (response.IsSuccessStatusCode) return;
            }
            catch (HttpRequestException) { }
            catch (TaskCanceledException) { }
            await Task.Delay(100);
        }
        throw new TimeoutException($"PlanVest API did not become healthy.{Environment.NewLine}{output}");
    }

    public async Task DisposeAsync()
    {
        Client.Dispose();
        if (!process.HasExited)
        {
            process.Kill(entireProcessTree: true);
            await process.WaitForExitAsync();
        }
        process.Dispose();
        DeleteIfPresent(databasePath);
        DeleteIfPresent($"{databasePath}-shm");
        DeleteIfPresent($"{databasePath}-wal");
    }

    private static int AvailablePort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    private static void DeleteIfPresent(string path)
    {
        if (File.Exists(path)) File.Delete(path);
    }
}

public sealed class AuthRateLimitIntegrationTests : IClassFixture<RateLimitedPlanVestApiHost>
{
    private readonly HttpClient client;

    public AuthRateLimitIntegrationTests(RateLimitedPlanVestApiHost host) => client = host.Client;

    [Fact]
    public async Task AuthRateLimit_IsClientPartitionedAndReturnsTooManyRequests()
    {
        var first = await client.PostAsync("/api/auth/demo-session", null);
        var second = await client.PostAsync("/api/auth/demo-session", null);
        var rejected = await client.PostAsync("/api/auth/demo-session", null);

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.Created, second.StatusCode);
        Assert.Equal(HttpStatusCode.TooManyRequests, rejected.StatusCode);

        var firstBody = await first.Content.ReadFromJsonAsync<JsonElement>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
            firstBody.GetProperty("accessToken").GetString());
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/auth/me")).StatusCode);
    }
}

public sealed class RateLimitedPlanVestApiHost : IAsyncLifetime
{
    private readonly PlanVestApiHost inner = new(authPermitLimit: 2);
    public HttpClient Client => inner.Client;
    public Task InitializeAsync() => inner.InitializeAsync();
    public Task DisposeAsync() => inner.DisposeAsync();
}
