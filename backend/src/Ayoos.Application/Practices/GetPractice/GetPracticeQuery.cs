using MediatR;

namespace Ayoos.Application.Practices.GetPractice;

public sealed record GetPracticeQuery(string Slug) : IRequest<PracticeModel?>;
