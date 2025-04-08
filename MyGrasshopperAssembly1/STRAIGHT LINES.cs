using System;
using System.Collections.Generic;

using Grasshopper;
using Grasshopper.Kernel;
using Rhino.Geometry;

namespace KERFIN
{
  public class MyGrasshopperAssemblyComponent1 : GH_Component
  {
    /// <summary>
    /// Each implementation of GH_Component must provide a public 
    /// constructor without any arguments.
    /// Category represents the Tab in which the component will appear, 
    /// Subcategory the panel. If you use non-existing tab or panel names, 
    /// new tabs/panels will automatically be created.
    /// </summary>
    public MyGrasshopperAssemblyComponent1()
      : base(" Straight pattern lines", 
            " Straight Lines",
        " Pattern of the kerfing is a straight line patterns ",
        "KERF-IN", 
        "Patterns")
    {
    }

    /// <summary>
    /// Registers all the input parameters for this component.
    /// </summary>
    protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
    {
            pManager.AddSurfaceParameter(" Surface ", " srf ", " Surface in which you want the pattern", GH_ParamAccess.item);
            pManager.AddNumberParameter(" Number of lines", "No. of lines", " Number of lines of the pattern", GH_ParamAccess.item);
            pManager.AddNumberParameter(" rectangle width", " rectWidth", " Width of the lines of the pattern", GH_ParamAccess.item);
            pManager.AddNumberParameter(" rectangle length", " rectLength", " Lenght of the lines of the pattern", GH_ParamAccess.item);
            pManager.AddNumberParameter(" Gap", "Gap", " Distance / Gap between the two lines ", GH_ParamAccess.item);

    }

    /// <summary>
    /// Registers all the output parameters for this component.
    /// </summary>
    protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
    {
            pManager.AddCurveParameter(" PatternCurves", " Lines ", " Pattern lines for kerfing", GH_ParamAccess.list);

    }

    /// <summary>
    /// This is the method that actually does the work.
    /// </summary>
    /// <param name="DA">The DA object can be used to retrieve data from input parameters and 
    /// to store data in output parameters.</param>
    protected override void SolveInstance(IGH_DataAccess DA)
    {
            Surface srf = null;
            double numLines = double.NaN;
            double rectWidth = double.NaN;
            double rectLength = double.NaN;
            double gap = double.NaN;

            DA.GetData(0, ref srf);
            DA.GetData(1, ref numLines);
            DA.GetData(2, ref rectWidth);
            DA.GetData(3, ref rectLength);
            DA.GetData(4, ref gap);


               if (srf == null)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Error: Input surface is null.");
                return;
            }

            // Validate rectWidth and gap
            if (rectWidth >= gap)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Error: rectWidth must be smaller than gap.");                
                return;
            }

            List<Curve> kerfCurves = new List<Curve>();

            Interval uDomain = srf.Domain(0);
            Interval vDomain = srf.Domain(1);

            double dy = (vDomain.Length - (numLines - 1) * gap) / numLines;
            double dx = rectLength;
            double shift = dx / 2.0; // Offset for even rows

            List<Interval> occupiedIntervals = new List<Interval>(); // Tracks vertical occupied space

            for (int i = 0; i < numLines; i++)
            {
                double v = vDomain.Min + i * (dy + gap);
                double offset = (i % 2 == 0) ? 0 : shift; // Shift alternate rows

                // Check for vertical overlap
                Interval newInterval = new Interval(v, v + rectWidth);
                foreach (Interval existing in occupiedIntervals)
                {
                    // Manual intersection check
                    if (!(newInterval.Max < existing.Min || newInterval.Min > existing.Max))
                    {
                        AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Error: Rectangles are intersecting due to increased rectWidth. Adjust the values.");
                        return;
                    }
                }
                occupiedIntervals.Add(newInterval);

                for (double u = uDomain.Min + offset; u <= uDomain.Max; u += 2 * dx) // Allow exceeding boundary
                {
                    double u1 = u;
                    double u2 = u + dx;

                    // Ensure at least part of the rectangle is within surface bounds
                    if (u1 > uDomain.Max) break;
                    if (u2 > uDomain.Max) u2 = uDomain.Max;

                    Point3d p0 = srf.PointAt(u1, v);
                    Point3d p1 = srf.PointAt(u2, v);
                    Point3d p2 = srf.PointAt(u2, v + rectWidth);
                    Point3d p3 = srf.PointAt(u1, v + rectWidth);

                    Polyline rect = new Polyline(new List<Point3d> { p0, p1, p2, p3, p0 });
                    kerfCurves.Add(rect.ToNurbsCurve());
                }
            }
           

            DA.SetDataList(0, kerfCurves);



        }

        /// <summary>
        /// Provides an Icon for every component that will be visible in the User Interface.
        /// Icons need to be 24x24 pixels.
        /// You can add image files to your project resources and access them like this:
        /// return Resources.IconForThisComponent;
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                try
                {
                    var assembly = System.Reflection.Assembly.GetExecutingAssembly();

                    // Debug: print all available embedded resources
                    foreach (var res in assembly.GetManifestResourceNames())
                    {
                        Rhino.RhinoApp.WriteLine("Resource: " + res);
                    }

                    // Try loading your icon
                    using (var stream = assembly.GetManifestResourceStream("KERFIN.PATTERN1.png"))
                    {
                        if (stream != null)
                            return new System.Drawing.Bitmap(stream);
                    }
                }
                catch (Exception ex)
                {
                    Rhino.RhinoApp.WriteLine("Error loading icon: " + ex.Message);
                }
                return null;
            }
        }


        /// <summary>
        /// Each component must have a unique Guid to identify it. 
        /// It is vital this Guid doesn't change otherwise old ghx files 
        /// that use the old ID will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid => new Guid("14993506-b176-42b4-aacc-0575d28f43f1");
  }
}