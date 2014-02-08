﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Google.Common.Geometry;
using NUnit.Framework;

namespace S2Geometry.Tests
{
    public class S2LoopTest : GeometryTestCase
    {
         // A stripe that slightly over-wraps the equator.
  private S2Loop candyCane = makeLoop("-20:150, -20:-70, 0:70, 10:-150, 10:70, -10:-70");

  // A small clockwise loop in the northern & eastern hemisperes.
  private S2Loop smallNeCw = makeLoop("35:20, 45:20, 40:25");

  // Loop around the north pole at 80 degrees.
  private S2Loop arctic80 = makeLoop("80:-150, 80:-30, 80:90");

  // Loop around the south pole at 80 degrees.
  private S2Loop antarctic80 = makeLoop("-80:120, -80:0, -80:-120");

  // The northern hemisphere, defined using two pairs of antipodal points.
  private S2Loop northHemi = makeLoop("0:-180, 0:-90, 0:0, 0:90");

  // The northern hemisphere, defined using three points 120 degrees apart.
  private S2Loop northHemi3 = makeLoop("0:-180, 0:-60, 0:60");

  // The western hemisphere, defined using two pairs of antipodal points.
  private S2Loop westHemi = makeLoop("0:-180, -90:0, 0:0, 90:0");

  // The "near" hemisphere, defined using two pairs of antipodal points.
  private S2Loop nearHemi = makeLoop("0:-90, -90:0, 0:90, 90:0");

  // A diamond-shaped loop around the point 0:180.
  private S2Loop loopA = makeLoop("0:178, -1:180, 0:-179, 1:-180");

  // Another diamond-shaped loop around the point 0:180.
  private S2Loop loopB = makeLoop("0:179, -1:180, 0:-178, 1:-180");

  // The intersection of A and B.
  private S2Loop aIntersectB = makeLoop("0:179, -1:180, 0:-179, 1:-180");

  // The union of A and B.
  private S2Loop aUnionB = makeLoop("0:178, -1:180, 0:-178, 1:-180");

  // A minus B (concave)
  private S2Loop aMinusB = makeLoop("0:178, -1:180, 0:179, 1:-180");

  // B minus A (concave)
  private S2Loop bMinusA = makeLoop("0:-179, -1:180, 0:-178, 1:-180");

  // A self-crossing loop with a duplicated vertex
  private S2Loop bowtie = makeLoop("0:0, 2:0, 1:1, 0:2, 2:2, 1:1");

  // Initialized below.
  private S2Loop southHemi;
  private S2Loop eastHemi;
  private S2Loop farHemi;

  
  protected override void SetUp() {

    base.SetUp();
    southHemi = new S2Loop(northHemi);
    southHemi.invert();

    eastHemi = new S2Loop(westHemi);
    eastHemi.invert();

    farHemi = new S2Loop(nearHemi);
    farHemi.invert();
  }

        [Test]
  public void testBounds() {
    assertTrue(candyCane.getRectBound().Lng.isFull());
    assertTrue(candyCane.getRectBound().latLo().degrees() < -20);
    assertTrue(candyCane.getRectBound().latHi().degrees() > 10);
    assertTrue(smallNeCw.getRectBound().isFull());
    assertEquals(arctic80.getRectBound(),
        new S2LatLngRect(S2LatLng.fromDegrees(80, -180), S2LatLng.fromDegrees(90, 180)));
    assertEquals(antarctic80.getRectBound(),
        new S2LatLngRect(S2LatLng.fromDegrees(-90, -180), S2LatLng.fromDegrees(-80, 180)));

    arctic80.invert();
    // The highest latitude of each edge is attained at its midpoint.
    S2Point mid = S2Point.mul(S2Point.add(arctic80.vertex(0), arctic80.vertex(1)), 0.5);
    assertDoubleNear(arctic80.getRectBound().latHi().radians(), new S2LatLng(mid).lat().radians());
    arctic80.invert();

    assertTrue(southHemi.getRectBound().Lng.isFull());
    assertEquals(southHemi.getRectBound().Lat, new R1Interval(-S2.M_PI_2, 0));
  }

