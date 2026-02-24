using Postgrest.Attributes;
using Postgrest.Models;

namespace Vest.Models;

[Table("user_profiles")]
public class UserProfile : BaseModel
{
    [PrimaryKey("id", false)]
    public string Id { get; set; } = string.Empty;

    [Column("username")]
    public string Username { get; set; } = string.Empty;

    [Column("full_name")]
    public string? FullName { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }
}
