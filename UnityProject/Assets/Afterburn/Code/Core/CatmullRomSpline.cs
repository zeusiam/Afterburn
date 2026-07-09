using System;
using UnityEngine;

namespace Afterburn.Core
{
    /// <summary>
    /// Closed uniform Catmull-Rom spline, written to match three.js r128
    /// <c>CatmullRomCurve3(points, closed=true, 'catmullrom', tension)</c> numerically —
    /// the prototype's track sampling is the spec (PortSpec §1), so this reproduces:
    /// the cubic-poly basis with tangents <c>tension·(p2−p0)</c>, the 200-division arc-length
    /// LUT with linear interpolation (<c>getPointAt</c>/<c>getUtoTmapping</c>), and the
    /// clamped numeric tangent (<c>getTangent</c>, delta 1e-4).
    /// </summary>
    public sealed class CatmullRomSpline
    {
        /// <summary>three.js Curve.arcLengthDivisions default — keep 200 for prototype parity.</summary>
        public const int ArcLengthDivisions = 200;

        private readonly Vector3[] _points;
        private readonly float _tension;
        private float[]? _arcLengths;   // cumulative lengths, ArcLengthDivisions + 1 entries

        public CatmullRomSpline(Vector3[] points, float tension = 0.5f)
        {
            if (points == null || points.Length < 4)
                throw new ArgumentException("Closed Catmull-Rom needs at least 4 control points.", nameof(points));
            _points = points;
            _tension = tension;
        }

        /// <summary>Total curve length from the 200-division LUT (three.js getLength()).</summary>
        public float Length
        {
            get
            {
                float[] lengths = GetArcLengths();
                return lengths[lengths.Length - 1];
            }
        }

        /// <summary>Position at raw curve parameter t ∈ [0,1] (three.js getPoint, closed).</summary>
        public Vector3 GetPoint(float t)
        {
            int l = _points.Length;
            float p = l * t;                       // closed: (l - 0) * t
            int intPoint = Mathf.FloorToInt(p);
            float weight = p - intPoint;
            if (intPoint < 0)                      // three.js closed wrap for negative intPoint
            {
                intPoint += (Mathf.FloorToInt(Mathf.Abs(intPoint) / (float)l) + 1) * l;
            }

            Vector3 p0 = _points[Mod(intPoint - 1, l)];
            Vector3 p1 = _points[Mod(intPoint, l)];
            Vector3 p2 = _points[Mod(intPoint + 1, l)];
            Vector3 p3 = _points[Mod(intPoint + 2, l)];

            return new Vector3(
                CubicPoly(p0.x, p1.x, p2.x, p3.x, weight),
                CubicPoly(p0.y, p1.y, p2.y, p3.y, weight),
                CubicPoly(p0.z, p1.z, p2.z, p3.z, weight));
        }

        /// <summary>Numeric tangent at raw t (three.js getTangent — clamped, delta 1e-4).</summary>
        public Vector3 GetTangent(float t)
        {
            const float delta = 0.0001f;
            float t1 = Mathf.Max(0f, t - delta);
            float t2 = Mathf.Min(1f, t + delta);
            return (GetPoint(t2) - GetPoint(t1)).normalized;
        }

        /// <summary>Position at arc-length fraction u ∈ [0,1] (three.js getPointAt).</summary>
        public Vector3 GetPointAt(float u) => GetPoint(UToT(u));

        /// <summary>Tangent at arc-length fraction u ∈ [0,1] (three.js getTangentAt).</summary>
        public Vector3 GetTangentAt(float u) => GetTangent(UToT(u));

        /// <summary>Arc-length u → raw parameter t via the LUT (three.js getUtoTmapping).</summary>
        public float UToT(float u)
        {
            float[] arcLengths = GetArcLengths();
            int il = arcLengths.Length;
            float targetArcLength = u * arcLengths[il - 1];

            // Binary search for the largest index whose cumulative length <= target.
            int low = 0, high = il - 1, i;
            while (low <= high)
            {
                i = low + (high - low) / 2;
                float comparison = arcLengths[i] - targetArcLength;
                if (comparison < 0f) low = i + 1;
                else if (comparison > 0f) high = i - 1;
                else { high = i; break; }
            }
            i = high;
            if (Mathf.Approximately(arcLengths[i], targetArcLength))
            {
                return i / (float)(il - 1);
            }

            float lengthBefore = arcLengths[i];
            float lengthAfter = arcLengths[i + 1];
            float segmentFraction = (targetArcLength - lengthBefore) / (lengthAfter - lengthBefore);
            return (i + segmentFraction) / (il - 1);
        }

        private float[] GetArcLengths()
        {
            if (_arcLengths != null) return _arcLengths;

            var lengths = new float[ArcLengthDivisions + 1];
            Vector3 last = GetPoint(0f);
            float sum = 0f;
            lengths[0] = 0f;
            for (int d = 1; d <= ArcLengthDivisions; d++)
            {
                Vector3 current = GetPoint(d / (float)ArcLengthDivisions);
                sum += Vector3.Distance(current, last);
                lengths[d] = sum;
                last = current;
            }
            _arcLengths = lengths;
            return lengths;
        }

        /// <summary>Hermite basis with Catmull-Rom tangents t·(p2−p0) / t·(p3−p1) (three.js CubicPoly).</summary>
        private float CubicPoly(float x0, float x1, float x2, float x3, float w)
        {
            float t0 = _tension * (x2 - x0);
            float t1 = _tension * (x3 - x1);
            float c0 = x1;
            float c1 = t0;
            float c2 = -3f * x1 + 3f * x2 - 2f * t0 - t1;
            float c3 = 2f * x1 - 2f * x2 + t0 + t1;
            float w2 = w * w;
            return c0 + c1 * w + c2 * w2 + c3 * w2 * w;
        }

        private static int Mod(int a, int n) => ((a % n) + n) % n;
    }
}
