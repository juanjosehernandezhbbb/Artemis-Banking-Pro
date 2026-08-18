using ArtemisBankingPro.Core.Application.Common.Attributes;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace ArtemisBankingPro.Core.Application.Common.Behaviors
{
    public class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : IRequest<TResponse>
    {
        private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;

        public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
        {
            _logger = logger;
        }

        public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
        {
            var requestName = typeof(TRequest).Name;

            _logger.LogInformation("Iniciando Request: {Name} {@Request}", requestName, BuildSafeLogObject(request));

            var timer = Stopwatch.StartNew();
            var response = await next();
            timer.Stop();

            _logger.LogInformation("Completado Request: {Name} en {ElapsedMilliseconds} ms", requestName, timer.ElapsedMilliseconds);

            return response;
        }

        private static object BuildSafeLogObject(TRequest request)
        {
            var safe = new Dictionary<string, object?>();
            foreach (var prop in typeof(TRequest).GetProperties())
            {
                var isSensitive = prop.GetCustomAttribute<SensitiveDataAttribute>() != null;
                safe[prop.Name] = isSensitive ? "***REDACTED***" : prop.GetValue(request);
            }
            return safe;
        }
    }
}