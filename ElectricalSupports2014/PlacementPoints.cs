using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using Autodesk.Revit;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using Autodesk.Revit.DB.Electrical;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.DB.Mechanical;
using Autodesk.Revit.DB.Structure;

namespace ElectricalSupports2014
{
    public static class Points
    {
        const double _eps = 1.0e-9;
        const double spacing = 1500;

        public static List<CompletedTrays> createdList = new List<CompletedTrays>(0);

        //method to return the point nearest to the passes point in the created list
        private static XYZ nearestPoint(XYZ checkPoint, List<XYZ> createdPoints)
        {
            try
            {
                XYZ nearest = createdPoints.First();

                double distance = checkPoint.DistanceTo(createdPoints.First());

                for (int i = 0; i < createdPoints.Count; i++)
                {
                    if (distance > checkPoint.DistanceTo(createdPoints[i]))
                    {
                        distance = checkPoint.DistanceTo(createdPoints[i]);
                        nearest = createdPoints[i];
                    }
                }
                return nearest;
            }
            catch
            {
                throw new Exception();
            }
        }


        //check if the point passed exist in the list
        private static bool IfExist(XYZ checkPoint, List<XYZ> createdPoints)
        {
            try
            {
                for (int i = 0; i < createdPoints.Count; i++)
                {
                    if (checkPoint.DistanceTo(createdPoints[i]) < _eps)
                    {
                        return true;
                    }
                }
                return false;
            }
            catch
            {
                throw new Exception();
            }
        }


        //method to get points other than already created points
        public static List<XYZ> GetOtherPoints(Curve trayDuctCurve, List<XYZ> createdPoints)
        {
            try
            {
                List<XYZ> points = new List<XYZ>();
                
                XYZ start = trayDuctCurve.GetEndPoint(0), end = trayDuctCurve.GetEndPoint(1);
                
                XYZ nearStart = nearestPoint(start, createdPoints), nearEnd = nearestPoint(end, createdPoints);
                
                points.AddRange(ComputePoints(start,nearStart));
               
                if(!IfExist(start,createdPoints))
                    points.Add(start);
              
                points.AddRange(ComputePoints(end, nearEnd));
               
                if (!IfExist(end, createdPoints))
                    points.Add(end);
                
                List<XYZ> sortedList = SortPoints(start, createdPoints);
                
                for (int i = 0; i < sortedList.Count - 1; i++)
                {
                    if (sortedList[i].DistanceTo(sortedList[i + 1]) > 0.00328084 * spacing)
                    {
                        points.AddRange(ComputePoints(sortedList[i],sortedList[i+1]));
                    }
                }
                return points;
            }
            catch
            {
                throw new Exception();
            }
        }


        //sort a list of points
        public static List<XYZ> SortPoints(XYZ start, List<XYZ> createdPoints)
        {
            try
            {
                if (createdPoints != null || createdPoints.Count != 0)
                {
                    List<XYZ> sortedPoints = createdPoints.OrderBy(x => x.DistanceTo(start)).ToList();
                    return sortedPoints;
                }
                else
                    return null;
            }
            catch
            {
                throw new Exception();
            }
        }


        //compute the points for lines at the ends
        private static List<XYZ> ComputePoints(XYZ point, XYZ nearPoint)
        {
            try
            {
                List<XYZ> points = new List<XYZ>();

                if (point.DistanceTo(nearPoint) > _eps)
                {
                    Line trayDuctLine = Line.CreateBound(nearPoint, point);
                    double length = 304.8 * trayDuctLine.Length;

                    if (point.DistanceTo(nearPoint) > 0.00328084 * spacing)
                    {
                        //compute number of splits required
                        int splits = (int)length / (int)spacing;

                        if (((double)splits - ((double)length / (double)spacing)) != 0)
                            ++splits;

                        for (int i = 1; i < splits; i++)
                        {
                            points.Add(trayDuctLine.Evaluate((double)i / (double)splits, true));
                        }
                    }
                }

                return points;
            }
            catch
            {
                throw new Exception();
            }
        }


        //method to get the points for family placement
        public static List<XYZ> PlacementPoints(Curve trayDuctCurve, double minSpacing)
        {
            try
            {
                List<XYZ> points = new List<XYZ>();

                double length = 304.8 * trayDuctCurve.Length;
                
                //check whether the spacing is less than or greater than length of pipe
                if (length < spacing)
                {
                    //check if spacing minus offsets will be less than or 
                    //greater than minimum spacing between pipes
                    if ((length) < minSpacing)
                    {
                        points.Add(trayDuctCurve.Evaluate(0.5, true));
                    }
                    else
                    {
                        points.Add(trayDuctCurve.Evaluate(0, true));
                        points.Add(trayDuctCurve.Evaluate(1, true));
                    }
                }
                else
                {
                    //compute number of splits required
                    int splits = (int)length / (int)spacing;

                    if(((double)splits-((double)length/(double)spacing))!=0)
                        ++splits;

                    for (int i=0; i <= splits; i++)
                    {
                        points.Add(trayDuctCurve.Evaluate((double)i/(double)splits, true));
                    }
                }

                return points;
            }
            catch
            {
                throw new Exception();
            }            
        }


