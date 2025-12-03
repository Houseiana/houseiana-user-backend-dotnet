namespace HouseianaApi.Enums;

public enum BookingStatus
{
    PENDING,
    AWAITING_PAYMENT,
    CONFIRMED,
    CANCELLED,
    COMPLETED,
    REJECTED,
    REQUESTED,
    APPROVED,
    EXPIRED,
    CHECKED_IN
}

public enum PaymentStatus
{
    PENDING,
    PAID,
    FAILED,
    REFUNDED,
    PARTIALLY_REFUNDED
}

public enum CalendarLockStatus
{
    NONE,
    SOFT_HOLD,
    CONFIRMED
}

public enum PropertyStatus
{
    DRAFT,
    PENDING,
    PENDING_REVIEW,
    ACTIVE,
    PUBLISHED,
    UNLISTED,
    SUSPENDED,
    REJECTED
}

public enum UserStatus
{
    ACTIVE,
    SUSPENDED,
    BANNED,
    DEACTIVATED
}

public enum KycStatus
{
    PENDING,
    IN_REVIEW,
    APPROVED,
    REJECTED,
    REQUIRES_UPDATE
}

public enum OwnerType
{
    INDIVIDUAL,
    ORGANIZATION
}

public enum PropertyType
{
    HOUSE,
    APARTMENT,
    VILLA,
    CONDO,
    TOWNHOUSE,
    GUESTHOUSE,
    HOTEL,
    CABIN,
    BUNGALOW,
    STUDIO,
    LOFT,
    CASA,
    OTHER
}

public enum RoomType
{
    ENTIRE_PLACE,
    PRIVATE_ROOM,
    SHARED_ROOM
}