        [Test]
  public void testAreaCentroid() {
    assertDoubleNear(northHemi.getArea(), 2 * S2.M_PI);
    assertDoubleNear(eastHemi.getArea(), 2 * S2.M_PI);

    // Construct spherical caps of random height, and approximate their boundary
    // with closely spaces vertices. Then check that the area and centroid are
    // correct.

    for (int i = 0; i < 100; ++i) {
      // Choose a coordinate frame for the spherical cap.
      S2Point x = randomPoint();
      S2Point y = S2Point.normalize(S2Point.crossProd(x, randomPoint()));
      S2Point z = S2Point.normalize(S2Point.crossProd(x, y));

      // Given two points at latitude phi and whose longitudes differ by dtheta,
      // the geodesic between the two points has a maximum latitude of
      // atan(Tan(phi) / Cos(dtheta/2)). This can be derived by positioning
      // the two points at (-dtheta/2, phi) and (dtheta/2, phi).
      //
      // We want to position the vertices close enough together so that their
      // maximum distance from the boundary of the spherical cap is kMaxDist.
      // Thus we want fabs(atan(Tan(phi) / Cos(dtheta/2)) - phi) <= kMaxDist.
      double kMaxDist = 1e-6;
      double height = 2 * rand.NextDouble();
      double phi = Math.Asin(1 - height);
      double maxDtheta =
          2 * Math.Acos(Math.Tan(Math.Abs(phi)) / Math.Tan(Math.Abs(phi) + kMaxDist));
      maxDtheta = Math.Min(S2.M_PI, maxDtheta); // At least 3 vertices.

      List<S2Point> vertices = new List<S2Point>();
      for (double theta = 0; theta < 2 * S2.M_PI; theta += rand.NextDouble() * maxDtheta) {

        S2Point xCosThetaCosPhi = S2Point.mul(x, (Math.Cos(theta) * Math.Cos(phi)));
        S2Point ySinThetaCosPhi = S2Point.mul(y, (Math.Sin(theta) * Math.Cos(phi)));
        S2Point zSinPhi = S2Point.mul(z, Math.Sin(phi));

        S2Point sum = S2Point.add(S2Point.add(xCosThetaCosPhi, ySinThetaCosPhi), zSinPhi);

        vertices.Add(sum);
      }

      S2Loop loop = new S2Loop(vertices);
      S2AreaCentroid areaCentroid = loop.getAreaAndCentroid();

      double area = loop.getArea();
      S2Point centroid = loop.getCentroid();
      double expectedArea = 2 * S2.M_PI * height;
      assertTrue(areaCentroid.getArea() == area);
      assertTrue(centroid.Equals(areaCentroid.getCentroid()));
      assertTrue(Math.Abs(area - expectedArea) <= 2 * S2.M_PI * kMaxDist);

      // high probability
      assertTrue(Math.Abs(area - expectedArea) >= 0.01 * kMaxDist);

      S2Point expectedCentroid = S2Point.mul(z, expectedArea * (1 - 0.5 * height));

      assertTrue(S2Point.sub(centroid, expectedCentroid).norm() <= 2 * kMaxDist);
    }
  }

  private S2Loop rotate(S2Loop loop) {
    List<S2Point> vertices = new List<S2Point>();
    for (int i = 1; i <= loop.numVertices(); ++i) {
      vertices.Add(loop.vertex(i));
    }
    return new S2Loop(vertices);
  }

