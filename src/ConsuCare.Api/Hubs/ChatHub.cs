using ConsuCare.Api.Data;
using ConsuCare.Shared.Dtos;
using ConsuCare.Shared.Models;
using Microsoft.AspNetCore.SignalR;

namespace ConsuCare.Api.Hubs;

public class ChatHub : Hub
{
    private readonly AppDbContext _db;

    public ChatHub(AppDbContext db) => _db = db;

    public Task Register(int userId)
        => Groups.AddToGroupAsync(Context.ConnectionId, GroupFor(userId));

    public async Task SendMessage(SendMessageRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Content)) return;

        var sender = await _db.Users.FindAsync(request.SenderId);
        if (sender is null) return;

        var message = new ChatMessage
        {
            SenderId = request.SenderId,
            RecipientId = request.RecipientId,
            Content = request.Content.Trim(),
            SentAt = DateTime.UtcNow,
            IsRead = false
        };
        _db.Messages.Add(message);

        _db.Notifications.Add(new Notification
        {
            UserId = request.RecipientId,
            Kind = NotificationKind.NewMessage,
            Title = $"New message from {sender.FullName}",
            Body = $"\"{Truncate(message.Content, 80)}\"",
            CreatedAt = message.SentAt,
            IsRead = false
        });

        await _db.SaveChangesAsync();

        var dto = new ChatMessageDto(message.Id, message.SenderId, message.RecipientId, message.Content, message.SentAt);
        await Clients.Group(GroupFor(request.RecipientId)).SendAsync("ReceiveMessage", dto);
        await Clients.Group(GroupFor(request.SenderId)).SendAsync("ReceiveMessage", dto);
    }

    private static string GroupFor(int userId) => $"user-{userId}";

    private static string Truncate(string s, int max)
        => s.Length <= max ? s : s[..max] + "…";
}
