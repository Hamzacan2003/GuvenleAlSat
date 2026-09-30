using System.Security.Claims;
using GuvenleAlSat.DataAccess.Concrete.EntityFramework.Contexts;
using GuvenleAlSat.DataAccess.Entities.Messages;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace GuvenleAlSat.API.Hubs;

[Authorize]
public class ChatHub : Hub
{
    private readonly AppDbContext _context;

    public ChatHub(AppDbContext context)
    {
        _context = context;
    }

    public async Task SendMessage(Guid listingId, Guid receiverId, string content)
    {
        var senderIdStr = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(senderIdStr, out var senderId)) return;

        // 1. İlanı doğrula
        var listing = await _context.Listings.FindAsync(listingId);
        if (listing == null) return;

        // 2. Mevcut konuşmayı bul veya yeni oluştur
        var conversation = await _context.MessageConversations
            .FirstOrDefaultAsync(c => c.ListingId == listingId &&
                                      ((c.BuyerId == senderId && c.SellerId == receiverId) ||
                                       (c.BuyerId == receiverId && c.SellerId == senderId)));

        if (conversation == null)
        {
            conversation = new MessageConversation
            {
                ListingId = listingId,
                BuyerId = senderId == listing.UserId ? receiverId : senderId,
                SellerId = listing.UserId,
                LastMessageDate = DateTime.UtcNow
            };
            _context.MessageConversations.Add(conversation);
            await _context.SaveChangesAsync();
        }
        else
        {
            conversation.LastMessageDate = DateTime.UtcNow;
        }

        // 3. Mesajı veritabanına ekle
        var chatMessage = new ChatMessage
        {
            ConversationId = conversation.Id,
            SenderId = senderId,
            Content = content,
            CreatedAt = DateTime.UtcNow,
            IsRead = false
        };
        _context.ChatMessages.Add(chatMessage);
        await _context.SaveChangesAsync();

        // 4. Alıcıya WebSocket ile anlık ilet
        var messagePayload = new
        {
            conversationId = conversation.Id,
            senderId = senderId,
            content = content,
            sentAt = chatMessage.CreatedAt
        };

        // Alıcının User ID'sine özel SignalR grubuna gönder
        await Clients.User(receiverId.ToString()).SendAsync("ReceiveMessage", messagePayload);
        // Gönderenin ekranına da teyit dön
        await Clients.Caller.SendAsync("MessageSent", messagePayload);
    }
}