        [Test]
  public void testContains() {
    assertTrue(candyCane.contains(S2LatLng.fromDegrees(5, 71).toPoint()));
    for (int i = 0; i < 4; ++i) {
      assertTrue(northHemi.contains(new S2Point(0, 0, 1)));
      assertTrue(!northHemi.contains(new S2Point(0, 0, -1)));
      assertTrue(!southHemi.contains(new S2Point(0, 0, 1)));
      assertTrue(southHemi.contains(new S2Point(0, 0, -1)));
      assertTrue(!westHemi.contains(new S2Point(0, 1, 0)));
      assertTrue(westHemi.contains(new S2Point(0, -1, 0)));
      assertTrue(eastHemi.contains(new S2Point(0, 1, 0)));
      assertTrue(!eastHemi.contains(new S2Point(0, -1, 0)));
      northHemi = rotate(northHemi);
      southHemi = rotate(southHemi);
      eastHemi = rotate(eastHemi);
      westHemi = rotate(westHemi);
    }

    // This code checks each cell vertex is contained by exactly one of
    // the adjacent cells.
    for (int level = 0; level < 3; ++level) {
      List<S2Loop> loops = new List<S2Loop>();
      List<S2Point> loopVertices = new List<S2Point>();
      ISet<S2Point> points = new HashSet<S2Point>();
      for (S2CellId id = S2CellId.begin(level); !id.Equals(S2CellId.end(level)); id = id.next()) {
        S2Cell cell = new S2Cell(id);
        points.Add(cell.getCenter());
        for (int k = 0; k < 4; ++k) {
          loopVertices.Add(cell.getVertex(k));
          points.Add(cell.getVertex(k));
        }
        loops.Add(new S2Loop(loopVertices));
        loopVertices.Clear();
      }
      foreach (S2Point point in points) {
        int count = 0;
        for (int j = 0; j < loops.Count; ++j) {
          if (loops[j].contains(point)) {
            ++count;
          }
        }
        assertEquals(count, 1);
      }
    }
  }

  private S2CellId advance(S2CellId id, int n) {
    while (id.isValid() && --n >= 0) {
      id = id.next();
    }
    return id;
  }

  private S2Loop makeCellLoop(S2CellId begin, S2CellId end) {
    // Construct a CCW polygon whose boundary is the union of the cell ids
    // in the range [begin, end). We Add the edges one by one, removing
    // any edges that are already present in the opposite direction.

    IDictionary<S2Point, ISet<S2Point>> edges = new Dictionary<S2Point, ISet<S2Point>>();
    for (S2CellId id = begin; !id.Equals(end); id = id.next()) {
      S2Cell cell = new S2Cell(id);
      for (int k = 0; k < 4; ++k) {
        S2Point a = cell.getVertex(k);
        S2Point b = cell.getVertex((k + 1) & 3);
        if (!edges.ContainsKey(b)) {
          edges.Add(b, new HashSet<S2Point>());
        }
        // if a is in b's set, remove it and remove b's set if it's empty
        // otherwise, Add b to a's set
        if (!edges[b].Remove(a)) {
          if (!edges.ContainsKey(a)) {
            edges.Add(a, new HashSet<S2Point>());
          }
          edges[a].Add(b);
        } else if (edges[b].Count == 0) {
          edges.Remove(b);
        }
      }
    }

    // The remaining edges form a single loop. We simply follow it starting
    // at an arbitrary vertex and build up a list of vertices.

    List<S2Point> vertices = new List<S2Point>();
    //S2Point p = edges.keySet().iterator().next();
      var p = edges.Keys.First();
    while (edges.Any()) {
      assertEquals(1, edges[p].Count);
      S2Point next = edges[p].First();
      //S2Point next = edges[p].iterator().next();
      vertices.Add(p);
      edges.Remove(p);
      p = next;
    }

    return new S2Loop(vertices);
  }

