using System.ComponentModel.DataAnnotations;

namespace Mastery.Models.Quest;

public class CreateQuestBindingModel
{
    [Required]
    public string Title { get; set; } = default!;
    [Required]
    public string Description { get; set; } = default!;
    [Required]
    public string Objective { get; set; } = default!;
    [ValidateComplexType]
    public List<CreateEventBindingModel> EventBindingModels { get; set; } = new();
}
