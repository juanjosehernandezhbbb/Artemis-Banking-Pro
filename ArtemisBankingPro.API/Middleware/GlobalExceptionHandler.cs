using ArtemisBankingPro.Core.Domain.Common.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace ArtemisBankingPro.API.Middleware
{
    public class GlobalExceptionHandler : IExceptionHandler
    {
        private readonly ILogger<GlobalExceptionHandler> _logger;

        public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
        {
            _logger = logger;
        }

        public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
        {
            try
            {
                return await HandleAsync(httpContext, exception, cancellationToken);
            }
            catch (Exception handlerException)
            {
                _logger.LogCritical(handlerException, "El manejador global de excepciones falló procesando {Path}", httpContext.Request.Path);

                try
                {
                    httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
                    await httpContext.Response.WriteAsJsonAsync(
                        new ProblemDetails
                        {
                            Title = "Error interno del servidor",
                            Status = StatusCodes.Status500InternalServerError,
                            Detail = "Ha ocurrido un error inesperado procesando la solicitud.",
                            Instance = httpContext.Request.Path
                        },
                        cancellationToken);
                }
                catch
                {
                    // El cliente ya no está disponible; no hay nada más que se pueda hacer.
                }

                return true;
            }
        }

        private async ValueTask<bool> HandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
        {
            if (exception is OperationCanceledException)
                return false;

            var problemDetails = new ProblemDetails
            {
                Instance = httpContext.Request.Path
            };

            switch (exception)
            {
                case FluentValidation.ValidationException validationException:
                    problemDetails.Title = "Error de validación";
                    problemDetails.Status = StatusCodes.Status400BadRequest;
                    problemDetails.Detail = "Uno o más campos no cumplen con las reglas de validación.";
                    problemDetails.Extensions["errors"] = validationException.Errors.Select(e => new { e.PropertyName, e.ErrorMessage });
                    break;

                case BusinessRuleException businessException:
                    problemDetails.Title = "Regla de negocio no cumplida";
                    problemDetails.Status = StatusCodes.Status400BadRequest;
                    problemDetails.Detail = businessException.Message;
                    break;

                case InvalidTokenException tokenException:
                    problemDetails.Title = "Token inválido";
                    problemDetails.Status = StatusCodes.Status400BadRequest;
                    problemDetails.Detail = tokenException.Message;
                    break;

                case UnauthorizedAccessException unauthorizedException:
                    problemDetails.Title = "No autorizado";
                    problemDetails.Status = StatusCodes.Status401Unauthorized;
                    problemDetails.Detail = unauthorizedException.Message;
                    break;

                case ForbiddenAccessException forbiddenException:
                    problemDetails.Title = "Acceso denegado";
                    problemDetails.Status = StatusCodes.Status403Forbidden;
                    problemDetails.Detail = forbiddenException.Message;
                    break;

                case NotFoundException notFoundException:
                    problemDetails.Title = "Recurso no encontrado";
                    problemDetails.Status = StatusCodes.Status404NotFound;
                    problemDetails.Detail = notFoundException.Message;
                    break;

                case ConflictException conflictException:
                    problemDetails.Title = "Conflicto de datos";
                    problemDetails.Status = StatusCodes.Status409Conflict;
                    problemDetails.Detail = conflictException.Message;
                    break;

                case HighRiskClientException highRiskException:
                    problemDetails.Title = "Cliente de alto riesgo";
                    problemDetails.Status = StatusCodes.Status409Conflict;
                    problemDetails.Detail = highRiskException.Message;
                    problemDetails.Extensions["riskType"] = highRiskException.RiskType;
                    problemDetails.Extensions["currentDebt"] = highRiskException.CurrentDebt;
                    problemDetails.Extensions["projectedDebt"] = highRiskException.ProjectedDebt;
                    problemDetails.Extensions["averageDebt"] = highRiskException.AverageDebt;
                    break;

                case BadHttpRequestException:
                case JsonException:
                    problemDetails.Title = "Solicitud inválida";
                    problemDetails.Status = StatusCodes.Status400BadRequest;
                    problemDetails.Detail = "El cuerpo de la solicitud no es válido o está mal formado.";
                    break;

                case DbUpdateException:
                    _logger.LogError(exception, "Error de base de datos procesando {Path}", httpContext.Request.Path);
                    problemDetails.Title = "Conflicto de datos";
                    problemDetails.Status = StatusCodes.Status409Conflict;
                    problemDetails.Detail = "La operación no pudo completarse por una violación de integridad de los datos.";
                    break;

                default:
                    _logger.LogError(exception, "Error no controlado procesando {Path}", httpContext.Request.Path);

                    problemDetails.Title = "Error interno del servidor";
                    problemDetails.Status = StatusCodes.Status500InternalServerError;
                    problemDetails.Detail = "Ha ocurrido un error inesperado procesando la solicitud.";
                    break;
            }

            httpContext.Response.StatusCode = problemDetails.Status.Value;

            await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

            return true;
        }
    }
}