        //method to get points on bothsides of the placement points
        public static ReferPoints FindRodPoints(XYZ instPoint, Element elem)
        {
            try
            {
                ReferPoints twoPoints = new ReferPoints();
                twoPoints.left = null;
                twoPoints.right = null;

                Curve eleCurve = ((LocationCurve)elem.Location).Curve;
                XYZ start = eleCurve.GetEndPoint(0), end = eleCurve.GetEndPoint(1);

                double eleWidth = 0, eleHeight = 0;

                if (eleCurve is Line)
                {
                    Line eleLine = (Line)eleCurve;

                    if (elem is CableTray)
                    {
                        eleWidth = ((CableTray)elem).Width;
                        eleHeight = ((CableTray)elem).Height;
                    }
                    else if (elem is Duct)
                    {
                        eleWidth = ((Duct)elem).Width;
                        eleHeight = ((Duct)elem).Height;
                    }

                    double yAngle = XYZ.BasisY.AngleTo(eleLine.Direction);

                    double x = (eleWidth + 0.00328084 * 25) * Math.Cos(yAngle), y = (eleWidth + 0.00328084 * 25) * Math.Sin(yAngle);

                    if (end.Y > start.Y && end.X > start.X
                        || start.Y > end.Y && start.X > end.X)
                    {
                        twoPoints.left = new XYZ(instPoint.X - x, instPoint.Y + y, instPoint.Z - eleHeight / 2);
                        twoPoints.right = new XYZ(instPoint.X + x, instPoint.Y - y, instPoint.Z - eleHeight / 2);
                    }
                    else if (end.Y < start.Y && end.X > start.X
                        || start.Y < end.Y && start.X > end.X)
                    {
                        twoPoints.right = new XYZ(instPoint.X - x, instPoint.Y + y, instPoint.Z - eleHeight / 2);
                        twoPoints.left = new XYZ(instPoint.X + x, instPoint.Y - y, instPoint.Z - eleHeight / 2);
                    }
                    else if (start.X == end.X)
                    {
                        twoPoints.left = new XYZ(instPoint.X, instPoint.Y + y, instPoint.Z - eleHeight / 2);
                        twoPoints.right = new XYZ(instPoint.X, instPoint.Y - y, instPoint.Z - eleHeight / 2);
                    }
                    else if (start.Y == end.Y)
                    {
                        twoPoints.left = new XYZ(instPoint.X - x, instPoint.Y, instPoint.Z - eleHeight / 2);
                        twoPoints.right = new XYZ(instPoint.X + x, instPoint.Y, instPoint.Z - eleHeight / 2);
                    }
                }

                if (twoPoints.left != null && twoPoints.right != null)
                {
                    return twoPoints;
                }
                return twoPoints;
            }
            catch
            {
                throw new Exception();
            }
        }


        //method to remove the least width element from created list
        public static void RemoveCreated(ElementId id)
        {
            if (createdList.Count != 0)
            {
                for (int i = 0; i < createdList.Count; i++)
                {
                    if (createdList[i].eId == id)
                    {
                        createdList.RemoveAt(i);
                        return;
                    }
                }
            }
        }


        //method to ad element with point to the created list of points
        public static void AddCreatedRecord(ElementId id, XYZ point)
        {
            try
            {
                CompletedTrays temCreated = new CompletedTrays(0);
                temCreated.eId = id;
                temCreated.points.Add(point);

                if (createdList.Count != 0)
                {
                    for (int i = 0; i < createdList.Count; i++)
                    {
                        if (createdList[i].eId == id)
                        {
                            createdList[i].points.Add(point);
                            return;
                        }
                    }

                    createdList.Add(temCreated);
                }
                else
                    createdList.Add(temCreated);
            }
            catch
            {
                throw new Exception();
            }
        }


        //method to return created record
        public static List<XYZ> GetCreatedPoints(ElementId id)
        {
            try
            {
                if (createdList.Count != 0)
                {
                    for (int i = 0; i < createdList.Count; i++)
                    {
                        if (createdList[i].eId == id && createdList[i].points.Count != 0)
                        {
                            return createdList[i].points;
                        }
                    }
                }

                return null;
            }
            catch
            {
                throw new Exception();
            }
        }


        //method to check if supports are required at the point passed
        public static bool CheckIfRequired(XYZ point, List<XYZ> sortedList)
        {
            try
            {
                for (int i = 0; i < sortedList.Count - 1; i++)
                {
                    if (sortedList[i].DistanceTo(point) < sortedList[i].DistanceTo(sortedList[i + 1])
                        && sortedList[i + 1].DistanceTo(point) < sortedList[i].DistanceTo(sortedList[i + 1]))
                    {
                        return false;
                    }
                }

                return true;
            }
            catch
            {
                throw new Exception();
            }
        }
    }
}
