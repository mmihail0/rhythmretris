using osu.Game.Rulesets.Objects.Drawables;
using osu.Game.Rulesets.Scoring;
using osu.Game.Rulesets.Tetris.Objects;

namespace osu.Game.Rulesets.Tetris.Objects.Drawables
{
    public partial class DrawableTetrisHitObject : DrawableHitObject<TetrisHitObject>
    {
        public DrawableTetrisHitObject(TetrisHitObject hitObject)
            : base(hitObject)
        {
        }

        protected override void CheckForResult(bool userTriggered, double timeOffset)
        {
            if (!userTriggered)
            {
                if (timeOffset > 150)
                    ApplyResult(HitResult.Miss);
                return;
            }

            double absOffset = System.Math.Abs(timeOffset);

            if (absOffset <= 50)
                ApplyResult(HitResult.Great);
            else if (absOffset <= 100)
                ApplyResult(HitResult.Ok);
            else if (absOffset <= 150)
                ApplyResult(HitResult.Meh);
            else
                ApplyResult(HitResult.Miss);
        }

        public void TriggerUserHit() => UpdateResult(true);
    }
}
