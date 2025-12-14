using AzureKeyVaultEmulator.Aspire.Client;
using System.Threading.Channels;
using WorkerService1;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddHostedService<SecretGenerator>();
//builder.Services.AddHostedService<SecretNameWorker>();
builder.Services.AddHostedService<SecretNameUsingRESTWorker>();
for (int i = 1; i <= 3; i++)
{
    builder.Services.AddKeyedHostedService<SecretValueWorker>(i);
}

builder.Services.Configure<HostOptions>(options =>
{
    options.ServicesStartConcurrently = true;
    options.ServicesStopConcurrently = true;
});

// Injected by Aspire using the name "keyvault".
var vaultUri = builder.Configuration.GetConnectionString("keyvault") ?? string.Empty;

// Basic Secrets only implementation
builder.Services.AddAzureKeyVaultEmulator(vaultUri);
builder.Services.AddSingleton(new EmulatedTokenCredential(vaultUri));

builder.Services.AddSingleton(Channel.CreateBounded<string>(new BoundedChannelOptions(100)
{
    FullMode = BoundedChannelFullMode.Wait,
    SingleReader = false,
    SingleWriter = true
}));

var host = builder.Build();
host.Run();
