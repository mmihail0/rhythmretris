using osu.Game.Rulesets.Judgements;
using osu.Game.Rulesets.Scoring;

namespace osu.Game.Rulesets.Tetris.Scoring
{
    public class TetrisJudgement : Judgement
    {
        public override HitResult MaxResult => HitResult.Great;
        public override HitResult MinResult => HitResult.IgnoreMiss;
    }

    public partial class TetrisScoreProcessor : ScoreProcessor
    {
        public TetrisScoreProcessor(TetrisRuleset ruleset)
            : base(ruleset)
        {
        }
    }
}
