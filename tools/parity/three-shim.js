'use strict';
/*
 * three-shim.js — minimal THREE r128 shim for the Afterburn parity harness.
 *
 * REAL (algorithms ported line-for-line from three.js r128, double precision):
 *   - Vector3 (the subset the sliced prototype code actually calls)
 *   - Curve base-class arc-length machinery: getLengths (200-division cumulative
 *     Euclidean LUT), getUtoTmapping (binary search), getPointAt, getTangent
 *     (numeric delta 1e-4 clamped to [0,1]), getTangentAt, getLength
 *   - CatmullRomCurve3: closed uniform 'catmullrom' with tension, via the r128
 *     CubicPoly algorithm (initCatmullRom tangents t0 = tension*(x2-x0),
 *     t1 = tension*(x3-x1)), including the r128 closed-curve intPoint wrap.
 *
 * INERT: geometries, materials, meshes, Group, GridHelper — just enough surface
 * for the Track IIFE's (physics-irrelevant) mesh building to run without throwing.
 * Object3D-like stubs carry a REAL Vector3 as .position because Track's gateAt()
 * and shortcut-marker code call .copy().addScaledVector() on mesh positions.
 */

/* ------------------------------- Vector3 -------------------------------- */
// r128 semantics preserved exactly, including normalize() implemented as
// divideScalar(length()||1) and divideScalar() as multiplyScalar(1/scalar).
class Vector3 {
  constructor(x = 0, y = 0, z = 0) { this.x = x; this.y = y; this.z = z; }
  set(x, y, z) { this.x = x; this.y = y; this.z = z; return this; }
  setScalar(s) { this.x = s; this.y = s; this.z = s; return this; }
  clone() { return new this.constructor(this.x, this.y, this.z); }
  copy(v) { this.x = v.x; this.y = v.y; this.z = v.z; return this; }
  add(v) { this.x += v.x; this.y += v.y; this.z += v.z; return this; }
  addVectors(a, b) { this.x = a.x + b.x; this.y = a.y + b.y; this.z = a.z + b.z; return this; }
  sub(v) { this.x -= v.x; this.y -= v.y; this.z -= v.z; return this; }
  subVectors(a, b) { this.x = a.x - b.x; this.y = a.y - b.y; this.z = a.z - b.z; return this; }
  addScaledVector(v, s) { this.x += v.x * s; this.y += v.y * s; this.z += v.z * s; return this; }
  multiplyScalar(scalar) { this.x *= scalar; this.y *= scalar; this.z *= scalar; return this; }
  divideScalar(scalar) { return this.multiplyScalar(1 / scalar); }      // r128: reciprocal multiply
  length() { return Math.sqrt(this.x * this.x + this.y * this.y + this.z * this.z); }
  lengthSq() { return this.x * this.x + this.y * this.y + this.z * this.z; }
  normalize() { return this.divideScalar(this.length() || 1); }         // r128 exact
  distanceTo(v) { return Math.sqrt(this.distanceToSquared(v)); }
  distanceToSquared(v) {
    const dx = this.x - v.x, dy = this.y - v.y, dz = this.z - v.z;
    return dx * dx + dy * dy + dz * dz;
  }
  dot(v) { return this.x * v.x + this.y * v.y + this.z * v.z; }
  crossVectors(a, b) {
    const ax = a.x, ay = a.y, az = a.z;
    const bx = b.x, by = b.y, bz = b.z;
    this.x = ay * bz - az * by;
    this.y = az * bx - ax * bz;
    this.z = ax * by - ay * bx;
    return this;
  }
  lerp(v, alpha) {
    this.x += (v.x - this.x) * alpha;
    this.y += (v.y - this.y) * alpha;
    this.z += (v.z - this.z) * alpha;
    return this;
  }
}
Vector3.prototype.isVector3 = true;

/* ----------------------------- Curve (base) ------------------------------ */
// Ported from three.js r128 src/extras/core/Curve.js (only the members the
// prototype exercises: getPoint is supplied by the subclass).
class Curve {
  constructor() {
    this.type = 'Curve';
    this.arcLengthDivisions = 200;
  }

  getPoint(/* t, optionalTarget */) {
    throw new Error('Curve: .getPoint() not implemented.');
  }

  getPointAt(u, optionalTarget) {
    const t = this.getUtoTmapping(u);
    return this.getPoint(t, optionalTarget);
  }

  getLength() {
    const lengths = this.getLengths();
    return lengths[lengths.length - 1];
  }

  getLengths(divisions = this.arcLengthDivisions) {
    if (this.cacheArcLengths &&
      (this.cacheArcLengths.length === divisions + 1) &&
      !this.needsUpdate) {
      return this.cacheArcLengths;
    }

    this.needsUpdate = false;

    const cache = [];
    let current, last = this.getPoint(0);
    let sum = 0;

    cache.push(0);

    for (let p = 1; p <= divisions; p++) {
      current = this.getPoint(p / divisions);
      sum += current.distanceTo(last);
      cache.push(sum);
      last = current;
    }

    this.cacheArcLengths = cache;
    return cache; // { sums: cache, sum: sum }; Sum is in the last element.
  }

