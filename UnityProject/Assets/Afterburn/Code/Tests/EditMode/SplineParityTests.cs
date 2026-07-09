using Afterburn.Core;
using NUnit.Framework;
using UnityEngine;

namespace Afterburn.Tests
{
    /// <summary>
    /// Verifies the C# Catmull-Rom port reproduces the prototype's measured track
    /// (PortSpec §1): lap length ≈ 1823 u, min corner radius ≈ 58 u, drivable at boost.
    /// Pure Core math — no scene, no assets (BUILD §3: Core is unit-testable with no scene).
    /// </summary>
    [TestFixture]
    public sealed class SplineParityTests
    {
        private static readonly float[] Arena01Shape =
            { 1.00f, 0.94f, 0.78f, 0.74f, 0.92f, 1.06f, 1.10f, 0.86f, 0.72f, 0.82f, 1.02f, 1.08f, 0.92f, 0.84f };

        private static CatmullRomSpline BuildArena01Spline()
        {
            Vector3[] points = TrackSampler.GenerateStarConvexPoints(300f, Arena01Shape);
            return new CatmullRomSpline(points, 0.5f);
        }

        [Test]
        public void Arena01_LapLength_MatchesPrototype()
        {
            // Prototype measured LEN ≈ 1823 u (lap ≈ 29 s at cruise 62 u/s).
            float length = BuildArena01Spline().Length;
            Assert.That(length, Is.EqualTo(1823f).Within(20f),
                "Lap length drifted from the prototype — check CubicPoly/arc-length LUT parity.");
        }

        [Test]
        public void Arena01_MinCornerRadius_MatchesPrototype()
        {
            // Prototype: min corner radius ≈ 57.9 u — drivable at boost (BUILD §6 note).
            CatmullRomSpline spline = BuildArena01Spline();
            const int n = 700;
            var pts = new Vector3[n];
            for (int i = 0; i < n; i++) pts[i] = spline.GetPointAt(i / (float)n);

            float minRadius = float.MaxValue;
            for (int i = 0; i < n; i++)
            {
                Vector3 a = pts[(i - 1 + n) % n];
                Vector3 b = pts[i];
                Vector3 c = pts[(i + 1) % n];
                float ab = Vector3.Distance(a, b);
                float bc = Vector3.Distance(b, c);
                float ca = Vector3.Distance(c, a);
                float area = Vector3.Cross(b - a, c - a).magnitude * 0.5f;
                if (area < 1e-5f) continue;   // collinear — infinite radius
                float r = ab * bc * ca / (4f * area);
                if (r < minRadius) minRadius = r;
            }

            Assert.That(minRadius, Is.EqualTo(57.9f).Within(6f),
                "Min corner radius drifted from the prototype's star-convex loop.");
        }

        [Test]
        public void Samples_NormalsPointInward_AndAreUnit()
        {
            CatmullRomSpline spline = BuildArena01Spline();
            for (int i = 0; i < 700; i += 35)
            {
                float t = i / 700f;
                Vector3 pos = spline.GetPointAt(t);
                Vector3 tan = spline.GetTangentAt(t);
                Vector3 nrm = Vector3.Cross(tan, Vector3.up).normalized;

                Assert.That(tan.magnitude, Is.EqualTo(1f).Within(0.001f));
                Assert.That(nrm.magnitude, Is.EqualTo(1f).Within(0.001f));
                // +nrm points toward the loop interior (PortSpec §1): stepping inward
                // must shrink the distance to the loop centre (origin).
                float inward = (pos + nrm * 5f).magnitude;
                Assert.That(inward, Is.LessThan(pos.magnitude),
                    $"Normal at t={t:F3} does not point toward the interior.");
            }
        }

        [Test]
        public void GetPointAt_IsArcLengthParameterised()
        {
            // Equal u steps must produce (nearly) equal arc steps — the property the
            // prototype relies on for even sample spacing.
            CatmullRomSpline spline = BuildArena01Spline();
            const int n = 100;
            float expected = spline.Length / n;
            Vector3 prev = spline.GetPointAt(0f);
            for (int i = 1; i < n; i++)
            {
                Vector3 current = spline.GetPointAt(i / (float)n);
                float step = Vector3.Distance(prev, current);
                Assert.That(step, Is.EqualTo(expected).Within(expected * 0.15f),
                    $"Uneven arc spacing at step {i}.");
                prev = current;
            }
        }

        [Test]
        public void GenerateStarConvexPoints_MatchesPrototypeFormula()
        {
            Vector3[] pts = TrackSampler.GenerateStarConvexPoints(300f, Arena01Shape);
            Assert.That(pts, Has.Length.EqualTo(14));
            for (int i = 0; i < 14; i++)
            {
                float a = i / 14f * Mathf.PI * 2f;
                Assert.That(pts[i].x, Is.EqualTo(Mathf.Cos(a) * 300f * Arena01Shape[i]).Within(0.001f));
                Assert.That(pts[i].y, Is.EqualTo(0f));
                Assert.That(pts[i].z, Is.EqualTo(Mathf.Sin(a) * 300f * Arena01Shape[i]).Within(0.001f));
            }
        }
    }
}
