using osu.Game.Rulesets.Judgements;
using osu.Game.Rulesets.Scoring;

namespace osu.Game.Rulesets.Tetris.Scoring
{
    public class TetrisJudgement : Judgement
    {
        public override HitResult MaxResult => HitResult.Great;
        public override HitResult MinResult => HitResult.Miss;

        protected override double HealthIncreaseFor(HitResult result) => result switch
        {
            HitResult.Great => DEFAULT_MAX_HEALTH_INCREASE,
            HitResult.Ok => DEFAULT_MAX_HEALTH_INCREASE * 0.5,
            HitResult.Meh => DEFAULT_MAX_HEALTH_INCREASE * 0.1,
            _ => 0
        };
    }

    public partial class TetrisScoreProcessor : ScoreProcessor
    {
        public TetrisScoreProcessor(TetrisRuleset ruleset)
            : base(ruleset)
        {
        }
    }

    public partial class TetrisHealthProcessor : HealthProcessor
    {
        public bool TopOutTriggered { get; private set; }

        public TetrisHealthProcessor()
        {
        }

        public void TriggerTopOut()
        {
            TopOutTriggered = true;
            Health.Value = 0;
        }

        protected override bool CheckDefaultFailCondition(JudgementResult result)
            => TopOutTriggered;
    }
}
