using Azure.Security.KeyVault.Secrets;
using System.Threading.Channels;

namespace WorkerService1;

public class SecretGenerator : IHostedService
{
    private readonly SecretClient _secretClient;
    private readonly ILogger<SecretGenerator> _logger;

    public SecretGenerator(SecretClient secretClient, ILogger<SecretGenerator> logger)
    {
        _secretClient = secretClient;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        const int noOfSecrets = 1000;
        _logger.LogInformation("Started creating {NoOfSecrets} at: {time}", noOfSecrets, DateTimeOffset.Now);

        for (int i = 1; i <= noOfSecrets; i++)
        {
            await _secretClient.SetSecretAsync(i.ToString(), Guid.NewGuid().ToString(), cancellationToken);
            _logger.LogInformation("Created secret: {NoOfSecret}", i);
        }

        _logger.LogInformation("Completed creating {NoOfSecrets} at: {time}", noOfSecrets, DateTimeOffset.Now);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("SecretGenerator Stopping at: {time}", DateTimeOffset.Now);

        return Task.CompletedTask;
    }
}

public class SecretNameWorker : IHostedService
{
    private readonly SecretClient _secretClient;
    private readonly Channel<string> _channel;
    private readonly ILogger<SecretNameWorker> _logger;

    public SecretNameWorker(SecretClient secretClient, Channel<string> channel, ILogger<SecretNameWorker> logger)
    {
        _secretClient = secretClient;
        _channel = channel;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
        _logger.LogInformation("SecretNameWorker Started at: {time}", DateTimeOffset.Now);

        await foreach (var secretProperties in _secretClient.GetPropertiesOfSecretsAsync(cancellationToken))
        {
            //await Task.Delay(Random.Shared.Next(100, 2000), cancellationToken);
            await _channel.Writer.WriteAsync(secretProperties.Name, cancellationToken);
            _logger.LogInformation("Found secret: {secretName}", secretProperties.Name);
        }

        _channel.Writer.Complete();

        _logger.LogInformation("SecretNameWorker Completed at: {time}", DateTimeOffset.Now);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("SecretNameWorker Stopping at: {time}", DateTimeOffset.Now);

        return Task.CompletedTask;
    }
}

public class SecretValueWorker : IHostedService
{
    private readonly SecretClient _secretClient;
    private readonly Channel<string> _channel;
    private readonly ILogger<SecretValueWorker> _logger;
    private readonly int _instance;

    public SecretValueWorker(SecretClient secretClient, Channel<string> channel, ILogger<SecretValueWorker> logger, [ServiceKey] int instance)
    {
        _secretClient = secretClient;
        _channel = channel;
        _logger = logger;
        _instance = instance;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = _logger.BeginScope("Instance:{Instance}", _instance);
        _logger.LogInformation("SecretValueWorker Started at: {time}", DateTimeOffset.Now);

        //while (await _channel.Reader.WaitToReadAsync(cancellationToken))
        //{
        //    var secretName = await _channel.Reader.ReadAsync(cancellationToken);
        //    KeyVaultSecret secret = await _secretClient.GetSecretAsync(secretName, cancellationToken: cancellationToken);
        //    _logger.LogInformation("Read secret value {secretName}:{SecretValue}", secretName, secret.Value);
        //}

        await foreach (var secretName in _channel.Reader.ReadAllAsync(cancellationToken))
        {
            await Task.Delay(Random.Shared.Next(100, 2000), cancellationToken);
            KeyVaultSecret secret = await _secretClient.GetSecretAsync(secretName, cancellationToken: cancellationToken);
            _logger.LogInformation("Instance {Instance}: Read secret value {secretName}:{SecretValue}", _instance, secretName, secret.Value);
        }

        _logger.LogInformation("SecretValueWorker Completed at: {time}", DateTimeOffset.Now);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("SecretValueWorker Stopping at: {time}", DateTimeOffset.Now);

        return Task.CompletedTask;
    }
}
