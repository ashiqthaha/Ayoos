using Ayoos.Application.Common.Exceptions;
using Ayoos.Application.Common.Interfaces;
using Ayoos.Application.Providers.GetAvailableSlots;
using Ayoos.Domain.Bookings;
using FluentValidation;
using MediatR;

namespace Ayoos.Application.Bookings.CreateBooking;

public sealed record CreateBookingCommand(
    Guid PatientId,
    Guid ProviderId,
    Guid? AvailabilityScheduleId,
    DateTimeOffset StartTime,
    DateTimeOffset EndTime,
    string? Reason) : IRequest<BookingModel>;

public sealed class CreateBookingCommandValidator
    : AbstractValidator<CreateBookingCommand>
{
    public CreateBookingCommandValidator()
    {
        RuleFor(command => command.PatientId).NotEmpty();
        RuleFor(command => command.ProviderId).NotEmpty();
        RuleFor(command => command.AvailabilityScheduleId)
            .NotEqual(Guid.Empty)
            .When(command => command.AvailabilityScheduleId.HasValue);
        RuleFor(command => command.EndTime)
            .GreaterThan(command => command.StartTime)
            .WithMessage("EndTime must be after StartTime.");
        RuleFor(command => command.Reason)
            .MaximumLength(BookingValidation.MaximumReasonLength);
    }
}

internal sealed class CreateBookingCommandHandler(
    IPatientRepository patientRepository,
    IProviderRepository providerRepository,
    IBookingRepository bookingRepository,
    ISender sender)
    : IRequestHandler<CreateBookingCommand, BookingModel>
{
    public async Task<BookingModel> Handle(
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
            cancellationToken: cancellationToken);
        if (provider is null || !provider.IsActive)
        {
            throw new NotFoundException(
                $"Active provider '{request.ProviderId}' was not found.");
        }

        var startTime = request.StartTime.ToUniversalTime();
        var endTime = request.EndTime.ToUniversalTime();
        if (await bookingRepository.HasOverlapAsync(
            provider.Id,
            startTime,
            endTime,
            cancellationToken: cancellationToken))
        {
            throw new ConflictException(
                "The provider already has a booking that overlaps this time.");
        }

        var date = DateOnly.FromDateTime(startTime.UtcDateTime);
        var slots = await sender.Send(
            new GetAvailableSlotsQuery(provider.Id, date, date),
            cancellationToken);
        var matchingSlot = slots.SingleOrDefault(slot =>
            ToUtc(slot.Date, slot.StartTime) == startTime &&
            ToUtc(slot.Date, slot.EndTime) == endTime &&
            (!request.AvailabilityScheduleId.HasValue ||
                slot.AvailabilityScheduleId == request.AvailabilityScheduleId));
        if (matchingSlot is null)
        {
            throw new ConflictException(
                "The requested time does not match an available provider slot.");
        }

        var booking = Booking.Create(
            patient.PracticeId.ToString("D"),
            patient.Id,
            provider.Id,
            matchingSlot.AvailabilityScheduleId,
            startTime,
            endTime,
            request.Reason,
            DateTimeOffset.UtcNow);
        await bookingRepository.AddAsync(booking, cancellationToken);
        await bookingRepository.SaveChangesAsync(cancellationToken);

        return booking.ToModel();
    }

    private static DateTimeOffset ToUtc(DateOnly date, TimeOnly time) =>
        new(date.ToDateTime(time), TimeSpan.Zero);
}
