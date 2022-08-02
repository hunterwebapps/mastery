using System.ComponentModel.DataAnnotations;

namespace Mastery.Models.Skills;

public class CreateSkillBindingModel
{
    [Required]
    public string Name { get; set; } = default!;
    [Required]
    public string Description { get; set; } = default!;
    [Required]
    public string ImageUrl { get; set; } = default!;
}
