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
    class Execution
    {
        static Document doc;

        double _eps = 1.0e-3;

        string familyName;

        DuctsAndTrays ducttray= new DuctsAndTrays();

        double minSpacing;

        //constructor for the class
        public Execution()
        {
            doc = null;
        }

        //methdo to execute all the other methods
        public Execution(Document tempDoc, ElementSet tempSelected, string name, int minSpace)
        {
            doc = tempDoc; 
            familyName = name;
            minSpacing = minSpace;

            ManageView view = new ManageView(doc);

            Intersectors.view3D = ManageView.view3D;

            DuctsAndTrays.trayDuctlist = ConverToList(tempSelected);

            CreateAndPlace createAndPlace = new CreateAndPlace(doc, familyName);
            
            using (TransactionGroup tx = new TransactionGroup(doc, "Support Generation"))
            {
                tx.Start();

                view.GetBoundsDetail();
                
                LeastWidthPoints();

                view.SetBounds();

                tx.Assimilate();
            }
            
        }

        //methdo to convert element set into a list of elements with tays and ducts only
        private List<Element> ConverToList(ElementSet eSet)
        {
            try
            {
                List<Element> eleList = new List<Element>();

                foreach (Element e in eSet)
                {
                    if ((e is CableTray) || (e is Duct))
                    {
                        eleList.Add(e);
                    }
                }
                return RemoveVertical(eleList);
            }
            catch
            {
                throw new Exception();
            }
        }

        //method to remove all vertical elements from list
        private List<Element> RemoveVertical(List<Element> inList)
        {
            List<Element> outList = new List<Element>();
            
            try
            {
                foreach (Element e in inList)
                {
                    Curve curve = ((LocationCurve)e.Location).Curve;

                    if (Math.Abs(curve.GetEndPoint(0).X-curve.GetEndPoint(1).X) > _eps || Math.Abs(curve.GetEndPoint(0).Y - curve.GetEndPoint(1).Y) >_eps)
                    {
                        outList.Add(e);
                    }
                }
                return outList;
            }
            catch
            {
                throw new Exception();
            }
        }

        //recursive method to find least width element and find points required
        private void LeastWidthPoints()
        {
            try
            {
                bool flag = false;
                List<XYZ> placementPoints = null;

                Element leastWidth = ducttray.LeastWidth();

                //check if least width element exist in the created element list
                if (Points.createdList.Count != 0)
                {
                    foreach (CompletedTrays tempCom in Points.createdList)
                    {
                        if (tempCom.eId == leastWidth.Id)
                        {
                            flag = true;
                            break;
                        }
                    }
                }

                if (flag != true)
                {
                    placementPoints = Points.PlacementPoints(((LocationCurve)leastWidth.Location).Curve, minSpacing);
                    InterParallel(placementPoints, leastWidth);
                }
                else
                {
                    placementPoints = Points.GetOtherPoints(((LocationCurve)leastWidth.Location).Curve, Points.GetCreatedPoints(leastWidth.Id));
                    InterParallel(placementPoints, leastWidth);
                }

                Points.RemoveCreated(leastWidth.Id);
                DuctsAndTrays.trayDuctlist.Remove(leastWidth);

                if (DuctsAndTrays.trayDuctlist.Count != 0)
                    LeastWidthPoints();
                else
                    return;
            }
            catch
            {
                throw new Exception();
            }
        }

        //method to make choice based on availability of parallel trays with no intersection/ parallel with intersecting trays
        //or no parallel trays
        private void InterParallel(List<XYZ> placementPoints, Element leastWidth)
        {
            try
            {
                ICollection<ElementId> parallels = ducttray.ParallelElements(doc, leastWidth);

                List<Element> interParallel = new List<Element>();

                foreach (XYZ point in placementPoints)
                {
                    if (parallels == null || parallels.Count == 0)
                    {
                        AdjustInstance.height = Intersectors.StructureHeight(point, doc) + DuctsAndTrays.TrayDuctHeight(leastWidth);

                        CreateAdjustElement(leastWidth, point, point, DuctsAndTrays.GetWidth(leastWidth), true, 0.0);
                        continue;
                    }
                    else
                    {
                        interParallel = Intersectors.ParallelElements(point, doc, parallels);

                        Element maxWidth = null, lowest = null;
                        XYZ widthProject = null, lowestProject;

                        if (interParallel.Count != 0)
                        {
                            interParallel.Add(leastWidth);

                            maxWidth = DuctsAndTrays.MaxWidthElement(interParallel);                            
                            lowest = DuctsAndTrays.LowestElement(interParallel);
                            
                            lowestProject = Intersectors.Projectpoint(point, ((LocationCurve)maxWidth.Location).Curve as Line);                            
                            AdjustInstance.height = Intersectors.StructureHeight(lowestProject, doc) + DuctsAndTrays.TrayDuctHeight(lowest);
                            
                            widthProject = Intersectors.Projectpoint(point, ((LocationCurve)maxWidth.Location).Curve as Line);
                            
                            PlaceOnParallel(interParallel, widthProject, DuctsAndTrays.GetWidth(maxWidth), true, null, 0.0);
                        }
                        else
                        {
                            AdjustInstance.height = Intersectors.StructureHeight(point, doc) + DuctsAndTrays.TrayDuctHeight(leastWidth);

                            CreateAdjustElement(leastWidth, point, point, DuctsAndTrays.GetWidth(leastWidth), true, 0.0);
                            continue;
                        }
                    }
                }
            }
            catch
            {
                throw new Exception();
            }
        }
        
        //recursive method to check if points are required at the location or not in case of parallel and intersecting trays/ducts
        private void PlaceOnParallel(List<Element> interParallel, XYZ widthProject, double width, bool first, XYZ above, double aboveHeight)
        {
            try
            {
                Element highest = DuctsAndTrays.HighestElement(interParallel);
                XYZ highProject = Intersectors.Projectpoint(widthProject, ((LocationCurve)highest.Location).Curve as Line);

                double height = DuctsAndTrays.TrayDuctHeight(highest);

                bool required = true;

                if (Points.GetCreatedPoints(highest.Id) != null)
                {
                    List<XYZ> sortedList = Points.SortPoints((((LocationCurve)highest.Location).Curve as Line).GetEndPoint(0), Points.GetCreatedPoints(highest.Id));

                    required = Points.CheckIfRequired(highProject, sortedList);
                }

                if (required)
                {
                    if (first != true && above != null)
                    {
                        XYZ instancePoint = new XYZ(widthProject.X, widthProject.Y, highProject.Z);
                        CreateAdjustElement(highest, instancePoint, highProject, width, false, above.Z - highProject.Z + height - aboveHeight);
                    }
                    else
                    {
                        XYZ instancePoint = new XYZ(widthProject.X, widthProject.Y, highProject.Z);
                        CreateAdjustElement(highest, instancePoint, highProject, width, true, 0.0);
                    }
                }
                else
                {
                    if (first != true)
                    {
                        highProject = above;
                        height = aboveHeight;
                    }
                    else
                        highProject = null;
                }

                interParallel.Remove(highest);

                if (interParallel.Count != 0)
                    PlaceOnParallel(interParallel, widthProject, width, false, highProject, height);
                else
                    return;
            }
            catch
            {
                throw new Exception();
            }
        }

        //method to do all creation and adjustment of instances
        private void CreateAdjustElement(Element hostEle, XYZ instancePoint, XYZ savePoint , double width, bool stru, double rod)
        {
            try
            {
                using (Transaction tx = new Transaction(doc, "Create Element"))
                {
                    tx.Start();
                    Element created = CreateAndPlace.CreateElement(instancePoint, hostEle, doc.GetElement(hostEle.LevelId) as Level);
                    Points.AddCreatedRecord(hostEle.Id, savePoint);

                    AdjustInstance.doc = doc;
                    AdjustInstance.instance = created;
                    AdjustInstance.hostElement = hostEle;
                    AdjustInstance.instancePoint = instancePoint;
                    AdjustInstance.width = width;
                    AdjustInstance.stru = stru;
                    AdjustInstance.rod = rod;
                    AdjustInstance.type = ConfigurationData.type;

                    if (AdjustInstance.MakeAdjustments())
                        tx.Commit();
                    else
                        tx.RollBack();
                }
            }
            catch
            {
                throw new Exception();
            }
        }
    }
}
