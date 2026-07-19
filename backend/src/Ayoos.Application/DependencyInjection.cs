using Ayoos.Application.Common.Behaviors;
using Ayoos.Application.Practices.CreatePractice;
using Ayoos.Application.Practices.UpdatePractice;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Ayoos.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddMediatR(configuration =>
            configuration.RegisterServicesFromAssemblyContaining<AssemblyReference>());
        services.AddScoped<IValidator<CreatePracticeCommand>, CreatePracticeCommandValidator>();
        services.AddScoped<IValidator<UpdatePracticeCommand>, UpdatePracticeCommandValidator>();
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

        return services;
    }
}
