namespace Microsoft.Extensions.DependencyInjection;

public static class Extension
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddKeyedHostedService<TImplementation>(int instance)
        where TImplementation : class, IHostedService
        {
            services.AddKeyedSingleton<TImplementation>(instance);
            services.AddSingleton<IHostedService, TImplementation>(sp => Factory(sp, instance));

            return services;

            static TImplementation Factory(IServiceProvider sp, object key) => sp.GetRequiredKeyedService<TImplementation>(key);
        }
    }
}
