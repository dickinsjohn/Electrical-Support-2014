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
    class ConfigurationData
    {
        static List<ElectricalRodSpecs> rodSpecList = new List<ElectricalRodSpecs>();
        static List<ElectricalAngleSpecs> angleSpecList = new List<ElectricalAngleSpecs>();

        public static SpecificationData configFileData;

        public static int type=0;

        //constructors
        public ConfigurationData()
        {
            configFileData = ReadConfig();
            GetElectricalSpecs();
        }

        //method to return the rod dia from the list
        public static double ReturnDia(double height)
        {
            try
            {
                height *= 304.8;

                for (int i = 0; i < rodSpecList.Count - 1; i++)
                {
                    if (height <= rodSpecList[i].height)
                    {
                        return rodSpecList[i].dia * 0.00328084;
                    }
                    else if (height > rodSpecList[i].height && height <= rodSpecList[i + 1].height)
                    {
                        return rodSpecList[i + 1].dia * 0.00328084;
                    }
                }

                return 0;
            }
            catch
            {
                throw new Exception();
            }
        }


        //methdo to return the angle support specifications
        public static AngleSpecs ReturnAngleSpecs(double height)
        {
            try
            {
                AngleSpecs angleSpec = new AngleSpecs();

                for (int i = 0; i < angleSpecList.Count - 1; i++)
                {
                    if (height <= angleSpecList[i].height)
                    {
                        angleSpec.dim1 = 0.00328084 * angleSpecList[i].dim1;
                        angleSpec.dim2 = 0.00328084 * angleSpecList[i].dim2;
                        angleSpec.dim3 = 0.00328084 * angleSpecList[i].dim3;
                        return angleSpec;
                    }
                    else if (height > angleSpecList[i].height && height <= angleSpecList[i + 1].height)
                    {
                        angleSpec.dim1 = 0.00328084 * angleSpecList[i + 1].dim1;
                        angleSpec.dim2 = 0.00328084 * angleSpecList[i + 1].dim2;
                        angleSpec.dim3 = 0.00328084 * angleSpecList[i + 1].dim3;
                        return angleSpec;
                    }
                }

                return angleSpec;
            }
            catch
            {
                throw new Exception();
            }
        }

        //method to Read data from the configuration file
        private SpecificationData ReadConfig()
        {
            try
            {
                SpecificationData fileData = new SpecificationData();

                string configurationFile = null, assemblyLocation = null;

                //open configuration file
                assemblyLocation = System.Reflection.Assembly.GetExecutingAssembly().Location;

                //convert the active file path into directory name
                if (File.Exists(assemblyLocation))
                {
                    assemblyLocation = new FileInfo(assemblyLocation).Directory.ToString();
                }

                //get parent directory of the current directory
                if (Directory.Exists(assemblyLocation))
                {
                    assemblyLocation = Directory.GetParent(assemblyLocation).ToString();
                }

                configurationFile = assemblyLocation + @"\Spacing Configuration\Configuration.txt";

                if (!(File.Exists(configurationFile)))
                {
                    TaskDialog.Show("Error!", "Configuration file doesn't exist!");
                    return fileData;
                }
                else
                {
                    //read all the contents of the file into a string array
                    string[] fileContents = File.ReadAllLines(configurationFile);

                    for (int i = 0; i < fileContents.Count(); i++)
                    {
                        if (fileContents[i].Contains("SelectedFamily: "))
                        {
                            fileData.selectedFamily = fileContents[i].Replace("SelectedFamily: ", "").Trim();
                        }
                        else if (fileContents[i].Contains("Discipline: "))
                        {
                            fileData.discipline = fileContents[i].Replace("Discipline: ", "").Trim();
                        }
                        else if (fileContents[i].Contains("Spacing: "))
                        {
                            fileData.minSpacing = (int)(1000 * double.Parse(fileContents[i].Replace("Spacing: ", "").Trim()));
                        }
                        else if (fileContents[i].Contains("Support Type: "))
                        {
                            fileData.supportType = fileContents[i].Replace("Support Type: ", "").Trim();

                            if (fileData.supportType.Contains("Threaded_Rod_Support"))
                                type = -1;
                            else if (fileData.supportType.Contains("Angle_Support"))
                                type = 1;
                        }
                        else if (fileContents[i].Contains("File Location: "))
                        {
                            fileData.specsFile = fileContents[i].Replace("File Location: ", "").Trim();
                        }
                    }
                }
                return fileData;
            }
            catch
            {
                throw new Exception();
            }
        }
        

        //get specificaions regarding the rod, angle and height
        private void GetElectricalSpecs()
        {
            try
            {
                ElectricalRodSpecs rodSpecs = new ElectricalRodSpecs();
                ElectricalAngleSpecs angleSpecs = new ElectricalAngleSpecs();

                if (File.Exists(configFileData.specsFile))
                {
                    //read all the contents of the file into a string array
                    string[] fileContents = File.ReadAllLines(configFileData.specsFile);
                    
                    for (int i = 0; i < fileContents.Count(); i++)
                    {
                        if (type == 1 && fileContents[i].Contains("ANGLE_SUPPORT"))
                        {
                            string[] secondSplit = null, firstSplit = null;
                            
                            for (int j = i + 1; j < fileContents.Count(); j++)
                            {
                                if (!fileContents[j].Contains(" "))
                                    return;

                                firstSplit = fileContents[j].Split(' ');

                                if (firstSplit[0] != "" && firstSplit[1] != "")
                                {
                                    secondSplit = firstSplit[1].Split('X');
                                
                                    angleSpecs.height = double.Parse(firstSplit[0].Trim());
                                    angleSpecs.dim1 = double.Parse(secondSplit[0].Trim());
                                    angleSpecs.dim2 = double.Parse(secondSplit[1].Trim());
                                    angleSpecs.dim3 = double.Parse(secondSplit[2].Trim());
                                    angleSpecList.Add(angleSpecs);
                                }
                            }
                        }
                        else if (type == -1 && fileContents[i].Contains("THREADED_ROD_SUPPORT"))
                        {
                            string[] firstSplit = null;
                            for (int j = i + 1; j < fileContents.Count(); j++)
                            {
                                if (!fileContents[j].Contains(" "))
                                    return;

                                firstSplit = fileContents[j].Split(' ');

                                rodSpecs.height = double.Parse(firstSplit[0].Trim());
                                rodSpecs.dia = double.Parse(firstSplit[1].Trim());

                                rodSpecList.Add(rodSpecs);
                            }
                        }
                    }
                }
            }
            catch(FormatException)
            {
                TaskDialog.Show("Error!", "Error in the configuration. Please correct it.");
                throw new FormatException();
            }
            catch
            {
                throw new Exception();
            }
        }
    }
}
