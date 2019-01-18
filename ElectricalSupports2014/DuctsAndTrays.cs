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
    class DuctsAndTrays
    {
        public static List<Element> trayDuctlist;

        //constructor for class
        static DuctsAndTrays()
        {
            trayDuctlist = new List<Element>();
        }
        
        
        //method to find the tray or duct with least width
        public Element LeastWidth()
        {
            try
            {
                Element thinnest = null;

                CableTray thinnestTray = ThinnestTray();
                Duct thinnestDuct = ThinnestDuct();
                if (thinnestDuct != null && thinnestTray != null)
                {
                    if (thinnestDuct.Width <= thinnestTray.Width)
                        thinnest = thinnestDuct as Element;
                    else
                        thinnest = thinnestTray as Element;
                    return thinnest;
                }
                else if (thinnestDuct == null && thinnestTray != null)
                {
                    thinnest = thinnestTray as Element;
                    return thinnest;
                }
                else if (thinnestDuct != null && thinnestTray == null)
                {
                    thinnest = thinnestDuct as Element;
                    return thinnest;
                }

                return thinnest;
            }
            catch
            {
                throw new Exception();
            }
        }

        //method to get thinnest duct element
        private Duct ThinnestDuct()
        {
            try
            {
                Duct thinDuct = null;

                foreach (Element e in trayDuctlist)
                {
                    if (e is Duct)
                    {
                        thinDuct = e as Duct;
                        break;
                    }
                }

                foreach (Element e in trayDuctlist)
                {
                    if (e is Duct)
                    {
                        if (((Duct)e).Width < thinDuct.Width)
                        {
                            thinDuct = e as Duct;
                        }
                    }
                }

                return thinDuct;
            }
            catch
            {
                throw new Exception();
            }
        }

        //method to get thinnest tray element
        private CableTray ThinnestTray()
        {
            try
            {
                CableTray thinTray = null;

                foreach (Element e in trayDuctlist)
                {
                    if (e is CableTray)
                    {
                        thinTray = e as CableTray;
                        break;
                    }
                }

                foreach (Element e in trayDuctlist)
                {
                    if (e is CableTray)
                    {
                        if (((CableTray)e).Width < thinTray.Width)
                        {
                            thinTray = e as CableTray;
                        }
                    }
                }

                return thinTray;
            }
            catch
            {
                throw new Exception();
            }
        }
        
        //method to find the highest tray
        public static Element HighestElement(List<Element> eleList)
        {
            try
            {
                Element highest = null;

                foreach (Element e in eleList)
                {
                    if ((e is CableTray) || (e is Duct))
                    {
                        highest = e;
                        break;
                    }
                }

                foreach (Element e in eleList)
                {
                    if ((e is CableTray) || (e is Duct))
                    {
                        XYZ maxTemp = null, temp = null;

                        if (((LocationCurve)highest.Location).Curve.GetEndPoint(0).Z >
                            ((LocationCurve)highest.Location).Curve.GetEndPoint(1).Z)
                        {
                            maxTemp = ((LocationCurve)highest.Location).Curve.GetEndPoint(0);
                        }
                        else
                        {
                            maxTemp = ((LocationCurve)highest.Location).Curve.GetEndPoint(1);
                        }

                        if (((LocationCurve)e.Location).Curve.GetEndPoint(0).Z >
                            ((LocationCurve)e.Location).Curve.GetEndPoint(1).Z)
                        {
                            temp = ((LocationCurve)e.Location).Curve.GetEndPoint(0);
                        }
                        else
                        {
                            temp = ((LocationCurve)e.Location).Curve.GetEndPoint(1);
                        }

                        if (maxTemp.Z < temp.Z)
                        {
                            highest = e;
                        }
                    }
                }

                return highest;
            }
            catch
            {
                throw new Exception();
            }
        }


        //method to find the highest tray
        public static Element LowestElement(List<Element> eleList)
        {
            try
            {
                Element lowest = null;

                foreach (Element e in eleList)
                {
                    if ((e is CableTray) || (e is Duct))
                    {
                        lowest = e;
                        break;
                    }
                }

                foreach (Element e in eleList)
                {
                    if ((e is CableTray) || (e is Duct))
                    {
                        XYZ minTemp = null, temp = null;

                        if (((LocationCurve)lowest.Location).Curve.GetEndPoint(0).Z <
                            ((LocationCurve)lowest.Location).Curve.GetEndPoint(1).Z)
                        {
                            minTemp = ((LocationCurve)lowest.Location).Curve.GetEndPoint(0);
                        }
                        else
                        {
                            minTemp = ((LocationCurve)lowest.Location).Curve.GetEndPoint(1);
                        }

                        if (((LocationCurve)e.Location).Curve.GetEndPoint(0).Z <
                            ((LocationCurve)e.Location).Curve.GetEndPoint(1).Z)
                        {
                            temp = ((LocationCurve)e.Location).Curve.GetEndPoint(0);
                        }
                        else
                        {
                            temp = ((LocationCurve)e.Location).Curve.GetEndPoint(1);
                        }

                        if (minTemp.Z > temp.Z)
                        {
                            lowest = e;
                        }
                    }
                }

                return lowest;
            }
            catch
            {
                throw new Exception();
            }
        }


        //method to find all the parallel tray/duct to element
        public ICollection<ElementId> ParallelElements(Document m_doc, Element ele)
        {
            try
            {
                const double _eps = 1.0e-9;

                ICollection<ElementId> parallel = new List<ElementId>();

                Line eleLine = ((LocationCurve)ele.Location).Curve as Line;

                XYZ eleNormal = (eleLine.GetEndPoint(0) - eleLine.GetEndPoint(1)).CrossProduct(XYZ.BasisZ).Normalize();

                foreach (Element e in trayDuctlist)
                {
                    if (ele != e)
                    {
                        Line tempLine = ((LocationCurve)e.Location).Curve as Line;

                        XYZ normal = (tempLine.GetEndPoint(0) - tempLine.GetEndPoint(1)).CrossProduct(XYZ.BasisZ).Normalize();

                        double angle = normal.AngleTo(eleNormal);

                        if ((_eps > angle || Math.Abs(angle - Math.PI) < _eps)
                            && eleLine.GetEndPoint(0).Z != tempLine.GetEndPoint(0).Z)
                        {
                            parallel.Add(e.Id);
                        }
                    }
                }
                return parallel;
            }
            catch
            {
                throw new Exception();
            }
        }

        //method to find maximum width of the tray/duct from list
        public static Element MaxWidthElement(List<Element> eleList)
        {
            try
            {
                Element maxWidthEle = null;

                CableTray maxTray = MaxWidthTray(eleList);
                Duct maxDuct = MaxWidthDuct(eleList);

                if (maxDuct != null && maxTray != null)
                {

                    if (maxDuct.Width >= maxTray.Width)
                        maxWidthEle = maxDuct as Element;
                    else
                        maxWidthEle = maxTray as Element;
                    return maxWidthEle;
                }
                else if (maxDuct == null && maxTray != null)
                {
                    maxWidthEle = maxTray as Element;
                    return maxWidthEle;
                }
                else if (maxDuct != null && maxTray == null)
                {
                    maxWidthEle = maxDuct as Element;
                    return maxWidthEle;
                }

                return maxWidthEle;
            }
            catch
            {
                throw new Exception();
            }
        }


        //method to get thinnest duct element
        private static Duct MaxWidthDuct(List<Element> eleList)
        {
            try
            {
                Duct maxDuct = null;

                foreach (Element e in eleList)
                {
                    if (e is Duct)
                    {
                        maxDuct = e as Duct;
                        break;
                    }
                }

                foreach (Element e in eleList)
                {
                    if (e is Duct)
                    {
                        if (((Duct)e).Width > maxDuct.Width)
                        {
                            maxDuct = e as Duct;
                        }
                    }
                }

                return maxDuct;
            }
            catch
            {
                throw new Exception();
            }
        }

        //method to get thinnest tray element
        private static CableTray MaxWidthTray(List<Element> eleList)
        {
            try
            {
                CableTray maxTray = null;

                foreach (Element e in eleList)
                {
                    if (e is CableTray)
                    {
                        maxTray = e as CableTray;
                        break;
                    }
                }

                foreach (Element e in eleList)
                {
                    if (e is CableTray)
                    {
                        if (((CableTray)e).Width > maxTray.Width)
                        {
                            maxTray = e as CableTray;
                        }
                    }
                }

                return maxTray;
            }
            catch
            {
                throw new Exception();
            }
        }

        //find width of the element
        public static double GetWidth(Element maxWidth)
        {
            try
            {
                double width = 0.0;
                if (maxWidth is CableTray)
                {
                    width = ((CableTray)maxWidth).Width;
                }
                else if (maxWidth is Duct)
                {
                    width = ((Duct)maxWidth).Width;
                }
                return width;
            }
            catch
            {
                throw new Exception();
            }
        }

        //method to return the height of the tray
        public static double TrayDuctHeight(Element ele)
        {
            try
            {
                if (ele is CableTray)
                    return ((CableTray)ele).Height / 2;
                else if (ele is Duct)
                    return ((Duct)ele).Height / 2;
                else
                    return 0.0;
            }
            catch
            {
                throw new Exception();
            }
        }
    }
}
