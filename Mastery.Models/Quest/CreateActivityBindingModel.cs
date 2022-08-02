using System.ComponentModel.DataAnnotations;

namespace Mastery.Models.Quest;

public class CreateActivityBindingModel
{
    [Required]
    public int ActivityTypeId { get; set; } = default!;
    public int? NextEventIndex { get; set; }
}
