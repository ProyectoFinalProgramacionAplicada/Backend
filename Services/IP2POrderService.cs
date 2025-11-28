using System.Collections.Generic;
using System.Threading.Tasks;
using TruekAppAPI.DTO.P2P;

namespace TruekAppAPI.Services;

public interface IP2POrderService
{
    Task<P2POrderDto> CreateAsync(int creatorUserId, P2POrderCreateDto dto);
    Task<IEnumerable<P2POrderDto>> GetOrderBookAsync();
    Task<P2POrderDto> TakeAsync(int orderId, int takerUserId);
    Task<P2POrderDto> MarkPaidAsync(int orderId, int userId);
    Task<P2POrderDto> ReleaseAsync(int orderId, int userId);
    Task<P2POrderDto> CancelAsync(int orderId, int userId);
    Task<P2POrderDto?> GetByIdAsync(int orderId);
    
    Task<IEnumerable<P2POrderDto>> GetOrdersForUserAsync(int userId);


}