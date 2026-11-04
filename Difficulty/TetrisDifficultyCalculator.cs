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
        public override int Version => 2;

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

            // Scale off both BPM and note density so different songs AND different difficulties (Normal/Hard/Insane/Extra) have distinct star ratings
            double densityBonus = 0.0;
            if (beatmap.HitObjects.Count > 1)
            {
                double durationSeconds = Math.Max((beatmap.HitObjects[^1].StartTime - beatmap.HitObjects[0].StartTime) / 1000.0, 10.0) / clockRate;
                double notesPerSecond = beatmap.HitObjects.Count / durationSeconds;
                densityBonus = notesPerSecond * 0.45;
            }

            double starRating = (bpm / 65.0) + densityBonus;

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
