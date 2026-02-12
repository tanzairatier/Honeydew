namespace Honeydew.Entities;

public class TodoItemVote
{
    public Guid TodoItemId { get; set; }
    public Guid UserId { get; set; }

    public TodoItem TodoItem { get; set; } = null!;
    public User User { get; set; } = null!;
}
