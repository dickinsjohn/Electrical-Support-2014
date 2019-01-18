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
    class CreateAndPlace
    {
        static Document doc;
        string targetName;
        Family family = null;
        static FamilySymbol symbol = null;

        //constructors
        public CreateAndPlace()
        {
            doc = null;
            targetName = null;
        }

        public CreateAndPlace(Document d, string str)
        {
            doc = d;
            targetName = str;
            family = FindElementByName() as Family;
            symbol = GetFamilySymbol();
        }


        //method to get family symbol
        private FamilySymbol GetFamilySymbol()
        {
            FamilySymbol symbol = null;
            try
            {
                foreach (FamilySymbol s in family.Symbols)
                {
                    symbol = s;
                    break;
                }

                return symbol;
            }
            catch
            {
                TaskDialog.Show("Error!", "Please load Family and try again.");
                throw new Exception();
            }
        }

        //method to find the element by family name
        private Element FindElementByName()
        {
            try
            {
                return new FilteredElementCollector(doc).OfClass(typeof(Family))
                    .FirstOrDefault<Element>(e => e.Name.Equals(targetName));
            }
            catch
            {
                throw new Exception();
            }
        }

        //method to create an element
        public static Element CreateElement(XYZ point, Element host,Level lvl)
        {
            try
            {
                Element ele = doc.Create.NewFamilyInstance(point, symbol, host, lvl, StructuralType.NonStructural);                
                return ele;
            }
            catch
            {
                throw new Exception();
            }
        }
    }
}
