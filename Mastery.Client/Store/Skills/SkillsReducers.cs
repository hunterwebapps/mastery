using Fluxor;
using Mastery.Client.Store.Skills.Actions;

namespace Mastery.Client.Store.Skills;

public class SkillsReducers
{
    [ReducerMethod]
    public static SkillsState SetSkills(SkillsState state, FetchSkillsAction fetchSkillsAction)
        => new(fetchSkillsAction.Skills);
}
