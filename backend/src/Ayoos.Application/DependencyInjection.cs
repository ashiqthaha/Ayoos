using Ayoos.Application.Common.Behaviors;
using Ayoos.Application.Bookings.CancelBookingByPatient;
using Ayoos.Application.Bookings.CancelBookingByProvider;
using Ayoos.Application.Bookings.CompleteBooking;
using Ayoos.Application.Bookings.ConfirmBooking;
using Ayoos.Application.Bookings.CreateBooking;
using Ayoos.Application.Bookings.GetProviderSchedule;
using Ayoos.Application.Bookings.MarkNoShow;
using Ayoos.Application.Practices.CreatePractice;
using Ayoos.Application.Practices.UpdatePractice;
using Ayoos.Application.PracticeInvitations.CreatePracticeInvitation;
using Ayoos.Application.PracticeInvitations.GetInvitationByToken;
using Ayoos.Application.PracticeInvitations.ListPracticeInvitations;
using Ayoos.Application.PracticeInvitations.RevokePracticeInvitation;
using Ayoos.Application.Patients.DeactivatePatient;
using Ayoos.Application.Patients.LinkPatientToKeycloakUser;
using Ayoos.Application.Patients.RegisterPatient;
using Ayoos.Application.Patients.UpdatePatient;
using Ayoos.Application.Providers;
using Ayoos.Application.Providers.CreateAvailabilityException;
using Ayoos.Application.Providers.CreateAvailabilitySchedule;
using Ayoos.Application.Providers.CreateProvider;
using Ayoos.Application.Providers.DeleteAvailabilityException;
using Ayoos.Application.Providers.DeleteAvailabilitySchedule;
using Ayoos.Application.Providers.DeactivateProvider;
using Ayoos.Application.Providers.GenerateSlotPreview;
using Ayoos.Application.Providers.GetProviderExceptions;
using Ayoos.Application.Providers.PreviewScheduleOverlap;
using Ayoos.Application.Providers.UpdateAvailabilitySchedule;
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
        services.AddScoped<
            IValidator<CreatePracticeInvitationCommand>,
            CreatePracticeInvitationCommandValidator>();
        services.AddScoped<
            IValidator<RevokePracticeInvitationCommand>,
            RevokePracticeInvitationCommandValidator>();
        services.AddScoped<
            IValidator<ListPracticeInvitationsQuery>,
            ListPracticeInvitationsQueryValidator>();
        services.AddScoped<
            IValidator<GetInvitationByTokenQuery>,
            GetInvitationByTokenQueryValidator>();
        services.AddScoped<
            IValidator<RegisterPatientCommand>,
            RegisterPatientCommandValidator>();
        services.AddScoped<
            IValidator<UpdatePatientCommand>,
            UpdatePatientCommandValidator>();
        services.AddScoped<
            IValidator<DeactivatePatientCommand>,
            DeactivatePatientCommandValidator>();
        services.AddScoped<
            IValidator<LinkPatientToKeycloakUserCommand>,
            LinkPatientToKeycloakUserCommandValidator>();
        services.AddScoped<IValidator<CreateProviderCommand>, CreateProviderCommandValidator>();
        services.AddScoped<IValidator<UpdateProviderCommand>, UpdateProviderCommandValidator>();
        services.AddScoped<
            IValidator<DeactivateProviderCommand>,
            DeactivateProviderCommandValidator>();
        services.AddScoped<
            IValidator<CreateAvailabilityScheduleCommand>,
            CreateAvailabilityScheduleCommandValidator>();
        services.AddScoped<
            IValidator<UpdateAvailabilityScheduleCommand>,
            UpdateAvailabilityScheduleCommandValidator>();
        services.AddScoped<
            IValidator<DeleteAvailabilityScheduleCommand>,
            DeleteAvailabilityScheduleCommandValidator>();
        services.AddScoped<
            IValidator<CreateAvailabilityExceptionCommand>,
            CreateAvailabilityExceptionCommandValidator>();
        services.AddScoped<
            IValidator<DeleteAvailabilityExceptionCommand>,
            DeleteAvailabilityExceptionCommandValidator>();
        services.AddScoped<
            IValidator<GetProviderExceptionsQuery>,
            GetProviderExceptionsQueryValidator>();
        services.AddScoped<
            IValidator<PreviewScheduleOverlapQuery>,
            PreviewScheduleOverlapQueryValidator>();
        services.AddScoped<
            IValidator<GenerateSlotPreviewQuery>,
            GenerateSlotPreviewQueryValidator>();
        services.AddScoped<IValidator<CreateBookingCommand>, CreateBookingCommandValidator>();
        services.AddScoped<IValidator<ConfirmBookingCommand>, ConfirmBookingCommandValidator>();
        services.AddScoped<
            IValidator<CancelBookingByPatientCommand>,
            CancelBookingByPatientCommandValidator>();
        services.AddScoped<
            IValidator<CancelBookingByProviderCommand>,
            CancelBookingByProviderCommandValidator>();
        services.AddScoped<IValidator<CompleteBookingCommand>, CompleteBookingCommandValidator>();
        services.AddScoped<IValidator<MarkNoShowCommand>, MarkNoShowCommandValidator>();
        services.AddScoped<
            IValidator<GetProviderScheduleQuery>,
            GetProviderScheduleQueryValidator>();
        services.AddSingleton<AvailabilitySlotGenerator>();
        services.AddSingleton(TimeProvider.System);
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

        return services;
    }
}
