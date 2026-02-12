using System.ComponentModel.DataAnnotations;

namespace Honeydew.Controllers.Models;

public class CreateTodoRequest
{
    [Required, MinLength(1), MaxLength(500)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string? Notes { get; set; }

    public Guid AssignedToUserId { get; set; }

    public DateTime? DueDate { get; set; }
}
