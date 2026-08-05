using Ayoos.Application.Common.Exceptions;
using Ayoos.Domain.Common;
using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Ayoos.Api.ErrorHandling;

internal sealed class GlobalExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is ValidationException validationException)
        {
            var errors = validationException.Errors
                .GroupBy(error => error.PropertyName)
                .ToDictionary(
                    group => group.Key,
                    group => group.Select(error => error.ErrorMessage).Distinct().ToArray());

            var validationProblemDetails = new HttpValidationProblemDetails(errors)
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "One or more validation errors occurred.",
                Type = "https://tools.ietf.org/html/rfc9110#section-15.5.1"
            };

            httpContext.Response.StatusCode = validationProblemDetails.Status.Value;
            await httpContext.Response.WriteAsJsonAsync(
                validationProblemDetails,
                cancellationToken);
            return true;
        }

        ProblemDetails problemDetails;

        switch (exception)
        {
            case BadHttpRequestException:
                problemDetails = new ProblemDetails
                {
                    Status = StatusCodes.Status400BadRequest,
                    Title = "The request could not be read.",
                    Detail = exception.Message
                };
                break;

            case DomainException:
                problemDetails = new ProblemDetails
                {
                    Status = StatusCodes.Status400BadRequest,
                    Title = "The request violates a domain rule.",
                    Detail = exception.Message
                };
                break;

            case NotFoundException:
                problemDetails = new ProblemDetails
                {
                    Status = StatusCodes.Status404NotFound,
                    Title = "Resource not found.",
                    Detail = exception.Message
                };
                break;

            case ForbiddenException:
                problemDetails = new ProblemDetails
                {
                    Status = StatusCodes.Status403Forbidden,
                    Title = "Access denied.",
                    Detail = exception.Message
                };
                break;

            case GoneException:
                problemDetails = new ProblemDetails
                {
                    Status = StatusCodes.Status410Gone,
                    Title = "Resource no longer available.",
                    Detail = exception.Message
                };
                break;

            case ConflictException:
                problemDetails = new ProblemDetails
                {
                    Status = StatusCodes.Status409Conflict,
                    Title = "Resource conflict.",
                    Detail = exception.Message
                };
                break;

            default:
                return false;
        }

        httpContext.Response.StatusCode = problemDetails.Status!.Value;
        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);
        return true;
    }
}
