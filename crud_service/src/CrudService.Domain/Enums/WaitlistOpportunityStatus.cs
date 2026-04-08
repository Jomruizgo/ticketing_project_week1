namespace CrudService.Domain.Enums;

using NpgsqlTypes;

public enum WaitlistOpportunityStatus
{
    [PgName("pending")]
    Pending,
    [PgName("active")]
    Active,
    [PgName("consumed")]
    Consumed,
    [PgName("expired")]
    Expired,
    [PgName("failed")]
    Failed
}
