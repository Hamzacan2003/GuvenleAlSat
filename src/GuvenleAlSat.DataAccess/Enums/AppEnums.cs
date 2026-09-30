using System;
using System.Collections.Generic;
using System.Text;

namespace GuvenleAlSat.DataAccess.Enums;

public enum UserType
{
    Individual = 1,
    Corporate = 2
}

public enum ListingStatus
{
    Draft = 0,
    PendingApproval = 1,
    Active = 2,
    Sold = 3,
    Passive = 4,
    Expired = 5
}

public enum CurrencyType
{
    TRY = 1,
    USD = 2,
    EUR = 3
}

public enum PaintCondition
{
    Original = 1,
    Painted = 2,
    LocallyPainted = 3,
    Replaced = 4
}