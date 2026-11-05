using osu.Game.Rulesets.Judgements;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Objects.Types;
using osu.Game.Rulesets.Tetris.Scoring;

namespace osu.Game.Rulesets.Tetris.Objects
{
    public class TetrisHitObject : HitObject, IHasDuration
    {
        public double EndTime => StartTime + Duration;

        public double Duration { get; set; }

        public override Judgement CreateJudgement() => new TetrisJudgement();
    }
}
