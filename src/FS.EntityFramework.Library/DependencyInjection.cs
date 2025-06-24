using FS.EntityFramework.Library.UnitOfWorks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FS.EntityFramework.Library;

public static class DependencyInjection
{
    public static IServiceCollection AddGenericUnitOfWork<TContext>(this IServiceCollection services)
        where TContext : DbContext
    {
        // DbContext zaten dışarıdan eklenmiş olmalı
        services.AddScoped<IUnitOfWork, UnitOfWork>(provider =>
        {
            var context = provider.GetRequiredService<TContext>();
            return new UnitOfWork(context, provider);
        });

        return services;
    }
}