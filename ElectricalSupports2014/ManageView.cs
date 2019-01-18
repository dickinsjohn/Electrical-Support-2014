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
    class ManageView
    {
        private static Document doc;
        public static View3D view3D;
        private static BoundingBoxXYZ bounds;
        private int detail;
        
        //constructor
        public ManageView(Document docu)
        {
            doc = docu;
            view3D = null;
            bounds = null;
            detail = -1;
            Get3D_View();
        }

        //get the 3D view to work on
        public void Get3D_View()
        {
            try
            {
                //generate a 3D view for the reference intersector class to work
                view3D = (from v in new FilteredElementCollector(doc).OfClass(typeof(View3D)).Cast<View3D>()
                          where v.IsTemplate == false && v.IsPerspective == false select v).First();
            }
            catch
            {
                throw new Exception();
            }
        }


        //get the bounding box for sectionbox of the 3D view
        public void GetBoundsDetail()
        {
            try
            {
                using (Transaction tran = new Transaction(doc,"Manage View"))
                {
                    tran.Start();
                    bounds = view3D.GetSectionBox();
                    view3D.IsSectionBoxActive = false;
                    detail = view3D.get_Parameter(BuiltInParameter.VIEW_DETAIL_LEVEL).AsInteger();
                    view3D.get_Parameter(BuiltInParameter.VIEW_DETAIL_LEVEL).Set(2);
                    tran.Commit();
                }
            }
            catch
            {
                throw new Exception();
            }
        }


        //set the bounding box for the sectionbox of 3D view
        public void SetBounds()
        {
            try
            {
                using (Transaction tran = new Transaction(doc, "Manage View"))
                {
                    tran.Start();
                    view3D.IsSectionBoxActive = true;
                    view3D.SetSectionBox(bounds);
                    view3D.get_Parameter(BuiltInParameter.VIEW_DETAIL_LEVEL).Set(detail);
                    tran.Commit();
                }
            }
            catch
            {
                throw new Exception();
            }
        }
    }
}
