using System;
using System.Collections.Generic;
using System.Linq;
using osu.Game.Beatmaps;
using osu.Game.Beatmaps.ControlPoints;
using osu.Game.Rulesets.Difficulty;
using osu.Game.Rulesets.Difficulty.Preprocessing;
using osu.Game.Rulesets.Difficulty.Skills;
using osu.Game.Rulesets.Mods;

namespace osu.Game.Rulesets.Tetris.Difficulty
{
    public class TetrisDifficultyCalculator : DifficultyCalculator
    {
        public override int Version => 7;

        public TetrisDifficultyCalculator(IRulesetInfo ruleset, IWorkingBeatmap beatmap)
            : base(ruleset, beatmap)
        {
        }

        protected override DifficultyAttributes CreateDifficultyAttributes(
            IBeatmap beatmap,
            Mod[] mods,
            Skill[] skills)
        {
            double clockRate = 1.0;
            foreach (var mod in mods.OfType<IApplicableToRate>())
                clockRate = mod.ApplyToRate(0, clockRate);

            double beatLength = beatmap.ControlPointInfo.TimingPoints.FirstOrDefault()?.BeatLength
                                ?? TimingControlPoint.DEFAULT.BeatLength;

            if (beatmap.ControlPointInfo.TimingPoints.Count > 1)
            {
                double avgBeatLength = beatmap.ControlPointInfo.TimingPoints.Average(t => Math.Clamp(t.BeatLength, 60, 2000));
                double minBeatLength = beatmap.ControlPointInfo.TimingPoints.Min(t => Math.Clamp(t.BeatLength, 60, 2000));
                beatLength = avgBeatLength * 0.7 + minBeatLength * 0.3;
            }

            double bpm = (60000.0 / Math.Max(beatLength, 1.0)) * clockRate;

            double npsScale = 1.0;
            if (beatmap.HitObjects.Count > 1)
            {
                double durationSeconds = Math.Max((beatmap.HitObjects[^1].StartTime - beatmap.HitObjects[0].StartTime) / 1000.0, 10.0) / clockRate;
                double nps = beatmap.HitObjects.Count / durationSeconds;
                npsScale = 1.0 + Math.Clamp(nps * 0.12, 0.05, 1.5);
            }

            double effectiveStepsPerMinute = bpm * 2.0 * npsScale;
            double starRating = effectiveStepsPerMinute / 90.0;

            if (mods.Any(m => m is ModHardRock))
                starRating *= 1.25;

            if (mods.Any(m => m is ModHidden))
                starRating *= 1.15;

            return new DifficultyAttributes(mods, starRating)
            {
                MaxCombo = beatmap.HitObjects.Count
            };
        }

        protected override IEnumerable<DifficultyHitObject> CreateDifficultyHitObjects(
            IBeatmap beatmap,
            Mod[] mods)
        {
            return Array.Empty<DifficultyHitObject>();
        }

        protected override Skill[] CreateSkills(IBeatmap beatmap, Mod[] mods)
        {
            return Array.Empty<Skill>();
        }
    }
}
