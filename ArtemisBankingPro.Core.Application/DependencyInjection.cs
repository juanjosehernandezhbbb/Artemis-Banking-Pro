using ArtemisBankingPro.Core.Application.Common.Behaviors;
using ArtemisBankingPro.Core.Application.Mappings;
using AutoMapper;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging; 

namespace ArtemisBankingPro.Core.Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplicationLayer(this IServiceCollection services)
        {
            services.AddSingleton<IMapper>(provider =>
            {
                var loggerFactory = provider.GetRequiredService<ILoggerFactory>();

                var config = new MapperConfiguration(cfg =>
                {
                    cfg.AddProfile<LoginProfile>();
                }, loggerFactory); 

                return config.CreateMapper();
            });

            services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

            services.AddMediatR(cfg =>
            {
                cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly);
                cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));
                cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
                cfg.AddOpenBehavior(typeof(TransactionBehavior<,>));
            });

            return services;
        }
    }
}