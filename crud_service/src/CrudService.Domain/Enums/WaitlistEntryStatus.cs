namespace CrudService.Domain.Enums;

using NpgsqlTypes;

public enum WaitlistEntryStatus
{
    [PgName("active")]
    Active,
    [PgName("consumed")]
    Consumed,
    [PgName("expired")]
    Expired
}
