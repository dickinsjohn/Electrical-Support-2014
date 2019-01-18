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
    class Intersectors
    {
        public static View3D view3D;
        
        //method to extend the rod height to structure above
        public static double StructureHeight(XYZ point, Document m_document)
        {
            try
            {
                double leastProximity = 0;

                //create an instance of the refernce intersector and make it to intersect in Elements and revit links
                ReferenceIntersector intersector = new ReferenceIntersector(view3D);
                intersector.TargetType = FindReferenceTarget.Element;
                intersector.FindReferencesInRevitLinks = true;

                //find the points of intersection
                IList<ReferenceWithContext> referenceWithContext = intersector.Find(point, XYZ.BasisZ);

                //remove the intersection on the element inside which the point lies
                for (int i = 0; i < referenceWithContext.Count; i++)
                {
                    if (m_document.GetElement(referenceWithContext[i].GetReference()).GetType().Name != "RevitLinkInstance")
                    {
                        referenceWithContext.RemoveAt(i);
                        i--;
                    }
                }

                if (referenceWithContext.Count != 0)
                {
                    //find the least proximity
                    leastProximity = referenceWithContext.First().Proximity;

                    for (int i = 0; i < referenceWithContext.Count; i++)
                    {
                        if (leastProximity > referenceWithContext[i].Proximity)
                        {
                            leastProximity = referenceWithContext[i].Proximity;
                        }
                    }
                }
                if (leastProximity <= 10)
                    return leastProximity;
                else
                    return 0;
            }
            catch
            {
                throw new Exception();
            }
        }


        //method to find parallel trays/ducts from the list
        public static List<Element> ParallelElements(XYZ point, Document m_document, ICollection<ElementId> parallels)
        {
            try
            {
                List<Element> intersected = new List<Element>();

                //create an instance of the refernce intersector and make it to intersect in Elements in list
                ReferenceIntersector intersector = new ReferenceIntersector(parallels, FindReferenceTarget.Element, view3D);
                intersector.FindReferencesInRevitLinks = false;

                //find the points of intersection in upward direction
                IList<ReferenceWithContext> referContUp = intersector.Find(point, XYZ.BasisZ);

                //find the points of intersection in downward direction
                IList<ReferenceWithContext> referContDown = intersector.Find(point, -XYZ.BasisZ);

                for (int i = 0; i < referContUp.Count; i++)
                {
                    if ((m_document.GetElement(referContUp[i].GetReference()) is CableTray)
                        ||(m_document.GetElement(referContUp[i].GetReference()) is Duct))
                    {
                       intersected.Add(m_document.GetElement(referContUp[i].GetReference()));
                    }
                }
                
                for (int i = 0; i < referContDown.Count; i++)
                {
                    if ((m_document.GetElement(referContDown[i].GetReference()) is CableTray)
                        || (m_document.GetElement(referContDown[i].GetReference()) is Duct))
                    {
                        intersected.Add(m_document.GetElement(referContDown[i].GetReference()));
                    }
                }

                intersected = intersected.Distinct().ToList();

                return intersected;
            }
            catch
            {
                throw new Exception();
            }
        }


        //project a point to a line
        public static XYZ Projectpoint(XYZ originPoint, Line line)
        {
            try
            {
                IntersectionResult intersectionResult = new IntersectionResult();

                XYZ projectedPoint = null;

                intersectionResult = line.Project(originPoint);
                projectedPoint = intersectionResult.XYZPoint;

                return projectedPoint;
            }
            catch
            {
                throw new Exception();
            }
        }


        //method to create a new point from given points
        public static XYZ CreatePoint(XYZ widest, XYZ highest)
        {
            return new XYZ(widest.X,widest.Y, highest.Z);
        }
    }
}
