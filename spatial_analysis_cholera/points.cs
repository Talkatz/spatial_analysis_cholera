using Microsoft.Office.Interop.Excel;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace spatial_analysis_cholera
{
    // a class I made for handling coordinates 
    public class Point
    {
        public double x;
        public double y;
        public Point(double xCord, double yCord)
        {
            x = xCord;
            y = yCord;
        }

        // a method the returns the distance between two points
        public static double returnPointsDistance (Point one, Point two)
        {
            double dist = Math.Sqrt(Math.Pow(one.x - two.x, 2) + Math.Pow(one.y - two.y, 2));
            return dist;
        }

        // a method that makes a rectangle that holds all the points and returns its area
        public static double rectangleArea (List<Point> pntList)
        {
            double lowest_x = pntList.Min(point => point.x);
            double lowest_y = pntList.Min(point => point.y);
            double highest_x = pntList.Max(point => point.x);
            double highest_y = pntList.Max(point => point.y);

            Point north_west = new Point(lowest_x, highest_y);
            Point north_east = new Point(highest_x, highest_y);
            Point south_west = new Point(lowest_x, lowest_y);
            Point south_east = new Point(highest_x, lowest_y);

            double areaSqrm = (highest_y - lowest_y) * (highest_x - lowest_x);
            return areaSqrm;
        }

        // a method that returns the nearest neighbour index, that tells if a set of points is clustered or dispersed randomly \ regularly
        public static double nearestNeighbourIndex(List<Point> addressesPntList, double areaSqrm)
        {
            List<double> minDistListPoints = new List<double>();
            List<double> minDistPerPoint = new List<double>();
            for (int i = 0; i < addressesPntList.Count; i++)
            {
                double dist = 0;
                for (int j = 0; j < addressesPntList.Count; j++)
                {
                    if (addressesPntList[i] != addressesPntList[j])
                    {
                        dist = Point.returnPointsDistance(addressesPntList[i], addressesPntList[j]);
                        minDistPerPoint.Add(dist);
                    }
                }
                minDistListPoints.Add(minDistPerPoint.Min());
                minDistPerPoint.Clear();
            }

            double avgLowestDistace = minDistListPoints.Average();
            double density = areaSqrm / minDistListPoints.Count;
            double nearestNeighborIndex = avgLowestDistace / (0.5 * Math.Sqrt(density));
            return nearestNeighborIndex;
        }

        // a method that returns the average distance from a point to a list of points
        public static double distanceFromPump(double input_xCord, double input_yCord, List<Point> addressesPntList)
        {
            List<double> distList = new List<double>();
            double average_distance = (-1);
            Point pnt = new Point(input_xCord, input_yCord);
            try
            {
                for (int i = 0; i < addressesPntList.Count; i++)
                {
                    double distToPump = 0;
                    try
                    {
                        distToPump = Point.returnPointsDistance(addressesPntList[i], pnt);
                        distList.Add(distToPump);
                    }
                    catch (Exception ex)
                    {
                        // do something with the error
                        continue;
                    }
                }
                average_distance = distList.Average();
            }
            catch (Exception ex)
            {
                // do something with the error
                return average_distance;
            }
            return average_distance;
        }


    }
}
