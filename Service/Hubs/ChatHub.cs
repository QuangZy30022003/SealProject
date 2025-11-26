using Microsoft.AspNetCore.SignalR;

namespace Seal.Hubs
{
    public class ChatHub : Hub
    {
        // Khi user connect, join vào group của họ (mentor hoặc team)
        public override async Task OnConnectedAsync()
        {
            var userId = GetUserId();
            if (userId.HasValue)
            {
                // Join group cho user này (để nhận message)
                await Groups.AddToGroupAsync(Context.ConnectionId, $"User_{userId.Value}");
            }
            await base.OnConnectedAsync();
        }

        // Khi user disconnect
        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = GetUserId();
            if (userId.HasValue)
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"User_{userId.Value}");
            }
            await base.OnDisconnectedAsync(exception);
        }

        // Join vào chat group cụ thể (khi user mở conversation)
        public async Task JoinChatGroup(int chatGroupId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"ChatGroup_{chatGroupId}");
        }

        // Leave chat group (khi user đóng conversation)
        public async Task LeaveChatGroup(int chatGroupId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"ChatGroup_{chatGroupId}");
        }

        private int? GetUserId()
        {
            var userIdClaim = Context.User?.FindFirst("UserId")?.Value;
            return int.TryParse(userIdClaim, out var userId) ? userId : null;
        }
    }
}
