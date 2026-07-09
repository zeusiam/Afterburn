using System;
using System.Collections.Generic;
using System.IO;

namespace Afterburn.Core
{
    /// <summary>
    /// Recorded loadout run (BUILD §7.6): per-tick input stream + loadout header, deterministic
    /// playback through the SAME Core sim at the fixed 60 Hz tick. Zero netcode.
    ///
    /// The header carries the COSMETIC loadout (economy audit, blocking U4 requirement): every
    /// race lost to a ghost is an ad for what that player bought or earned — the status
    /// billboard. Cosmetic ids live strictly in the header, never the input stream (replay
    /// parity). The wall-break tick is serialized so replays render the slab correctly.
    /// </summary>
    public sealed class GhostRecording
    {
        public const int FormatVersion = 1;
        public const int TickRate = 60;

        public string TrackId = string.Empty;
        public string HullId = string.Empty;
        public string PilotId = string.Empty;

        // Cosmetic loadout header (empty strings until cosmetics exist — the sockets ship first).
        public string LiveryId = string.Empty;
        public string TrailId = string.Empty;
        public string PlateId = string.Empty;

        /// <summary>Tick index the recording's HeavyWall broke, −1 if never.</summary>
        public int WallBreakTick = -1;

        public float FinishTime;
        public List<byte> InputStream = new();

        // ---- Input packing: 1 byte per tick -----------------------------------

        public static byte Pack(in ShipInputState input)
        {
            byte b = 0;
            if (input.Thrust) b |= 1 << 0;
            if (input.Brake) b |= 1 << 1;
            if (input.Left) b |= 1 << 2;
            if (input.Right) b |= 1 << 3;
            if (input.Boost) b |= 1 << 4;
            if (input.Fire) b |= 1 << 5;
            if (input.Shield) b |= 1 << 6;
            if (input.AbilityEdge) b |= 1 << 7;
            return b;
        }

        public static ShipInputState Unpack(byte b) => new ShipInputState
        {
            Thrust = (b & (1 << 0)) != 0,
            Brake = (b & (1 << 1)) != 0,
            Left = (b & (1 << 2)) != 0,
            Right = (b & (1 << 3)) != 0,
            Boost = (b & (1 << 4)) != 0,
            Fire = (b & (1 << 5)) != 0,
            Shield = (b & (1 << 6)) != 0,
            AbilityEdge = (b & (1 << 7)) != 0,
        };

        // ---- Binary serialization ----------------------------------------------

        public byte[] Serialize()
        {
            using var ms = new MemoryStream();
            using var w = new BinaryWriter(ms);
            w.Write(FormatVersion);
            w.Write(TrackId);
            w.Write(HullId);
            w.Write(PilotId);
            w.Write(LiveryId);
            w.Write(TrailId);
            w.Write(PlateId);
            w.Write(WallBreakTick);
            w.Write(FinishTime);
            w.Write(InputStream.Count);
            w.Write(InputStream.ToArray());
            w.Flush();
            return ms.ToArray();
        }

        public static GhostRecording Deserialize(byte[] data)
        {
            using var ms = new MemoryStream(data);
            using var r = new BinaryReader(ms);
            int version = r.ReadInt32();
            if (version != FormatVersion)
                throw new InvalidDataException($"Ghost format v{version} unsupported (expected v{FormatVersion}).");
            var rec = new GhostRecording
            {
                TrackId = r.ReadString(),
                HullId = r.ReadString(),
                PilotId = r.ReadString(),
                LiveryId = r.ReadString(),
                TrailId = r.ReadString(),
                PlateId = r.ReadString(),
                WallBreakTick = r.ReadInt32(),
                FinishTime = r.ReadSingle(),
            };
            int count = r.ReadInt32();
            rec.InputStream = new List<byte>(r.ReadBytes(count));
            return rec;
        }
    }

    /// <summary>Captures a run tick-by-tick during Racing (wired by the race loop).</summary>
    public sealed class GhostRecorder
    {
        private readonly GhostRecording _recording = new();
        private int _tick;

        public GhostRecorder(string trackId, string hullId, string pilotId)
        {
            _recording.TrackId = trackId;
            _recording.HullId = hullId;
            _recording.PilotId = pilotId;
        }

        public void Capture(in ShipInputState input)
        {
            _recording.InputStream.Add(GhostRecording.Pack(input));
            _tick++;
        }

        public void NoteWallBreak()
        {
            if (_recording.WallBreakTick < 0) _recording.WallBreakTick = _tick;
        }

        public GhostRecording Complete(float finishTime)
        {
            _recording.FinishTime = finishTime;
            return _recording;
        }
    }

    /// <summary>
    /// Replays a recording through a private <see cref="ShipController"/> on the shared track —
    /// the SAME code path as a live player, so determinism is structural, not aspirational.
    /// A replayed Heavy re-breaks the wall live (its inputs assume it — prototype semantics).
    /// </summary>
    public sealed class GhostReplayer : IRacer
    {
        private readonly GhostRecording _recording;
        private readonly ShipController _ship;
        private int _tick;

        public GhostReplayer(GhostRecording recording, TrackSystem track, HullDefinition hull, GameTuning tuning)
        {
            _recording = recording ?? throw new ArgumentNullException(nameof(recording));
            _ship = new ShipController(track, hull, tuning, lane: 0);
        }

        public bool Exhausted => _tick >= _recording.InputStream.Count;
        public ShipController Ship => _ship;

        public void Step(float dt)
        {
            if (Exhausted) return;
            _ship.Step(GhostRecording.Unpack(_recording.InputStream[_tick]), dt);
            _tick++;
        }

        // ---- IRacer passthrough -----------------------------------------------
        public UnityEngine.Vector3 Position => _ship.Position;
        public UnityEngine.Vector3 Forward => _ship.Forward;
        public float Speed => _ship.Speed;
        public HullDefinition Hull => _ship.Hull;
        public EnergyCore Energy => _ship.Energy;
        public bool Shielding => _ship.Shielding;
        public float IntangibleTimer => _ship.IntangibleTimer;
        public float SpinoutTimer => _ship.SpinoutTimer;
        public void ApplySpinout(float duration) => _ship.ApplySpinout(duration);
        public void ApplyIntangible(float duration) => _ship.ApplyIntangible(duration);
    }
}
