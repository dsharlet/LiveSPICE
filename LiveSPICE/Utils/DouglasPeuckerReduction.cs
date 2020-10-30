using System;
using System.Collections.Generic;
using System.Windows;

namespace LiveSPICE
{
    // http://www.codeproject.com/Articles/18936/A-C-Implementation-of-Douglas-Peucker-Line-Approxi
    public class DouglasPeuckerReduction
    {
        public static void Reduce(List<Point> Points, double Tolerance)
        {
            if (Points == null || Points.Count < 3)
                return;

            int firstPoint = 0;
            int lastPoint = Points.Count - 1;
            List<int> pointIndexsToKeep = new List<int>(Points.Count)
            {
                //Add the first and last index to the keepers
                firstPoint,
                lastPoint
            };

            //The first and the last point cannot be the same
            while (Points[firstPoint].Equals(Points[lastPoint]))
                lastPoint--;

            Reduce(Points, firstPoint, lastPoint, Tolerance, ref pointIndexsToKeep);

            // We should only be removing points, so this is safe.
            pointIndexsToKeep.Sort();
            for (int i = 0; i < pointIndexsToKeep.Count; i++)
                Points[i] = Points[pointIndexsToKeep[i]];
            Points.RemoveRange(pointIndexsToKeep.Count, Points.Count - pointIndexsToKeep.Count);
        }

        /// <span class="code-SummaryComment"><summary></span>
        /// Douglases the peucker reduction.
        /// <span class="code-SummaryComment"></summary></span>
        /// <span class="code-SummaryComment"><param name="points">The points.</param></span>
        /// <span class="code-SummaryComment"><param name="firstPoint">The first point.</param></span>
        /// <span class="code-SummaryComment"><param name="lastPoint">The last point.</param></span>
        /// <span class="code-SummaryComment"><param name="tolerance">The tolerance.</param></span>
        /// <span class="code-SummaryComment"><param name="pointIndexsToKeep">The point index to keep.</param></span>
        private static void Reduce(List<Point> points, int firstPoint, int lastPoint, double tolerance, ref List<int> pointIndexsToKeep)
        {
            double maxDistance = 0;
            int indexFarthest = 0;

            for (int index = firstPoint; index < lastPoint; index++)
            {
                double distance = PerpendicularDistanceSq(points[firstPoint], points[lastPoint], points[index]);
                if (distance > maxDistance)
                {
                    maxDistance = distance;
                    indexFarthest = index;
                }
            }

            if (maxDistance > tolerance && indexFarthest != 0)
            {
                //Add the largest point that exceeds the tolerance
                pointIndexsToKeep.Add(indexFarthest);

                Reduce(points, firstPoint, indexFarthest, tolerance, ref pointIndexsToKeep);
                Reduce(points, indexFarthest, lastPoint, tolerance, ref pointIndexsToKeep);
            }
        }

        /// <span class="code-SummaryComment"><summary></span>
        /// The distance of a point from a line made from point1 and point2.
        /// <span class="code-SummaryComment"></summary></span>
        /// <span class="code-SummaryComment"><param name="pt1">The PT1.</param></span>
        /// <span class="code-SummaryComment"><param name="pt2">The PT2.</param></span>
        /// <span class="code-SummaryComment"><param name="p">The p.</param></span>
        /// <span class="code-SummaryComment"><returns></returns></span>
        public static double PerpendicularDistanceSq(Point Point1, Point Point2, Point Point)
        {
            //Vector n = Point2 - Point1;

            //Vector ap = Point1 - Point;

            //return (ap - (Vector.Multiply(ap, n) * n)).LengthSquared;

            //Area = |(1/2)(x1y2 + x2y3 + x3y1 - x2y1 - x3y2 - x1y3)|   *Area of triangle
            //Base = v((x1-x2)²+(x1-x2)²)                               *Base of Triangle*
            //Area = .5*Base*H                                          *Solve for height
            //Height = Area/.5/Base

            double areaSq = Square(
                Point1.X * Point2.Y + Point2.X * Point.Y + 
                Point.X * Point1.Y - Point2.X * Point1.Y -
                Point.X * Point2.Y - Point1.X * Point.Y);
            double bottomSq = Square(Point1.X - Point2.X) + Square(Point1.Y - Point2.Y);
            return areaSq / bottomSq;
        }

        private static double Square(double x) { return x * x; }
    }
}