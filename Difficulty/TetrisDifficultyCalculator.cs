using System.Collections.Generic;
using osu.Game.Beatmaps;
using osu.Game.Rulesets.Difficulty;
using osu.Game.Rulesets.Difficulty.Preprocessing;
using osu.Game.Rulesets.Difficulty.Skills;
using osu.Game.Rulesets.Mods;

namespace osu.Game.Rulesets.Tetris.Difficulty
{
    public class TetrisDifficultyCalculator : DifficultyCalculator
    {
        public TetrisDifficultyCalculator(IRulesetInfo ruleset, IWorkingBeatmap beatmap)
            : base(ruleset, beatmap)
        {
        }

        protected override DifficultyAttributes CreateDifficultyAttributes(
            IBeatmap beatmap,
            Mod[] mods,
            Skill[] skills)
        {
            return new DifficultyAttributes(mods, 2.0);
        }

        protected override IEnumerable<DifficultyHitObject> CreateDifficultyHitObjects(
            IBeatmap beatmap,
            Mod[] mods)
        {
            return System.Array.Empty<DifficultyHitObject>();
        }

        protected override Skill[] CreateSkills(IBeatmap beatmap, Mod[] mods)
        {
            return System.Array.Empty<Skill>();
        }
    }
}
