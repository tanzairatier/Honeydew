using System.ComponentModel.DataAnnotations;

namespace Honeydew.Controllers.Models;

public class UpdateTodoRequest
{
    [MaxLength(500)]
    public string? Title { get; set; }

    [MaxLength(2000)]
    public string? Notes { get; set; }

    public Guid? AssignedToUserId { get; set; }
    public DateTime? DueDate { get; set; }
    public bool? IsDone { get; set; }
    public DateTime? CompletedAt { get; set; }
}
