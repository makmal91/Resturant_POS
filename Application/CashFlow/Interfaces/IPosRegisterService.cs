using POSSystem.Application.CashFlow.DTOs;

namespace POSSystem.Application.CashFlow.Interfaces;

public interface IPosRegisterService
{
    Task<RegisterDashboardDto> GetDashboardAsync(int branchId);
    Task<IReadOnlyList<PosRegisterDto>> GetRegistersAsync(int branchId);
    Task<PosRegisterDto> CreateRegisterAsync(CreatePosRegisterRequest request, int userId);
    Task<PosRegisterDto> UpdateRegisterAsync(int id, UpdatePosRegisterRequest request, int branchId, int userId);
    Task<RegisterOpeningHintDto> GetOpeningHintAsync(int registerId, int branchId);
    Task<RegisterSessionDto> OpenRegisterAsync(OpenRegisterRequest request, int branchId, int userId);
    Task<RegisterClosePreviewDto> GetClosePreviewAsync(int registerId, int branchId);
    Task<RegisterSessionDto> CloseRegisterAsync(CloseRegisterRequest request, int branchId, int userId);
    Task<RegisterHistoryPageDto> GetHistoryAsync(int branchId, RegisterHistoryFilter filter);
}