  private void assertRelation(
      S2Loop a, S2Loop b, int containsOrCrosses, bool intersects, bool nestable) {
    assertEquals(a.contains(b), containsOrCrosses == 1);
    assertEquals(a.intersects(b), intersects);
    if (nestable) {
      assertEquals(a.containsNested(b), a.contains(b));
    }
    if (containsOrCrosses >= -1) {
      assertEquals(a.containsOrCrosses(b), containsOrCrosses);
    }
  }

[Test]
  public void testLoopRelations() {
    assertRelation(northHemi, northHemi, 1, true, false);
    assertRelation(northHemi, southHemi, 0, false, false);
    assertRelation(northHemi, eastHemi, -1, true, false);
    assertRelation(northHemi, arctic80, 1, true, true);
    assertRelation(northHemi, antarctic80, 0, false, true);
    assertRelation(northHemi, candyCane, -1, true, false);

    // We can't compare northHemi3 vs. northHemi or southHemi.
    assertRelation(northHemi3, northHemi3, 1, true, false);
    assertRelation(northHemi3, eastHemi, -1, true, false);
    assertRelation(northHemi3, arctic80, 1, true, true);
    assertRelation(northHemi3, antarctic80, 0, false, true);
    assertRelation(northHemi3, candyCane, -1, true, false);

    assertRelation(southHemi, northHemi, 0, false, false);
    assertRelation(southHemi, southHemi, 1, true, false);
    assertRelation(southHemi, farHemi, -1, true, false);
    assertRelation(southHemi, arctic80, 0, false, true);
    assertRelation(southHemi, antarctic80, 1, true, true);
    assertRelation(southHemi, candyCane, -1, true, false);

    assertRelation(candyCane, northHemi, -1, true, false);
    assertRelation(candyCane, southHemi, -1, true, false);
    assertRelation(candyCane, arctic80, 0, false, true);
    assertRelation(candyCane, antarctic80, 0, false, true);
    assertRelation(candyCane, candyCane, 1, true, false);

    assertRelation(nearHemi, westHemi, -1, true, false);

    assertRelation(smallNeCw, southHemi, 1, true, false);
    assertRelation(smallNeCw, westHemi, 1, true, false);
    assertRelation(smallNeCw, northHemi, -2, true, false);
    assertRelation(smallNeCw, eastHemi, -2, true, false);

    assertRelation(loopA, loopA, 1, true, false);
    assertRelation(loopA, loopB, -1, true, false);
    assertRelation(loopA, aIntersectB, 1, true, false);
    assertRelation(loopA, aUnionB, 0, true, false);
    assertRelation(loopA, aMinusB, 1, true, false);
    assertRelation(loopA, bMinusA, 0, false, false);

    assertRelation(loopB, loopA, -1, true, false);
    assertRelation(loopB, loopB, 1, true, false);
    assertRelation(loopB, aIntersectB, 1, true, false);
    assertRelation(loopB, aUnionB, 0, true, false);
    assertRelation(loopB, aMinusB, 0, false, false);
    assertRelation(loopB, bMinusA, 1, true, false);

    assertRelation(aIntersectB, loopA, 0, true, false);
    assertRelation(aIntersectB, loopB, 0, true, false);
    assertRelation(aIntersectB, aIntersectB, 1, true, false);
    assertRelation(aIntersectB, aUnionB, 0, true, true);
    assertRelation(aIntersectB, aMinusB, 0, false, false);
    assertRelation(aIntersectB, bMinusA, 0, false, false);

    assertRelation(aUnionB, loopA, 1, true, false);
    assertRelation(aUnionB, loopB, 1, true, false);
    assertRelation(aUnionB, aIntersectB, 1, true, true);
    assertRelation(aUnionB, aUnionB, 1, true, false);
    assertRelation(aUnionB, aMinusB, 1, true, false);
    assertRelation(aUnionB, bMinusA, 1, true, false);

    assertRelation(aMinusB, loopA, 0, true, false);
    assertRelation(aMinusB, loopB, 0, false, false);
    assertRelation(aMinusB, aIntersectB, 0, false, false);
    assertRelation(aMinusB, aUnionB, 0, true, false);
    assertRelation(aMinusB, aMinusB, 1, true, false);
    assertRelation(aMinusB, bMinusA, 0, false, true);

    assertRelation(bMinusA, loopA, 0, false, false);
    assertRelation(bMinusA, loopB, 0, true, false);
    assertRelation(bMinusA, aIntersectB, 0, false, false);
    assertRelation(bMinusA, aUnionB, 0, true, false);
    assertRelation(bMinusA, aMinusB, 0, false, true);
    assertRelation(bMinusA, bMinusA, 1, true, false);
  }

