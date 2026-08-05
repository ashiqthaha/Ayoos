using Ayoos.Application.Common.Exceptions;
using Ayoos.Application.Common.Interfaces;
using Ayoos.Application.Providers;
using Ayoos.Domain.Bookings;
using FluentValidation;
using MediatR;

namespace Ayoos.Application.Bookings.CreateBooking;

public sealed record CreateBookingCommand(
    Guid PatientId,
    Guid ProviderId,
    Guid? AvailabilityScheduleId,
    DateTimeOffset ScheduledStart,
    DateTimeOffset ScheduledEnd,
    string? Reason,
    bool Force = false) : IRequest<CreateBookingResult>;

public sealed class CreateBookingCommandValidator
    : AbstractValidator<CreateBookingCommand>
{
    public CreateBookingCommandValidator()
        : this(null, null, null, TimeProvider.System)
    {
    }

    public CreateBookingCommandValidator(
        IPatientRepository? patientRepository,
        IProviderRepository? providerRepository,
        AvailabilitySlotGenerator? slotGenerator,
        TimeProvider timeProvider)
    {
        RuleFor(command => command.PatientId).NotEmpty();
        RuleFor(command => command.ProviderId).NotEmpty();
        RuleFor(command => command.AvailabilityScheduleId)
            .NotEqual(Guid.Empty)
            .When(command => command.AvailabilityScheduleId.HasValue);
        RuleFor(command => command.ScheduledStart)
            .Must(value => value.Offset == TimeSpan.Zero)
            .WithMessage("ScheduledStart must be expressed in UTC.")
            .GreaterThan(_ => timeProvider.GetUtcNow())
            .WithMessage("ScheduledStart must be in the future.");
        RuleFor(command => command.ScheduledEnd)
            .Must(value => value.Offset == TimeSpan.Zero)
            .WithMessage("ScheduledEnd must be expressed in UTC.")
            .GreaterThan(command => command.ScheduledStart)
            .WithMessage("ScheduledEnd must be after ScheduledStart.");
        RuleFor(command => command.Reason)
            .MaximumLength(BookingValidation.MaximumReasonLength);

        if (patientRepository is null ||
            providerRepository is null ||
            slotGenerator is null)
        {
            return;
        }

        RuleFor(command => command).CustomAsync(async (command, context, cancellationToken) =>
        {
            if (command.PatientId != Guid.Empty)
            {
                var patient = await patientRepository.GetByIdAsync(
                    command.PatientId,
                    includeEmergencyContact: false,
                    cancellationToken);
                if (patient is null || !patient.IsActive)
                {
                    context.AddFailure(
                        nameof(command.PatientId),
                        "An active patient with this identifier does not exist.");
                }
            }

            if (command.ProviderId == Guid.Empty)
            {
                return;
            }

            var provider = await providerRepository.GetByIdAsync(
                command.ProviderId,
                includeAvailability: true,
                cancellationToken);
            if (provider is null || !provider.IsActive)
            {
                context.AddFailure(
                    nameof(command.ProviderId),
                    "An active provider with this identifier does not exist.");
                return;
            }

            if (command.ScheduledEnd > command.ScheduledStart &&
                BookingSlotMatcher.Find(
                    provider,
                    command.AvailabilityScheduleId,
                    command.ScheduledStart,
                    command.ScheduledEnd,
                    slotGenerator) is null)
            {
                context.AddFailure(
                    nameof(command.ScheduledStart),
                    "The requested time does not match a generated, non-excepted provider slot.");
            }
        });
    }
}

internal sealed class CreateBookingCommandHandler(
    IPatientRepository patientRepository,
    IProviderRepository providerRepository,
    IBookingRepository bookingRepository,
    AvailabilitySlotGenerator slotGenerator,
    TimeProvider timeProvider)
    : IRequestHandler<CreateBookingCommand, CreateBookingResult>
{
    public async Task<CreateBookingResult> Handle(
        CreateBookingCommand request,
        CancellationToken cancellationToken)
    {
        var patient = await patientRepository.GetByIdAsync(
            request.PatientId,
            includeEmergencyContact: false,
            cancellationToken);
        if (patient is null || !patient.IsActive)
        {
            throw new NotFoundException(
                $"Active patient '{request.PatientId}' was not found.");
        }

        var provider = await providerRepository.GetByIdAsync(
            request.ProviderId,
            includeAvailability: true,
            cancellationToken);
        if (provider is null || !provider.IsActive)
        {
            throw new NotFoundException(
                $"Active provider '{request.ProviderId}' was not found.");
        }

        var matchingSlot = BookingSlotMatcher.Find(
            provider,
            request.AvailabilityScheduleId,
            request.ScheduledStart,
            request.ScheduledEnd,
            slotGenerator);
        if (matchingSlot is null)
        {
            throw new ConflictException(
                "The requested time does not match a generated, non-excepted provider slot.");
        }

        var conflicts = await bookingRepository.FindActiveOverlapsAsync(
            provider.Id,
            request.ScheduledStart,
            request.ScheduledEnd,
            cancellationToken: cancellationToken);
        var preview = conflicts.ToConflictPreview();
        if (preview.HasConflicts && !request.Force)
        {
            return new CreateBookingResult(null, preview);
        }

        var booking = Booking.Create(
            patient.PracticeId.ToString("D"),
            patient.Id,
            provider.Id,
            matchingSlot.AvailabilityScheduleId,
            request.ScheduledStart,
            request.ScheduledEnd,
            request.Reason,
            timeProvider.GetUtcNow());
        await bookingRepository.AddAsync(booking, cancellationToken);
        await bookingRepository.SaveChangesAsync(cancellationToken);

        return new CreateBookingResult(booking.ToModel(), preview);
    }
}
