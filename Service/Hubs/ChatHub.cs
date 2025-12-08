using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Common.DTOs.ChatDto;
using System.Collections.Concurrent;

namespace Service.Hubs
{
    [Authorize]
    public class ChatHub : Hub
    {
        private static readonly ConcurrentDictionary<int, HashSet<string>> _userConnections = new();
        private static readonly ConcurrentDictionary<string, DateTime> _lastMessageTime = new();
        private const int MESSAGE_RATE_LIMIT_SECONDS = 1; // 1 message per second

        public ChatHub()
        {
            // No dependencies - avoid circular dependency with ChatService
        }

        // ==================== CONNECTION MANAGEMENT ====================
        
        public override async Task OnConnectedAsync()
        {
            try
            {
                var userId = GetUserId();
                var userName = GetUserName();
                
                if (userId.HasValue)
                {
                    // Check if this is first connection for this user
                    bool isFirstConnection = !_userConnections.ContainsKey(userId.Value);

                    // Track user connections
                    _userConnections.AddOrUpdate(
                        userId.Value,
                        new HashSet<string> { Context.ConnectionId },
                        (key, connections) =>
                        {
                            connections.Add(Context.ConnectionId);
                            return connections;
                        }
                    );

                    // Join personal group
                    await Groups.AddToGroupAsync(Context.ConnectionId, $"User_{userId.Value}");

                    // Only broadcast if this is the first connection
                    if (isFirstConnection)
                    {
                        await Clients.All.SendAsync("UserOnline", new
                        {
                            UserId = userId.Value,
                            UserName = userName,
                            Timestamp = DateTime.UtcNow
                        });
                    }
                }
                else
                {
                    Context.Abort(); // Disconnect if no userId
                }
            }
            catch (Exception ex)
            {
                // Log error (inject ILogger if needed)
                Console.WriteLine($"OnConnectedAsync Error: {ex.Message}");
            }

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            try
            {
                var userId = GetUserId();
                var userName = GetUserName();
                
                if (userId.HasValue)
                {
                    // Remove connection
                    if (_userConnections.TryGetValue(userId.Value, out var connections))
                    {
                        connections.Remove(Context.ConnectionId);
                        
                        // If no more connections, user is offline
                        if (connections.Count == 0)
                        {
                            _userConnections.TryRemove(userId.Value, out _);
                            
                            await Clients.All.SendAsync("UserOffline", new
                            {
                                UserId = userId.Value,
                                UserName = userName,
                                Timestamp = DateTime.UtcNow
                            });
                        }
                    }

                    await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"User_{userId.Value}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"OnDisconnectedAsync Error: {ex.Message}");
            }

            await base.OnDisconnectedAsync(exception);
        }

        // ==================== CHAT GROUP MANAGEMENT ====================

        public async Task JoinChatGroup(int chatGroupId)
        {
            try
            {
                var userId = GetUserId();
                if (!userId.HasValue)
                {
                    await Clients.Caller.SendAsync("Error", "Unauthorized");
                    return;
                }

                // TODO: Validate user has access to this chat group
                // var hasAccess = await _chatService.ValidateUserAccessAsync(chatGroupId, userId.Value);
                // if (!hasAccess) return;

                await Groups.AddToGroupAsync(Context.ConnectionId, $"ChatGroup_{chatGroupId}");
                
                await Clients.Caller.SendAsync("JoinedChatGroup", new
                {
                    ChatGroupId = chatGroupId,
                    Timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                await Clients.Caller.SendAsync("Error", ex.Message);
            }
        }

        public async Task LeaveChatGroup(int chatGroupId)
        {
            try
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"ChatGroup_{chatGroupId}");
                
                await Clients.Caller.SendAsync("LeftChatGroup", new
                {
                    ChatGroupId = chatGroupId,
                    Timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                await Clients.Caller.SendAsync("Error", ex.Message);
            }
        }

        // ==================== SEND MESSAGE ====================
        // NOTE: Send message via API endpoint instead to avoid circular dependency
        // Use: POST /api/chat/message
        // This hub method is kept for rate limiting and validation only

        // ==================== TYPING INDICATOR ====================

        public async Task UserTyping(int chatGroupId)
        {
            try
            {
                var userId = GetUserId();
                var userName = GetUserName();
                if (!userId.HasValue) return;

                // Broadcast to others in group (not to self)
                await Clients.OthersInGroup($"ChatGroup_{chatGroupId}")
                    .SendAsync("UserTyping", new
                    {
                        UserId = userId.Value,
                        UserName = userName,
                        ChatGroupId = chatGroupId,
                        Timestamp = DateTime.UtcNow
                    });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"UserTyping Error: {ex.Message}");
            }
        }

        public async Task UserStoppedTyping(int chatGroupId)
        {
            try
            {
                var userId = GetUserId();
                var userName = GetUserName();
                if (!userId.HasValue) return;

                await Clients.OthersInGroup($"ChatGroup_{chatGroupId}")
                    .SendAsync("UserStoppedTyping", new
                    {
                        UserId = userId.Value,
                        UserName = userName,
                        ChatGroupId = chatGroupId,
                        Timestamp = DateTime.UtcNow
                    });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"UserStoppedTyping Error: {ex.Message}");
            }
        }

        // ==================== MESSAGE STATUS ====================

        public async Task MessageDelivered(int messageId, int chatGroupId)
        {
            try
            {
                var userId = GetUserId();
                if (!userId.HasValue) return;

                // Broadcast delivery status
                await Clients.Group($"ChatGroup_{chatGroupId}")
                    .SendAsync("MessageDelivered", new
                    {
                        MessageId = messageId,
                        UserId = userId.Value,
                        Timestamp = DateTime.UtcNow
                    });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"MessageDelivered Error: {ex.Message}");
            }
        }

        // ==================== MARK AS READ ====================
        // NOTE: Use API endpoint instead: PUT /api/chat/group/{chatGroupId}/read
        // This avoids circular dependency with ChatService

        // ==================== ONLINE STATUS ====================

        public async Task GetOnlineUsers(List<int> userIds)
        {
            try
            {
                var onlineUsers = userIds.Where(id => _userConnections.ContainsKey(id)).ToList();
                
                await Clients.Caller.SendAsync("OnlineUsers", new
                {
                    UserIds = onlineUsers,
                    Timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"GetOnlineUsers Error: {ex.Message}");
            }
        }

        public static bool IsUserOnline(int userId)
        {
            return _userConnections.ContainsKey(userId);
        }

        // ==================== HELPER METHODS ====================

        private int? GetUserId()
        {
            var userIdClaim = Context.User?.FindFirst("UserId")?.Value;
            return int.TryParse(userIdClaim, out var userId) ? userId : null;
        }

        private string GetUserName()
        {
            // Get FullName from JWT claims (added in JwtHelper)
            return Context.User?.FindFirst("FullName")?.Value ?? "Unknown User";
        }
    }
}