  /**
   * TODO(user, ericv) Fix this test. It fails sporadically.
   * <p>
   * The problem is not in this test, it is that
   * {@link S2#robustCCW(S2Point, S2Point, S2Point)} currently requires
   * arbitrary-precision arithmetic to be truly robust. That means it can give
   * the wrong answers in cases where we are trying to determine edge
   * intersections.
   * <p>
   * It seems the strictfp modifier here in java (required for correctness in
   * other areas of the library) restricts the size of temporary registers,
   * causing us to lose some of the precision that the C++ version gets.
   * <p>
   * This test fails when it randomly chooses a cell loop with nearly colinear
   * edges. That's where S2.robustCCW provides the wrong answer. Note that there
   * is an attempted workaround in {@link S2Loop#isValid()}, but it
   * does not cover all cases.
   */
        [Test]
  public void suppressedTestLoopRelations2() {
    // Construct polygons consisting of a sequence of adjacent cell ids
    // at some fixed level. Comparing two polygons at the same level
    // ensures that there are no T-vertices.
    for (int iter = 0; iter < 1000; ++iter)
    {
        ulong num = (ulong)LongRandom();
      S2CellId begin = new S2CellId(num | 1);
      if (!begin.isValid()) {
        continue;
      }
      begin = begin.parent((int) Math.Round(rand.NextDouble() * S2CellId.MAX_LEVEL));
      S2CellId aBegin = advance(begin, skewed(6));
      S2CellId aEnd = advance(aBegin, skewed(6) + 1);
      S2CellId bBegin = advance(begin, skewed(6));
      S2CellId bEnd = advance(bBegin, skewed(6) + 1);
      if (!aEnd.isValid() || !bEnd.isValid()) {
        continue;
      }

      S2Loop a = makeCellLoop(aBegin, aEnd);
      S2Loop b = makeCellLoop(bBegin, bEnd);
      bool contained = (aBegin.lessOrEquals(bBegin) && bEnd.lessOrEquals(aEnd));
      bool intersects = (aBegin.lessThan(bEnd) && bBegin.lessThan(aEnd));
      Console.WriteLine(
          "Checking " + a.numVertices() + " vs. " + b.numVertices() + ", contained = " + contained
              + ", intersects = " + intersects);

      assertEquals(contained, a.contains(b));
      assertEquals(intersects, a.intersects(b));
    }
  }

  /**
   * Tests that nearly colinear points pass S2Loop.isValid()
   */
        [Test]
  public void testRoundingError() {
    S2Point a = new S2Point(-0.9190364081111774, 0.17231932652084575, 0.35451111445694833);
    S2Point b = new S2Point(-0.92130667053206, 0.17274500072476123, 0.3483578383756171);
    S2Point c = new S2Point(-0.9257244057938284, 0.17357332608634282, 0.3360158106235289);
    S2Point d = new S2Point(-0.9278712595449962, 0.17397586116468677, 0.32982923679138537);

      assertTrue(S2Loop.isValid(new List<S2Point>(new[] {a, b, c, d})));
  }

  /**
   * Tests {@link S2Loop#isValid()}.
   */
        [Test]
  public void testIsValid() {
    assertTrue(loopA.isValid());
    assertTrue(loopB.isValid());
    assertFalse(bowtie.isValid());
  }

  /**
   * Tests {@link S2Loop#compareTo(S2Loop)}.
   */
        [Test]
  public void testComparisons() {
    S2Loop abc = makeLoop("0:1, 0:2, 1:2");
    S2Loop abcd = makeLoop("0:1, 0:2, 1:2, 1:1");
    S2Loop abcde = makeLoop("0:1, 0:2, 1:2, 1:1, 1:0");
    assertTrue(abc.CompareTo(abcd) < 0);
    assertTrue(abc.CompareTo(abcde) < 0);
    assertTrue(abcd.CompareTo(abcde) < 0);
    assertTrue(abcd.CompareTo(abc) > 0);
    assertTrue(abcde.CompareTo(abc) > 0);
    assertTrue(abcde.CompareTo(abcd) > 0);

    S2Loop bcda = makeLoop("0:2, 1:2, 1:1, 0:1");
    assertEquals(0, abcd.CompareTo(bcda));
    assertEquals(0, bcda.CompareTo(abcd));

    S2Loop wxyz = makeLoop("10:11, 10:12, 11:12, 11:11");
    assertTrue(abcd.CompareTo(wxyz) > 0);
    assertTrue(wxyz.CompareTo(abcd) < 0);
  }

