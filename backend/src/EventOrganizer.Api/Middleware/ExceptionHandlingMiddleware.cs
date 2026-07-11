using EventOrganizer.Application.Common.Exceptions;
using FluentValidation;
using System.Net;

namespace EventOrganizer.Api.Middleware
{
    public sealed class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionHandlingMiddleware> _logger;

        public ExceptionHandlingMiddleware(
            RequestDelegate next,
            ILogger<ExceptionHandlingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception exception)
            {
                await HandleExceptionAsync(context, exception);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            var (statusCode, title, errors) = exception switch
            {
                ValidationException validationException => (
                    HttpStatusCode.BadRequest,
                    "Validation failed.",
                    validationException.Errors.Select(error => error.ErrorMessage).ToArray()),

                ConflictException => (
                    HttpStatusCode.Conflict,
                    exception.Message,
                    Array.Empty<string>()),

                UnauthorizedException => (
                    HttpStatusCode.Unauthorized,
                    exception.Message,
                    Array.Empty<string>()),

                NotFoundException => (
                    HttpStatusCode.NotFound,
                    exception.Message,
                    Array.Empty<string>()),

                _ => (
                    HttpStatusCode.InternalServerError,
                    "An unexpected error occurred.",
                    Array.Empty<string>()),
            };

            if (statusCode == HttpStatusCode.InternalServerError)
            {
                _logger.LogError(exception, "Unhandled exception occurred.");
            }

            context.Response.StatusCode = (int)statusCode;
            context.Response.ContentType = "application/json";

            await context.Response.WriteAsJsonAsync(new
            {
                status = context.Response.StatusCode,
                title,
                errors,
            });
        }
    }
}
