namespace Honeydew.Entities;

public class UserPreference
{
    public Guid UserId { get; set; }
    public int ItemsPerPage { get; set; } = 9;

    public User User { get; set; } = null!;
}
