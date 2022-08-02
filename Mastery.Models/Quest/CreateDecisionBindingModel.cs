using System.ComponentModel.DataAnnotations;

namespace Mastery.Models.Quest;

public class CreateDecisionBindingModel
{
    [Required]
    public string Description { get; set; } = default!;
    [Required]
    public int SortOrder { get; set; }
    [ValidateComplexType]
    public List<CreateActivityBindingModel> ActivityBindingModels { get; set; } = new();
}