  updateArcLengths() {
    this.needsUpdate = true;
    this.getLengths();
  }

  // Given u ( 0 .. 1 ), get a t to find p. This gives you points which are equidistant
  getUtoTmapping(u, distance) {
    const arcLengths = this.getLengths();

    let i = 0;
    const il = arcLengths.length;

    let targetArcLength; // The targeted u distance value to get

    if (distance) {
      targetArcLength = distance;
    } else {
      targetArcLength = u * arcLengths[il - 1];
    }

    // binary search for the index with largest value smaller than target u distance
    let low = 0, high = il - 1, comparison;

    while (low <= high) {
      i = Math.floor(low + (high - low) / 2); // less likely to overflow

      comparison = arcLengths[i] - targetArcLength;

      if (comparison < 0) {
        low = i + 1;
      } else if (comparison > 0) {
        high = i - 1;
      } else {
        high = i;
        break;
      }
    }

    i = high;

    if (arcLengths[i] === targetArcLength) {
      return i / (il - 1);
    }

    // we could get finer grain at lengths, or use simple interpolation between two points
    const lengthBefore = arcLengths[i];
    const lengthAfter = arcLengths[i + 1];

    const segmentLength = lengthAfter - lengthBefore;

    // determine where we are between the 'before' and 'after' points
    const segmentFraction = (targetArcLength - lengthBefore) / segmentLength;

    // add that fractional amount to t
    const t = (i + segmentFraction) / (il - 1);

    return t;
  }

  // Returns a unit vector tangent at t. Uses a small delta (1e-4) to approximate.
  getTangent(t, optionalTarget) {
    const delta = 0.0001;
    let t1 = t - delta;
    let t2 = t + delta;

    // Capping in case of danger
    if (t1 < 0) t1 = 0;
    if (t2 > 1) t2 = 1;

    const pt1 = this.getPoint(t1);
    const pt2 = this.getPoint(t2);

    const tangent = optionalTarget || new Vector3();

    tangent.copy(pt2).sub(pt1).normalize();

    return tangent;
  }

  getTangentAt(u, optionalTarget) {
    const t = this.getUtoTmapping(u);
    return this.getTangent(t, optionalTarget);
  }
}

/* --------------------------- CatmullRomCurve3 ---------------------------- */
// Ported from three.js r128 src/extras/curves/CatmullRomCurve3.js.
function CubicPoly() {
  let c0 = 0, c1 = 0, c2 = 0, c3 = 0;

  /*
   * Compute coefficients for a cubic polynomial
   *   p(s) = c0 + c1*s + c2*s^2 + c3*s^3
   * such that p(0) = x0, p(1) = x1, p'(0) = t0 and p'(1) = t1.
   */
  function init(x0, x1, t0, t1) {
    c0 = x0;
    c1 = t0;
    c2 = -3 * x0 + 3 * x1 - 2 * t0 - t1;
    c3 = 2 * x0 - 2 * x1 + t0 + t1;
  }

  return {
    initCatmullRom: function (x0, x1, x2, x3, tension) {
      init(x1, x2, tension * (x2 - x0), tension * (x3 - x1));
    },
    initNonuniformCatmullRom: function (x0, x1, x2, x3, dt0, dt1, dt2) {
      // compute tangents when parameterized in [t1,t2]
      let t1 = (x1 - x0) / dt0 - (x2 - x0) / (dt0 + dt1) + (x2 - x1) / dt1;
      let t2 = (x2 - x1) / dt1 - (x3 - x1) / (dt1 + dt2) + (x3 - x2) / dt2;

      // rescale tangents for parametrization in [0,1]
      t1 *= dt1;
      t2 *= dt1;

      init(x1, x2, t1, t2);
    },
    calc: function (t) {
      const t2 = t * t;
      const t3 = t2 * t;
      return c0 + c1 * t + c2 * t2 + c3 * t3;
    }
  };
}

// module-scoped temporaries, exactly as in r128
const tmp = new Vector3();
const px = new CubicPoly(), py = new CubicPoly(), pz = new CubicPoly();

class CatmullRomCurve3 extends Curve {
  constructor(points = [], closed = false, curveType = 'centripetal', tension = 0.5) {
    super();
    this.type = 'CatmullRomCurve3';
    this.points = points;
    this.closed = closed;
    this.curveType = curveType;
    this.tension = tension;
  }

