using osu.Framework.Localisation;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Objects.Drawables;
using osu.Game.Rulesets.Tetris.Objects;
using osu.Game.Rulesets.Tetris.UI;
using osu.Game.Rulesets.UI;

namespace osu.Game.Rulesets.Tetris.Mods
{
    public class TetrisModHardRock : ModHardRock, IApplicableToDrawableRuleset<TetrisHitObject>
    {
        public override double ScoreMultiplier => 1.10;

        public override LocalisableString Description => "Faster gravity, shorter lock delay, fewer next previews, and heavier health damage from buried holes.";

        public void ApplyToDrawableRuleset(DrawableRuleset<TetrisHitObject> drawableRuleset)
        {
            if (drawableRuleset.Playfield is TetrisPlayfield tetrisPlayfield)
                tetrisPlayfield.IsHardRock = true;
        }
    }

    public class TetrisModDoubleTime : ModDoubleTime
    {
    }

    public class TetrisModNightcore : ModNightcore<TetrisHitObject>
    {
    }

    public class TetrisModHalfTime : ModHalfTime
    {
    }

    public class TetrisModDaycore : ModDaycore
    {
    }

    public class TetrisModHidden : ModHidden, IApplicableToDrawableRuleset<TetrisHitObject>
    {
        public override LocalisableString Description => "Play with a fading board where locked blocks disappear.";

        protected override void ApplyIncreasedVisibilityState(DrawableHitObject hitObject, ArmedState state)
        {
        }

        protected override void ApplyNormalVisibilityState(DrawableHitObject hitObject, ArmedState state)
        {
        }

        public void ApplyToDrawableRuleset(DrawableRuleset<TetrisHitObject> drawableRuleset)
        {
            if (drawableRuleset.Playfield is TetrisPlayfield tetrisPlayfield)
                tetrisPlayfield.IsHidden = true;
        }
    }

    public class TetrisModNoFail : ModNoFail
    {
    }

    public class TetrisModSuddenDeath : ModSuddenDeath
    {
    }

    public class TetrisModPerfect : ModPerfect
    {
    }
}
