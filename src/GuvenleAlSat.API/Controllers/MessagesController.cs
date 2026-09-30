using System.Security.Claims;
using GuvenleAlSat.DataAccess.Concrete.EntityFramework.Contexts;
using GuvenleAlSat.DataAccess.Entities.Messages;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GuvenleAlSat.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class MessagesController : ControllerBase
{
    private readonly AppDbContext _context;

    public MessagesController(AppDbContext context)
    {
        _context = context;
    }

    [HttpPost]
    public async Task<IActionResult> SendMessage([FromBody] SendChatMessageDto dto)
    {
        var currentUserIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(currentUserIdStr, out var senderId))
            return Unauthorized(new { success = false, message = "Oturum açılmamış." });

        if (senderId == dto.ReceiverId)
            return BadRequest(new { success = false, message = "Kendinize mesaj gönderemezsiniz." });

        // İlan kontrolü
        var listing = await _context.Listings.AsNoTracking().FirstOrDefaultAsync(l => l.Id == dto.ListingId);
        if (listing == null)
            return NotFound(new { success = false, message = "İlan bulunamadı." });

        // Alıcı ve satıcıyı belirle (İlan sahibi daima Seller)
        Guid sellerId = listing.UserId;
        Guid buyerId = senderId == sellerId ? dto.ReceiverId : senderId;

        // Mevcut konuşmayı (conversation) bul veya yeni oluştur
        var conversation = await _context.MessageConversations
            .FirstOrDefaultAsync(c => c.ListingId == dto.ListingId && c.BuyerId == buyerId && c.SellerId == sellerId);

        if (conversation == null)
        {
            conversation = new MessageConversation
            {
                Id = Guid.NewGuid(),
                ListingId = dto.ListingId,
                BuyerId = buyerId,
                SellerId = sellerId,
                LastMessageDate = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                IsDeleted = false
            };
            await _context.MessageConversations.AddAsync(conversation);
        }
        else
        {
            conversation.LastMessageDate = DateTime.UtcNow;
            conversation.UpdatedAt = DateTime.UtcNow;
        }

        // Mesajı ekle
        var chatMessage = new ChatMessage
        {
            Id = Guid.NewGuid(),
            ConversationId = conversation.Id,
            SenderId = senderId,
            Content = dto.Content.Trim(),
            IsRead = false,
            CreatedAt = DateTime.UtcNow,
            IsDeleted = false
        };

        await _context.ChatMessages.AddAsync(chatMessage);
        await _context.SaveChangesAsync();

        return Ok(new { success = true, data = chatMessage, message = "Mesaj iletildi." });
    }
    [HttpGet("conversations")]
    public async Task<IActionResult> GetMyConversations()
    {
        var currentUserIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(currentUserIdStr, out var currentUserId))
            return Unauthorized(new { success = false, message = "Oturum açılmamış." });

        var conversations = await _context.MessageConversations
            .AsNoTracking()
            .Include(c => c.Listing)
            .Include(c => c.Buyer)
            .Include(c => c.Seller)
            .Include(c => c.Messages)
            .Where(c => !c.IsDeleted && (c.BuyerId == currentUserId || c.SellerId == currentUserId))
            .OrderByDescending(c => c.LastMessageDate)
            .Select(c => new
            {
                c.Id,
                c.ListingId,
                ListingTitle = c.Listing.Title,
                ListingNo = c.Listing.ListingNo,
                OtherUserId = c.BuyerId == currentUserId ? c.SellerId : c.BuyerId,
                OtherUserName = c.BuyerId == currentUserId
                    ? $"{c.Seller.FirstName} {c.Seller.LastName}"
                    : $"{c.Buyer.FirstName} {c.Buyer.LastName}",
                LastMessage = c.Messages.OrderByDescending(m => m.CreatedAt).Select(m => m.Content).FirstOrDefault(),
                LastMessageDate = c.LastMessageDate,
                UnreadCount = c.Messages.Count(m => !m.IsRead && m.SenderId != currentUserId)
            })
            .ToListAsync();

        return Ok(new { success = true, data = conversations });
    }
    [HttpPost("mark-read")]
    public async Task<IActionResult> MarkAsRead([FromBody] MarkReadRequestDto dto)
    {
        var currentUserIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(currentUserIdStr, out var currentUserId))
            return Unauthorized(new { success = false, message = "Oturum açılmamış." });

        // Kullanıcının alıcı olduğu ve henüz okunmamış mesajları bul
        var unreadMessages = await _context.ChatMessages
            .Where(m => m.ConversationId == dto.ConversationId && m.SenderId != currentUserId && !m.IsRead)
            .ToListAsync();

        if (unreadMessages.Any())
        {
            foreach (var msg in unreadMessages)
            {
                msg.IsRead = true;
                msg.ReadAt = DateTime.UtcNow;
            }
            await _context.SaveChangesAsync();
        }

        return Ok(new { success = true });
    }

    public class MarkReadRequestDto
    {
        public Guid ConversationId { get; set; }
    }

    [HttpGet("unread-count")]
    public async Task<IActionResult> GetUnreadCount()
    {
        var currentUserIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(currentUserIdStr, out var currentUserId))
            return Unauthorized(new { success = false, message = "Oturum açılmamış." });

        var count = await _context.ChatMessages
            .CountAsync(m => !m.IsRead && m.SenderId != currentUserId &&
                             _context.MessageConversations.Any(c => c.Id == m.ConversationId && (c.BuyerId == currentUserId || c.SellerId == currentUserId)));

        return Ok(new { success = true, count });
    }
    [HttpGet("conversation")]
    public async Task<IActionResult> GetConversation([FromQuery] Guid otherUserId, [FromQuery] Guid listingId)
    {
        var currentUserIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(currentUserIdStr, out var currentUserId))
            return Unauthorized(new { success = false, message = "Oturum açılmamış." });

        var conversation = await _context.MessageConversations
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.ListingId == listingId &&
                                      ((c.BuyerId == currentUserId && c.SellerId == otherUserId) ||
                                       (c.SellerId == currentUserId && c.BuyerId == otherUserId)));

        if (conversation == null)
            return Ok(new { success = true, data = new List<object>() });

        var messages = await _context.ChatMessages
            .AsNoTracking()
            .Where(m => m.ConversationId == conversation.Id && !m.IsDeleted)
            .OrderBy(m => m.CreatedAt)
            .Select(m => new
            {
                m.Id,
                m.ConversationId,
                m.SenderId,
                m.Content,
                SentAt = m.CreatedAt,
                m.IsRead
            })
            .ToListAsync();

        return Ok(new { success = true, data = messages });
    }
}

public class SendChatMessageDto
{
    public Guid ReceiverId { get; set; }
    public Guid ListingId { get; set; }
    public string Content { get; set; } = string.Empty;
}