using Fluxor;
using Mastery.Models.Skills;

namespace Mastery.Client.Store.Skills;

[FeatureState]
public class SkillsState
{
    public SkillsState() { }

    public SkillsState(IEnumerable<SkillViewModel>? skills)
    {
        this.Skills = skills;
    }

    public IEnumerable<SkillViewModel>? Skills { get; set; }
}
