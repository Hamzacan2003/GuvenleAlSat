using GuvenleAlSat.DataAccess.Entities.Common;
using GuvenleAlSat.DataAccess.Entities.Listings;
using GuvenleAlSat.DataAccess.Entities.Users;

namespace GuvenleAlSat.DataAccess.Entities.Messages;

public class MessageConversation : BaseEntity
{
    public Guid ListingId { get; set; }
    public Listing Listing { get; set; } = null!;

    public Guid BuyerId { get; set; }
    public ApplicationUser Buyer { get; set; } = null!;

    public Guid SellerId { get; set; }
    public ApplicationUser Seller { get; set; } = null!;

    public DateTime LastMessageDate { get; set; } = DateTime.UtcNow;
    public ICollection<ChatMessage> Messages { get; set; } = new List<ChatMessage>();
}

public class ChatMessage : BaseEntity
{
    public Guid ConversationId { get; set; }
    public MessageConversation Conversation { get; set; } = null!;

    public Guid SenderId { get; set; }
    public ApplicationUser Sender { get; set; } = null!;

    public string Content { get; set; } = string.Empty;
    public bool IsRead { get; set; } = false;
    public DateTime? ReadAt { get; set; }
}