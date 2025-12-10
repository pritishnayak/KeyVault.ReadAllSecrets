using AzureKeyVaultEmulator.Aspire.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

var keyVault = builder.AddAzureKeyVaultEmulator("keyvault");

builder.AddProject<Projects.WorkerService1>("workerservice1")
    .WithReference(keyVault);

builder.Build().Run();