        [Test]
  public void testGetDistance() {
    // Error margin since we're doing numerical computations
    double epsilon = 1e-15;

    // A square with (lat,lng) vertices (0,1), (1,1), (1,2) and (0,2)
    // Tests the case where the shortest distance is along a normal to an edge,
    // onto a vertex
    S2Loop s1 = makeLoop("0:1, 1:1, 1:2, 0:2");

    // A square with (lat,lng) vertices (-1,1), (1,1), (1,2) and (-1,2)
    // Tests the case where the shortest distance is along a normal to an edge,
    // not onto a vertex
    S2Loop s2 = makeLoop("-1:1, 1:1, 1:2, -1:2");

    // A diamond with (lat,lng) vertices (1,0), (2,1), (3,0) and (2,-1)
    // Test the case where the shortest distance is NOT along a normal to an
    // edge
    S2Loop s3 = makeLoop("1:0, 2:1, 3:0, 2:-1");

    // All the vertices should be distance 0
    for (int i = 0; i < s1.numVertices(); i++) {
      assertEquals(0d, s1.getDistance(s1.vertex(i)).radians(), epsilon);
    }

    // A point on one of the edges should be distance 0
    assertEquals(0d, s1.getDistance(S2LatLng.fromDegrees(0.5, 1).toPoint()).radians(), epsilon);

    // In all three cases, the closest point to the origin is (0,1), which is at
    // a distance of 1 degree.
    // Note: all of these are intentionally distances measured along the
    // equator, since that makes the math significantly simpler. Otherwise, the
    // distance wouldn't actually be 1 degree.
    S2Point origin = S2LatLng.fromDegrees(0, 0).toPoint();
    assertEquals(1d, s1.getDistance(origin).degrees(), epsilon);
    assertEquals(1d, s2.getDistance(origin).degrees(), epsilon);
    assertEquals(1d, s3.getDistance(origin).degrees(), epsilon);
  }

  /**
   * This function is useful for debugging.
   */
  
  private void dumpCrossings(S2Loop loop) {

    Console.WriteLine("Ortho(v1): " + S2.ortho(loop.vertex(1)));
    Console.WriteLine("Contains(kOrigin): {0}\n", loop.contains(S2.origin()));
    for (int i = 1; i <= loop.numVertices(); ++i) {
      S2Point a = S2.ortho(loop.vertex(i));
      S2Point b = loop.vertex(i - 1);
      S2Point c = loop.vertex(i + 1);
      S2Point o = loop.vertex(i);
      Console.WriteLine("Vertex {0}: [%.17g, %.17g, %.17g], "
          + "%d%dR=%d, %d%d%d=%d, R%d%d=%d, inside: %b\n",
          i,
          loop.vertex(i).x,
          loop.vertex(i).y,
          loop.vertex(i).z,
          i - 1,
          i,
          S2.robustCCW(b, o, a),
          i + 1,
          i,
          i - 1,
          S2.robustCCW(c, o, b),
          i,
          i + 1,
          S2.robustCCW(a, o, c),
          S2.orderedCCW(a, b, c, o));
    }
    for (int i = 0; i < loop.numVertices() + 2; ++i) {
      S2Point orig = S2.origin();
      S2Point dest;
      if (i < loop.numVertices()) {
        dest = loop.vertex(i);
        Console.WriteLine("Origin->{0} crosses:", i);
      } else {
        dest = new S2Point(0, 0, 1);
        if (i == loop.numVertices() + 1) {
          orig = loop.vertex(1);
        }
        Console.WriteLine("Case {0}:", i);
      }
      for (int j = 0; j < loop.numVertices(); ++j) {
        Console.WriteLine(
            " " + S2EdgeUtil.edgeOrVertexCrossing(orig, dest, loop.vertex(j), loop.vertex(j + 1)));
      }
      Console.WriteLine();
    }
    for (int i = 0; i <= 2; i += 2) {
      Console.WriteLine("Origin->v1 crossing v{0}->v1: ", i);
      S2Point a = S2.ortho(loop.vertex(1));
      S2Point b = loop.vertex(i);
      S2Point c = S2.origin();
      S2Point o = loop.vertex(1);
      Console.WriteLine("{0}1R={1}, M1{2}={3}, R1M={4}, crosses: {5}\n",
          i,
          S2.robustCCW(b, o, a),
          i,
          S2.robustCCW(c, o, b),
          S2.robustCCW(a, o, c),
          S2EdgeUtil.edgeOrVertexCrossing(c, o, b, a));
    }
  }
    }
}