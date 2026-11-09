using System;
using osu.Game.Rulesets.Judgements;
using osu.Game.Rulesets.Scoring;

namespace osu.Game.Rulesets.Tetris.Scoring
{
    public class TetrisJudgement : Judgement
    {
        public override HitResult MaxResult => HitResult.Great;
        public override HitResult MinResult => HitResult.Miss;

        protected override double HealthIncreaseFor(HitResult result) => 0;
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

        public bool HealthDepleted { get; private set; }

        public TetrisHealthProcessor()
        {
        }

        public void ApplyHoleDamage(double damageAmount)
        {
            if (damageAmount <= 0)
                return;

            Health.Value = Math.Clamp(Health.Value - damageAmount, 0.0, 1.0);
            if (Health.Value <= 0.0)
                HealthDepleted = true;
        }

        public void ApplyPlacementHeal(double healAmount)
        {
            if (healAmount <= 0 || TopOutTriggered || HealthDepleted)
                return;

            Health.Value = Math.Clamp(Health.Value + healAmount, 0.0, 1.0);
        }

        public void TriggerTopOut()
        {
            TopOutTriggered = true;
            Health.Value = 0;
        }

        protected override bool CheckDefaultFailCondition(JudgementResult result)
            => TopOutTriggered || HealthDepleted || Health.Value <= 0;
    }
}
