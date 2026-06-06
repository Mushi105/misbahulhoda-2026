using MediatR;
using Misbahuda.Application.Common;
using Misbahuda.Application.DTOs.Pilgrim;
using Misbahuda.Application.Interfaces;
using Misbahuda.Domain.Entities;
using Misbahuda.Domain.Interfaces;

namespace Misbahuda.Application.Features.Pilgrims.Commands;

public record CreatePilgrimProfileCommand(
    string PassportNumber,
    string? VisaNumber,
    string Country,
    int FamilyMemberCount,
    DateTime ArrivalDate,
    DateTime DepartureDate,
    string? ArrivalFlight,
    string? DepartureFlight,
    string? EmergencyContactName,
    string? EmergencyContactPhone
) : IRequest<ApiResponse<PilgrimDto>>;

public class CreatePilgrimProfileCommandHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUser)
    : IRequestHandler<CreatePilgrimProfileCommand, ApiResponse<PilgrimDto>>
{
    public async Task<ApiResponse<PilgrimDto>> Handle(CreatePilgrimProfileCommand request, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null)
            return ApiResponse<PilgrimDto>.Fail("Unauthorized.");

        var pilgrim = (await unitOfWork.Pilgrims.FindAsync(p => p.UserId == currentUser.UserId, cancellationToken)).FirstOrDefault();

        if (pilgrim is null)
        {
            pilgrim = new Pilgrim { UserId = currentUser.UserId.Value };
            await unitOfWork.Pilgrims.AddAsync(pilgrim, cancellationToken);
        }

        pilgrim.PassportNumber = request.PassportNumber;
        pilgrim.VisaNumber = request.VisaNumber;
        pilgrim.Country = request.Country;
        pilgrim.FamilyMemberCount = request.FamilyMemberCount;
        pilgrim.ArrivalDate = DateTime.SpecifyKind(request.ArrivalDate, DateTimeKind.Utc);
        pilgrim.DepartureDate = DateTime.SpecifyKind(request.DepartureDate, DateTimeKind.Utc);
        pilgrim.ArrivalFlight = request.ArrivalFlight;
        pilgrim.DepartureFlight = request.DepartureFlight;
        pilgrim.EmergencyContactName = request.EmergencyContactName;
        pilgrim.EmergencyContactPhone = request.EmergencyContactPhone;

        await unitOfWork.SaveChangesAsync(cancellationToken);

        var user = await unitOfWork.Users.GetByIdAsync(currentUser.UserId.Value, cancellationToken);

        return ApiResponse<PilgrimDto>.Ok(new PilgrimDto(
            pilgrim.Id,
            user!.FullName,
            user.Email,
            user.PhoneNumber,
            pilgrim.PassportNumber,
            pilgrim.VisaNumber,
            pilgrim.Country,
            pilgrim.FamilyMemberCount,
            pilgrim.ArrivalDate,
            pilgrim.DepartureDate,
            pilgrim.Status,
            null, null, null
        ), "Pilgrim profile created successfully.");
    }
}
