using Afterburn.Core;
using NUnit.Framework;
using UnityEngine;

namespace Afterburn.Tests
{
    /// <summary>
    /// U2 track queries: windowed nearest coherence (incl. the wrap seam), shortcut allowances,
    /// and the HeavyWall break/reset lifecycle (ruling #3).
    /// </summary>
    [TestFixture]
    public sealed class TrackSystemTests
    {
        private static readonly float[] Arena01Shape =
            { 1.00f, 0.94f, 0.78f, 0.74f, 0.92f, 1.06f, 1.10f, 0.86f, 0.72f, 0.82f, 1.02f, 1.08f, 0.92f, 0.84f };

        private static TrackSystem MakeTrack()
        {
            var def = ScriptableObject.CreateInstance<TrackDefinition>();
            def.controlPoints = TrackSampler.GenerateStarConvexPoints(300f, Arena01Shape);
            def.catmullTension = 0.5f;
            def.sampleCount = 700;
            def.halfWidth = 17f;
            def.shortcuts = new[]
            {
                new ShortcutZone { access = GateAccess.LightGap, fromFraction = 0.20f, toFraction = 0.24f, extraInnerAllowance = 20f, side = 1 },
                new ShortcutZone { access = GateAccess.HeavyWall, fromFraction = 0.61f, toFraction = 0.66f, extraInnerAllowance = 22f, side = 1 },
            };
            return new TrackSystem(def);
        }

        [Test]
        public void WindowedNearest_MatchesGlobal_AlongTheWholeLoop()
        {
            TrackSystem track = MakeTrack();
            int last = 0;
            for (int i = 0; i < 700; i += 3)
            {
                Vector3 probe = track[i].Pos + track[i].Nrm * 4f;
                int windowed = track.Nearest(probe, last);
                int global = track.NearestGlobal(probe);
                Assert.That(windowed, Is.EqualTo(global), $"windowed diverged from global at sample {i}");
                last = windowed;
            }
        }

        [Test]
        public void WindowedNearest_SurvivesTheWrapSeam()
        {
            TrackSystem track = MakeTrack();
            // Walk 690 → 699 → 0 → 10 with coherent last-index.
            int last = 690;
            for (int i = 690; i < 700 + 10; i++)
            {
                int idx = i % 700;
                int found = track.Nearest(track[idx].Pos, last);
                Assert.That(found, Is.EqualTo(idx), $"wrap seam broke at {idx}");
                last = found;
            }
        }

        [Test]
        public void WindowEscape_FallsBackToGlobalRescan()
        {
            TrackSystem track = MakeTrack();
            // Teleport-sized displacement: probe near sample 350 with lastIndex 0.
            Vector3 probe = track[350].Pos;
            int found = track.Nearest(probe, 0);
            Assert.That(found, Is.EqualTo(track.NearestGlobal(probe)),
                "a window-escape displacement must trigger the global rescan");
        }

        [Test]
        public void InnerAllow_GatesByHullAccess()
        {
            TrackSystem track = MakeTrack();
            int lightMid = (140 + 168) / 2;                     // light zone 0.20–0.24
            int heavyMid = (427 + 462) / 2;                     // heavy zone 0.61–0.66

            Assert.That(track.InnerAllow(lightMid, GateAccess.LightGap), Is.EqualTo(20f));
            Assert.That(track.InnerAllow(lightMid, GateAccess.None), Is.EqualTo(0f));
            Assert.That(track.InnerAllow(lightMid, GateAccess.HeavyWall), Is.EqualTo(0f));

            Assert.That(track.InnerAllow(heavyMid, GateAccess.HeavyWall), Is.EqualTo(22f));
            Assert.That(track.InnerAllow(heavyMid, GateAccess.None), Is.EqualTo(0f),
                "unbroken heavy wall must not open for Medium");
            Assert.That(track.InnerAllow(300, GateAccess.LightGap), Is.EqualTo(0f),
                "outside every zone the allowance is 0");
        }

        [Test]
        public void HeavyBreak_OpensForAll_AndResetsPerRace()
        {
            TrackSystem track = MakeTrack();
            int heavyMid = (427 + 462) / 2;

            Assert.That(track.TryBreakHeavy(heavyMid), Is.True, "first press breaks the wall");
            Assert.That(track.TryBreakHeavy(heavyMid), Is.False, "already broken — no second event");
            Assert.That(track.InnerAllow(heavyMid, GateAccess.None), Is.EqualTo(22f),
                "within the race, the broken wall opens for every hull (prototype canon)");

            track.ResetRaceState();                              // ruling #3
            Assert.That(track.InnerAllow(heavyMid, GateAccess.None), Is.EqualTo(0f),
                "race start must reset the break state — no leakage across 'one more run'");
        }

        [Test]
        public void BreakHeavy_IgnoresIndicesOutsideTheZone()
        {
            TrackSystem track = MakeTrack();
            Assert.That(track.TryBreakHeavy(100), Is.False);
            Assert.That(track.IsZoneBroken(1), Is.False);
        }
    }
}
