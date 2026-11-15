
using Bloqueio.Distribuido.Application.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Bloqueio.Distribuido.API.Middlewares;

public sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext,Exception exception,CancellationToken cancellationToken)
    {
        var (statusCode, title, detail) = exception switch
        {
            ArgumentException => (StatusCodes.Status400BadRequest, "Requisição inválida", exception.Message),
            DistributedLockUnavailableException => (StatusCodes.Status409Conflict, "Conflito na operação", exception.Message),
            TimeoutException => (StatusCodes.Status408RequestTimeout, "Tempo limite excedido", exception.Message),
            InvalidOperationException => (StatusCodes.Status422UnprocessableEntity, "Operação não permitida", exception.Message),
                                    _ =>(StatusCodes.Status500InternalServerError,"Erro interno do servidor","Ocorreu um erro inesperado ao processar a requisição.")
        };

        if (statusCode >= 500)
        {
            logger.LogError( exception,"Erro não tratado. Path: {Path}",httpContext.Request.Path);
        }
        else
        {
            logger.LogWarning(exception,"Exceção durante a requisição. Path: {Path} | Status: {StatusCode}",httpContext.Request.Path,statusCode);
        }

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail,
            Instance = httpContext.Request.Path
        };

        httpContext.Response.StatusCode = statusCode;

        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

        return true;
    }
}
