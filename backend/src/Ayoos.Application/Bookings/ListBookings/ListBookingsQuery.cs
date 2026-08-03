using Ayoos.Application.Common.Interfaces;
using Ayoos.Domain.Bookings;
using MediatR;

namespace Ayoos.Application.Bookings.ListBookings;

public sealed record ListBookingsQuery(
    Guid? ProviderId = null,
    Guid? PatientId = null,
    DateOnly? FromDate = null,
    DateOnly? ToDate = null,
    BookingStatus? Status = null,
    int Page = 1,
    int PageSize = 20) : IRequest<PagedBookingListModel>;

internal sealed class ListBookingsQueryHandler(IBookingRepository repository)
    : IRequestHandler<ListBookingsQuery, PagedBookingListModel>
{
    public async Task<PagedBookingListModel> Handle(
        ListBookingsQuery request,
        CancellationToken cancellationToken)
    {
        var page = Math.Max(request.Page, 1);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);
        var result = await repository.ListAsync(
            request.ProviderId,
            request.PatientId,
            request.FromDate,
            request.ToDate,
            request.Status,
            page,
            pageSize,
            cancellationToken);

        return new PagedBookingListModel(
            result.Items.Select(booking => booking.ToModel()).ToArray(),
            page,
            pageSize,
            result.TotalCount,
            result.TotalCount == 0
                ? 0
                : (int)Math.Ceiling(result.TotalCount / (double)pageSize));
    }
}
