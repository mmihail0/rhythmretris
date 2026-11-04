using osu.Game.Rulesets.Judgements;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Tetris.Scoring;

namespace osu.Game.Rulesets.Tetris.Objects
{
    public class TetrisHitObject : HitObject
    {
        public override Judgement CreateJudgement() => new TetrisJudgement();
    }
}
