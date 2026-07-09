using System;
using UnityEngine;

namespace Afterburn.Core
{
    /// <summary>
    /// One cached centreline sample — the track frame (PortSpec §1 + D6 directive):
    /// position, unit tangent, unit normal, unit up. All ship/camera math is written against
    /// this frame; on flat Arena01, Up is exactly world-up and everything degenerates to the
    /// prototype's arithmetic.
    /// </summary>
    public readonly struct TrackSample
    {
        public readonly Vector3 Pos;
        public readonly Vector3 Tan;

        /// <summary>normalize(cross(Tan, up)) — for the prototype loop, +Nrm points toward the interior.</summary>
        public readonly Vector3 Nrm;

        /// <summary>normalize(cross(Nrm, Tan)) — exactly (0,1,0) on a flat track.</summary>
        public readonly Vector3 Up;

        public TrackSample(Vector3 pos, Vector3 tan, Vector3 nrm, Vector3 up)
        {
            Pos = pos;
            Tan = tan;
            Nrm = nrm;
            Up = up;
        }
    }

    /// <summary>
    /// Builds the dense sample cache from a <see cref="TrackDefinition"/> exactly as the
    /// prototype does: N samples at arc-length fractions i/N, tangent normalised, normal =
    /// cross(tangent, up) normalised. Pure math — unit-testable with no scene (BUILD §3).
    /// </summary>
    public static class TrackSampler
    {
        /// <summary>Generate the star-convex control points from R0 × shape (PortSpec §1 formula).</summary>
        public static Vector3[] GenerateStarConvexPoints(float baseRadius, float[] radiusShape)
        {
            if (radiusShape == null || radiusShape.Length < 4)
                throw new ArgumentException("radiusShape needs at least 4 entries.", nameof(radiusShape));

            var points = new Vector3[radiusShape.Length];
            for (int i = 0; i < radiusShape.Length; i++)
            {
                float a = i / (float)radiusShape.Length * Mathf.PI * 2f;
                points[i] = new Vector3(
                    Mathf.Cos(a) * baseRadius * radiusShape[i],
                    0f,
                    Mathf.Sin(a) * baseRadius * radiusShape[i]);
            }
            return points;
        }

        /// <summary>Build the closed spline for a track definition.</summary>
        public static CatmullRomSpline BuildSpline(TrackDefinition track)
        {
            if (track == null) throw new ArgumentNullException(nameof(track));
            if (track.controlPoints == null || track.controlPoints.Length < 4)
                throw new InvalidOperationException(
                    $"TrackDefinition '{track.name}' has no baked control points — run Veratus/Afterburn/Create or Update SOs.");
            return new CatmullRomSpline(track.controlPoints, track.catmullTension);
        }

        /// <summary>Build the dense sample cache (prototype: N = 700 at t = i/N via getPointAt/getTangentAt).</summary>
        public static TrackSample[] BuildSamples(TrackDefinition track)
        {
            CatmullRomSpline spline = BuildSpline(track);
            int n = track.sampleCount;
            var samples = new TrackSample[n];
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)n;
                Vector3 pos = spline.GetPointAt(t);
                Vector3 tan = spline.GetTangentAt(t);           // already normalised
                Vector3 nrm = Vector3.Cross(tan, Vector3.up).normalized;
                Vector3 up = Vector3.Cross(nrm, tan).normalized; // world-up exactly on flat tracks
                samples[i] = new TrackSample(pos, tan, nrm, up);
            }
            return samples;
        }
    }
}