  getPoint(t, optionalTarget = new Vector3()) {
    const point = optionalTarget;

    const points = this.points;
    const l = points.length;

    const p = (l - (this.closed ? 0 : 1)) * t;
    let intPoint = Math.floor(p);
    let weight = p - intPoint;

    if (this.closed) {
      intPoint += intPoint > 0 ? 0 : (Math.floor(Math.abs(intPoint) / l) + 1) * l;
    } else if (weight === 0 && intPoint === l - 1) {
      intPoint = l - 2;
      weight = 1;
    }

    let p0, p3; // 4 points (p1 & p2 defined below)

    if (this.closed || intPoint > 0) {
      p0 = points[(intPoint - 1) % l];
    } else {
      // extrapolate first point
      tmp.subVectors(points[0], points[1]).add(points[0]);
      p0 = tmp;
    }

    const p1 = points[intPoint % l];
    const p2 = points[(intPoint + 1) % l];

    if (this.closed || intPoint + 2 < l) {
      p3 = points[(intPoint + 2) % l];
    } else {
      // extrapolate last point
      tmp.subVectors(points[l - 1], points[l - 2]).add(points[l - 1]);
      p3 = tmp;
    }

    if (this.curveType === 'centripetal' || this.curveType === 'chordal') {
      // init Centripetal / Chordal Catmull-Rom
      const pow = this.curveType === 'chordal' ? 0.5 : 0.25;
      let dt0 = Math.pow(p0.distanceToSquared(p1), pow);
      let dt1 = Math.pow(p1.distanceToSquared(p2), pow);
      let dt2 = Math.pow(p2.distanceToSquared(p3), pow);

      // safety check for repeated points
      if (dt1 < 1e-4) dt1 = 1.0;
      if (dt0 < 1e-4) dt0 = dt1;
      if (dt2 < 1e-4) dt2 = dt1;

      px.initNonuniformCatmullRom(p0.x, p1.x, p2.x, p3.x, dt0, dt1, dt2);
      py.initNonuniformCatmullRom(p0.y, p1.y, p2.y, p3.y, dt0, dt1, dt2);
      pz.initNonuniformCatmullRom(p0.z, p1.z, p2.z, p3.z, dt0, dt1, dt2);
    } else if (this.curveType === 'catmullrom') {
      px.initCatmullRom(p0.x, p1.x, p2.x, p3.x, this.tension);
      py.initCatmullRom(p0.y, p1.y, p2.y, p3.y, this.tension);
      pz.initCatmullRom(p0.z, p1.z, p2.z, p3.z, this.tension);
    }

    point.set(px.calc(weight), py.calc(weight), pz.calc(weight));

    return point;
  }
}
CatmullRomCurve3.prototype.isCatmullRomCurve3 = true;

/* ------------------------------ Inert stubs ------------------------------ */
// Object3D-like: real Vector3 position (Track calls .copy().addScaledVector()
// on mesh positions), plain rotation record, children array, no-op transforms.
class Object3DStub {
  constructor() {
    this.position = new Vector3();
    this.rotation = { x: 0, y: 0, z: 0 };
    this.scale = new Vector3(1, 1, 1);
    this.children = [];
    this.visible = true;
  }
  add(...objects) { for (const o of objects) this.children.push(o); return this; }
  remove(...objects) {
    for (const o of objects) {
      const i = this.children.indexOf(o);
      if (i !== -1) this.children.splice(i, 1);
    }
    return this;
  }
  lookAt() { return this; }
  rotateX() { return this; }
  rotateY() { return this; }
  rotateZ() { return this; }
  traverse(cb) { cb(this); for (const c of this.children) c.traverse && c.traverse(cb); }
}

class Group extends Object3DStub {}
class Mesh extends Object3DStub {
  constructor(geometry, material) { super(); this.geometry = geometry; this.material = material; }
}
class Points extends Object3DStub {
  constructor(geometry, material) { super(); this.geometry = geometry; this.material = material; }
}
class GridHelper extends Object3DStub {
  constructor(...args) { super(); this.args = args; }
}

class BufferGeometry {
  constructor() { this.attributes = {}; }
  setAttribute(name, attribute) { this.attributes[name] = attribute; return this; }
  computeVertexNormals() {}
}
class BufferAttribute {
  constructor(array, itemSize) { this.array = array; this.itemSize = itemSize; }
}

class GeometryStub { constructor(...args) { this.args = args; } }
class TorusGeometry extends GeometryStub {}
class CylinderGeometry extends GeometryStub {}
class BoxGeometry extends GeometryStub {}
class ConeGeometry extends GeometryStub {}
class SphereGeometry extends GeometryStub {}
class OctahedronGeometry extends GeometryStub {}

class MaterialStub { constructor(params) { Object.assign(this, params || {}); } }

module.exports = {
  // real
  Vector3,
  Curve,
  CatmullRomCurve3,
  // inert
  Group,
  Mesh,
  Points,
  GridHelper,
  BufferGeometry,
  BufferAttribute,
  TorusGeometry,
  CylinderGeometry,
  BoxGeometry,
  ConeGeometry,
  SphereGeometry,
  OctahedronGeometry,
  MeshStandardMaterial: MaterialStub,
  MeshBasicMaterial: MaterialStub,
  PointsMaterial: MaterialStub,
  // constants
  DoubleSide: 2,
  FrontSide: 0,
  BackSide: 1,
};
