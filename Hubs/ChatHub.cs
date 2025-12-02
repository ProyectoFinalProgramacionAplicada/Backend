using Microsoft.AspNetCore.SignalR;

namespace TruekAppAPI.Hubs; 

public class ChatHub : Hub
{
    public async Task JoinTradeGroup(string tradeId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, tradeId);
    }

    public async Task LeaveTradeGroup(string tradeId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, tradeId);
    }
    
    // Nuevo método para avisar que alguien escribe
    public async Task SendTyping(string tradeId, string userName)
    {
        // "OthersInGroup" envía a todos MENOS al que llamó al método.
        // Así tú no ves tu propio aviso de "Escribiendo..."
        await Clients.OthersInGroup(tradeId).SendAsync("UserTyping", userName);
    }
}