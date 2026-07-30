using Ayoos.Application.Common.Behaviors;
using Ayoos.Application.Practices.CreatePractice;
using Ayoos.Application.Practices.UpdatePractice;
using Ayoos.Application.Providers;
using Ayoos.Application.Providers.AddAvailabilityException;
using Ayoos.Application.Providers.CreateProvider;
using Ayoos.Application.Providers.DeactivateProvider;
using Ayoos.Application.Providers.GetProviderAvailability;
using Ayoos.Application.Providers.RemoveAvailabilityException;
using Ayoos.Application.Providers.SetAvailabilityRules;
using Ayoos.Application.Providers.UpdateProvider;
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
        services.AddScoped<IValidator<CreateProviderCommand>, CreateProviderCommandValidator>();
        services.AddScoped<IValidator<UpdateProviderCommand>, UpdateProviderCommandValidator>();
        services.AddScoped<
            IValidator<DeactivateProviderCommand>,
            DeactivateProviderCommandValidator>();
        services.AddScoped<
            IValidator<SetAvailabilityRulesCommand>,
            SetAvailabilityRulesCommandValidator>();
        services.AddScoped<
            IValidator<AddAvailabilityExceptionCommand>,
            AddAvailabilityExceptionCommandValidator>();
        services.AddScoped<
            IValidator<RemoveAvailabilityExceptionCommand>,
            RemoveAvailabilityExceptionCommandValidator>();
        services.AddScoped<
            IValidator<GetProviderAvailabilityQuery>,
            GetProviderAvailabilityQueryValidator>();
        services.AddSingleton<AvailabilitySlotGenerator>();
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

        return services;
    }
}
