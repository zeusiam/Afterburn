using Afterburn.Core;
using NUnit.Framework;
using UnityEngine;

namespace Afterburn.Tests
{
    /// <summary>
    /// BUILD §11 U4: the recorded-ghost format — pack/unpack fidelity, binary round-trip
    /// (incl. the cosmetic header, the economy audit's blocking requirement), and REPLAY
    /// DETERMINISM: a recording replayed through the fixed-tick Core reproduces the original
    /// run exactly. This is the property every stored ghost and every leaderboard
    /// verification depends on.
    /// </summary>
    [TestFixture]
    public sealed class GhostRecordingTests
    {
        private const float Dt = ShipController.Tick;

        private static readonly float[] Arena01Shape =
            { 1.00f, 0.94f, 0.78f, 0.74f, 0.92f, 1.06f, 1.10f, 0.86f, 0.72f, 0.82f, 1.02f, 1.08f, 0.92f, 0.84f };

        private static TrackDefinition MakeArena01()
        {
            var track = ScriptableObject.CreateInstance<TrackDefinition>();
            track.controlPoints = TrackSampler.GenerateStarConvexPoints(300f, Arena01Shape);
            track.catmullTension = 0.5f;
            track.sampleCount = 700;
            track.halfWidth = 17f;
            track.shortcuts = System.Array.Empty<ShortcutZone>();
            return track;
        }

        private static HullDefinition MakeMedium()
        {
            var hull = ScriptableObject.CreateInstance<HullDefinition>();
            hull.maxEnergy = 100f;
            hull.regenPerSec = 8f;
            hull.topSpeedMult = 1f;
            hull.collisionRadius = 2f;
            return hull;
        }

        /// <summary>The parity schedule reused — varied, deterministic inputs.</summary>
        private static ShipInputState InputAt(int f)
        {
            int second = f / 60;
            bool odd = (second & 1) == 1;
            return new ShipInputState
            {
                Thrust = f < 1200 && !(f >= 480 && f < 540),
                Left = (f >= 180 && f < 300) || (f >= 540 && f < 900 && odd),
                Right = (f >= 390 && f < 480) || (f >= 540 && f < 900 && !odd),
                Boost = (f >= 300 && f < 390) || (f >= 900 && f < 1080),
                Fire = f >= 390 && f < 480,
                Shield = (f >= 480 && f < 540) || (f >= 1080 && f < 1140),
                Brake = f >= 480 && f < 540,
            };
        }

        [Test]
        public void PackUnpack_RoundTripsAll256Combinations()
        {
            for (int b = 0; b < 256; b++)
            {
                ShipInputState state = GhostRecording.Unpack((byte)b);
                Assert.That(GhostRecording.Pack(state), Is.EqualTo((byte)b));
            }
        }

        [Test]
        public void Serialize_RoundTripsHeaderAndStream()
        {
            var recorder = new GhostRecorder("Arena01", "Medium", "Vex");
            for (int f = 0; f < 600; f++) recorder.Capture(InputAt(f));
            recorder.NoteWallBreak();

            GhostRecording original = recorder.Complete(87.3f);
            original.LiveryId = "livery.reactive.ember";
            original.TrailId = "trail.plasma";
            original.PlateId = "plate.founder";

            GhostRecording restored = GhostRecording.Deserialize(original.Serialize());

            Assert.That(restored.TrackId, Is.EqualTo("Arena01"));
            Assert.That(restored.HullId, Is.EqualTo("Medium"));
            Assert.That(restored.PilotId, Is.EqualTo("Vex"));
            Assert.That(restored.LiveryId, Is.EqualTo("livery.reactive.ember"), "cosmetic header survives — the status billboard");
            Assert.That(restored.TrailId, Is.EqualTo("trail.plasma"));
            Assert.That(restored.PlateId, Is.EqualTo("plate.founder"));
            Assert.That(restored.WallBreakTick, Is.EqualTo(600), "wall-break tick serialized");
            Assert.That(restored.FinishTime, Is.EqualTo(87.3f));
            Assert.That(restored.InputStream, Is.EqualTo(original.InputStream));
        }

        [Test]
        public void Replay_ReproducesTheRunExactly()
        {
            TrackDefinition trackDef = MakeArena01();
            HullDefinition hull = MakeMedium();
            var tuning = ScriptableObject.CreateInstance<GameTuning>();

            // Original run: record 30 s of the schedule while simulating.
            var liveTrack = new TrackSystem(trackDef);
            var liveShip = new ShipController(liveTrack, hull, tuning);
            var recorder = new GhostRecorder("Arena01", "Medium", "Vex");
            for (int f = 0; f < 1800; f++)
            {
                ShipInputState input = InputAt(f);
                recorder.Capture(input);
                liveShip.Step(input, Dt);
            }
            GhostRecording recording = recorder.Complete(30f);

            // Replay through a fresh track + replayer (same code path as a live player).
            var replayTrack = new TrackSystem(trackDef);
            var replayer = new GhostReplayer(recording, replayTrack, hull, tuning);
            while (!replayer.Exhausted) replayer.Step(Dt);

            Assert.That(replayer.Position, Is.EqualTo(liveShip.Position),
                "replay must reproduce the exact final position — bitwise determinism");
            Assert.That(replayer.Speed, Is.EqualTo(liveShip.Speed));
            Assert.That(replayer.Energy.Energy, Is.EqualTo(liveShip.Energy.Energy),
                "the energy trajectory is the game — it must replay exactly");
        }

        [Test]
        public void Replay_AfterSerializationRoundTrip_StillExact()
        {
            TrackDefinition trackDef = MakeArena01();
            HullDefinition hull = MakeMedium();
            var tuning = ScriptableObject.CreateInstance<GameTuning>();

            var liveShip = new ShipController(new TrackSystem(trackDef), hull, tuning);
            var recorder = new GhostRecorder("Arena01", "Medium", "Vex");
            for (int f = 0; f < 900; f++)
            {
                ShipInputState input = InputAt(f);
                recorder.Capture(input);
                liveShip.Step(input, Dt);
            }

            byte[] wire = recorder.Complete(15f).Serialize();
            var replayer = new GhostReplayer(GhostRecording.Deserialize(wire), new TrackSystem(trackDef), hull, tuning);
            while (!replayer.Exhausted) replayer.Step(Dt);

            Assert.That(replayer.Position, Is.EqualTo(liveShip.Position),
                "determinism must survive the wire format");
        }
    }
}
