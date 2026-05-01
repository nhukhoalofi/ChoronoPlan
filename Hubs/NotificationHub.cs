using Microsoft.AspNetCore.SignalR;

namespace ChronoPlan.Hubs;

public class NotificationHub : Hub
{
    public Task JoinUserGroup(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Task.CompletedTask;
        }

        return Groups.AddToGroupAsync(Context.ConnectionId, GetGroupName(userId));
    }

    public static string GetGroupName(string userId) => $"user:{userId}";
}
