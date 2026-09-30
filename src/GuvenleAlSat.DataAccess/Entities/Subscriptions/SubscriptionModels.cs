using GuvenleAlSat.DataAccess.Entities.Common;
using GuvenleAlSat.DataAccess.Entities.Users;
using GuvenleAlSat.DataAccess.Enums;

namespace GuvenleAlSat.DataAccess.Entities.Subscriptions;

public class SubscriptionPlan : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public UserType TargetUserType { get; set; }
    public decimal MonthlyPrice { get; set; }
    public int MaxActiveListingCount { get; set; }
    public int MaxPhotosPerListing { get; set; }
    public bool CanUploadVideo { get; set; } = false;
    public bool HasStorefront { get; set; } = false;
    public bool HasShowcasePriority { get; set; } = false;
    public bool IsActive { get; set; } = true;
}

public class UserSubscription : BaseEntity
{
    public Guid UserId { get; set; }
    public ApplicationUser User { get; set; } = null!;

    public Guid PlanId { get; set; }
    public SubscriptionPlan Plan { get; set; } = null!;

    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool IsActive { get; set; } = true;
}