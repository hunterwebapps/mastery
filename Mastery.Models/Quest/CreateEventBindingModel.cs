using System.ComponentModel.DataAnnotations;

namespace Mastery.Models.Quest;

public class CreateEventBindingModel
{
    [Required]
    public string EventTypeId { get; set; } = default!;
    [Required]
    public string Description { get; set; } = default!;
    [ValidateComplexType]
    public List<CreateDecisionBindingModel> DecisionBindingModels { get; set; } = new();
}
