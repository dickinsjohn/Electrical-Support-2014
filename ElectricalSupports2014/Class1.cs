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

using Security_Check;

namespace ElectricalSupports2014
{
    //Transaction assigned as automatic
    [Autodesk.Revit.Attributes.Transaction(Autodesk.Revit.Attributes.TransactionMode.Manual)]
    [Autodesk.Revit.Attributes.Regeneration(Autodesk.Revit.Attributes.RegenerationOption.Manual)]
    [Autodesk.Revit.Attributes.Journaling(Autodesk.Revit.Attributes.JournalingMode.NoCommandData)]

    public class ElectricalSupports2014: IExternalCommand
    {
        //FIELDS
        UIDocument m_doc = null;
        ElementSet eleSet = null;

        //METHODS
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                m_doc = commandData.Application.ActiveUIDocument;

                //check for LNT domain
                if (!CheckSecurity())
                {
                    return Result.Succeeded;
                }

                //get selected elements
                if(!GetSelectedElements())
                {
                    TaskDialog.Show("Error!", "Please select trays and ducts before executing the Add-in!");
                    return Result.Succeeded;
                }

                ConfigurationData configData = new ConfigurationData();

                if (ConfigurationData.configFileData.discipline != "ELECTRICAL")
                {
                    TaskDialog.Show("Error!", "Add-in not intended for your discipline!");
                    return Result.Succeeded;
                }
                
                Execution Execute = new Execution(m_doc.Document, eleSet, ConfigurationData.configFileData.selectedFamily, 
                    ConfigurationData.configFileData.minSpacing);

                return Result.Succeeded;
            }
            catch
            {
                return Result.Failed;
            }
        }


        //check security
        private bool CheckSecurity()
        {
            try
            {
                //call to the security check method to check for authentication
                bool security = SecurityLNT.Security_Check();
                
                if (security == true)
                    return true;
                else
                    return false;
            }
            catch
            {
                return false;
            }
        }

        //method to get elements selected by user
        private bool GetSelectedElements()
        {
            try
            {
                //get the selected element set
                eleSet = m_doc.Selection.Elements;

                if (eleSet.IsEmpty)
                    return false;
                else
                    return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
