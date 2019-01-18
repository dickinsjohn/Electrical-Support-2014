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
    public static class AdjustInstance
    {
        public static Document doc { get; set;}
        public static Element instance { get; set;}
        public static Element hostElement { get; set;}
        public static XYZ instancePoint { get; set; }        
        public static double width { get; set; }
        public static bool stru {get; set;}
        public static double rod { get; set; }
        public static int type { get; set; }
        public static double height {get;set;}

        static Line line;
        static double offset, yAngle, zAngle, angleWidth, trayHeight, rodLeft= 0, rodRight= 0;

        //constructor
        public static bool MakeAdjustments()
        {
            try
            {
                ComputeValues();
                AdjustOffset();
                AdjustAngle();
                if (!SetParameters())
                    return false;
                return true;
            }
            catch
            {
                throw new Exception();
            }
        }

        //method to compute all required values
        private static void ComputeValues()
        {
            try
            {
                line = (Line)((LocationCurve)hostElement.Location).Curve;

                if (hostElement is CableTray)
                {
                    offset = ((CableTray)hostElement).LevelOffset + ((CableTray)hostElement).ReferenceLevel.Elevation;
                    trayHeight = ((CableTray)hostElement).Height / 2;
                }
                else if (hostElement is Duct)
                {
                    offset = ((Duct)hostElement).LevelOffset;
                    trayHeight = ((Duct)hostElement).Height / 2;
                }

                Line yAngleAxis = Line.CreateBound(line.GetEndPoint(0), new XYZ(line.GetEndPoint(1).X, line.GetEndPoint(1).Y, line.GetEndPoint(0).Z));

                yAngle = XYZ.BasisY.AngleTo(yAngleAxis.Direction);

                zAngle = XYZ.BasisZ.AngleTo(line.Direction);

                angleWidth = 0.00328084 * double.Parse(((FamilyInstance)instance).Symbol.get_Parameter("Angle Width").AsValueString()) / 2;

                if (stru == true)
                    ComputeRodHeights();
                else if (stru == false)
                    rodLeft = rodRight = rod - 0.00328084 * 40;
            }
            catch (FormatException)
            {
                TaskDialog.Show("Error!", "Please change the length unit to millimeters and try again.");
                throw new Exception();
            }
            catch
            {
                throw new Exception();
            }
        }

        //method to compute rod heights
        private static void ComputeRodHeights()
        {
            try
            {
                ReferPoints points = new ReferPoints();
                points = Points.FindRodPoints(instancePoint, hostElement);
                rodLeft = Intersectors.StructureHeight(points.left, doc);
                rodRight = Intersectors.StructureHeight(points.right, doc);
            }
            catch
            {
                throw new Exception();
            }
        }

        //adjust the offset to make the instance flush with the bottom of the tray/duct
        private static void AdjustOffset()
        {
            try
            {
                if (Math.Round(zAngle, 5) > Math.Round(Math.PI / 2, 5) && line.GetEndPoint(0).Z > line.GetEndPoint(1).Z)
                {
                    XYZ temInstancePoint = new XYZ(instancePoint.X, instancePoint.Y, line.GetEndPoint(0).Z);
                    double correction = (line.GetEndPoint(0).DistanceTo(temInstancePoint)) / Math.Tan(Math.PI - zAngle)
                        + trayHeight / Math.Cos(zAngle - Math.PI / 2) + angleWidth / Math.Tan(Math.PI - zAngle) - trayHeight * Math.Cos(zAngle - Math.PI / 2);
                    offset -= correction;
                }
                else if (Math.Round(zAngle, 5) < Math.Round(Math.PI / 2, 5) && line.GetEndPoint(0).Z < line.GetEndPoint(1).Z)
                {
                    XYZ temInstancePoint = new XYZ(instancePoint.X, instancePoint.Y, line.GetEndPoint(0).Z);
                    double correction = (line.GetEndPoint(0).DistanceTo(temInstancePoint)) / Math.Tan(zAngle)
                        - trayHeight / Math.Sin(zAngle) - angleWidth / Math.Tan(zAngle) + trayHeight * Math.Cos(Math.PI / 2 - zAngle);
                    offset += correction;
                }

                if (Math.Round(line.GetEndPoint(0).Z, 5) != Math.Round(line.GetEndPoint(1).Z, 5))
                    offset -= (trayHeight) * Math.Sin(Math.Abs(zAngle));
                else
                    offset -= trayHeight;
            }
            catch
            {
                throw new Exception();
            }
        }

        //adjust the angle of the placed instance
        private static void AdjustAngle()
        {
            try
            {
                //axis of rotation
                Line axis = Line.CreateBound(instancePoint, new XYZ(instancePoint.X, instancePoint.Y, instancePoint.Z + 10));

                if (line.GetEndPoint(0).Y > line.GetEndPoint(1).Y)
                {
                    if (line.GetEndPoint(0).X > line.GetEndPoint(1).X)
                    {
                        //rotate the created family instance to align with the pipe
                        ElementTransformUtils.RotateElement(doc, instance.Id, axis, Math.PI + yAngle);
                    }
                    else if (line.GetEndPoint(0).X < line.GetEndPoint(1).X)
                    {
                        //rotate the created family instance to align with the pipe
                        ElementTransformUtils.RotateElement(doc, instance.Id, axis, 2 * Math.PI - yAngle);
                    }
                }
                else if (line.GetEndPoint(0).Y < line.GetEndPoint(1).Y)
                {
                    if (line.GetEndPoint(0).X > line.GetEndPoint(1).X)
                    {
                        //rotate the created family instance to align with the pipe
                        ElementTransformUtils.RotateElement(doc, instance.Id, axis, Math.PI + yAngle);
                    }
                    else if (line.GetEndPoint(0).X < line.GetEndPoint(1).X)
                    {
                        //rotate the created family instance to align with the pipe
                        ElementTransformUtils.RotateElement(doc, instance.Id, axis, 2 * Math.PI - yAngle);
                    }
                }
                else
                {
                    //rotate the created family instance to align with the pipe
                    ElementTransformUtils.RotateElement(doc, instance.Id, axis, yAngle);
                }


                if (Math.Round(zAngle, 5) < Math.Round(Math.PI / 2, 5)
                    && Math.Round(line.GetEndPoint(0).Y, 5) < Math.Round(line.GetEndPoint(1).Y, 5))
                {
                    //rotate the created family instance to align with the pipe
                    ElementTransformUtils.RotateElement(doc, instance.Id, axis, Math.PI);
                }
                else if (Math.Round(zAngle, 5) > Math.Round(Math.PI / 2, 5)
                    && Math.Round(line.GetEndPoint(0).Y, 5) > Math.Round(line.GetEndPoint(1).Y, 5))
                {
                    //rotate the created family instance to align with the pipe
                    ElementTransformUtils.RotateElement(doc, instance.Id, axis, Math.PI);
                }
                else if (Math.Round(zAngle, 5) < Math.Round(Math.PI / 2, 5)
                    && Math.Round(line.GetEndPoint(0).X, 5) < Math.Round(line.GetEndPoint(1).X, 5))
                {
                    //rotate the created family instance to align with the pipe
                    ElementTransformUtils.RotateElement(doc, instance.Id, axis, Math.PI);
                }
                else if (Math.Round(zAngle, 5) > Math.Round(Math.PI / 2, 5)
                    && Math.Round(line.GetEndPoint(0).X, 5) > Math.Round(line.GetEndPoint(1).X, 5))
                {
                    //rotate the created family instance to align with the pipe
                    ElementTransformUtils.RotateElement(doc, instance.Id, axis, Math.PI);
                }
            }
            catch
            {
                throw new Exception();
            }
        }

        //method to set all the parameters
        private static bool SetParameters()
        {
            if (rodLeft == 0 || rodRight == 0)
            {
                return false;
            }
            try
            {
                ParameterSet parameters = instance.Parameters;

                foreach (Parameter para in parameters)
                {
                    if (para.Definition.Name == "Offset")
                    {
                        para.Set(offset);
                    }
                    else if (para.Definition.Name == "Cable Tray Width" && width!=0)
                    {
                        para.Set(width);
                    }
                    else if (para.Definition.Name == "Rod 1")
                    {
                        para.Set(rodLeft);
                    }
                    else if (para.Definition.Name == "Rod 2")
                    {
                        para.Set(rodRight);
                    }
                    else if (para.Definition.Name == "Dia" && type == -1)
                    {
                        para.Set(ConfigurationData.ReturnDia(height));
                    }
                    else if (para.Definition.Name == "Web" && type == 1)
                    {
                        para.Set(ConfigurationData.ReturnAngleSpecs(height).dim1);
                    }
                    else if (para.Definition.Name == "Flange" && type == 1)
                    {
                        para.Set(ConfigurationData.ReturnAngleSpecs(height).dim2);
                    }
                    else if (para.Definition.Name == "Thickness" && type == 1)
                    {
                        para.Set(ConfigurationData.ReturnAngleSpecs(height).dim3);
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
