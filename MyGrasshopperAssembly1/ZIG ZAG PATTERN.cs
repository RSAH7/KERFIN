using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;

namespace KERFIN
{
    public class ZIG_ZAG_PATTERN : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the ZIG_ZAG_PATTERN class.
        /// </summary>
        public ZIG_ZAG_PATTERN()
          : base("ZIG ZAG PATTERN", 
                "ZIG ZAG",
              "Zig Zag pattern lines for kerfing",
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
            pManager.AddNumberParameter(" U Divisions ", "uDiv", " Number of divisions in U directions of the pattern", GH_ParamAccess.item);
            pManager.AddNumberParameter(" V Divisions ", "vDiv", " Number of divisions in V directions of the pattern", GH_ParamAccess.item);
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
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            Surface srf = null;
            double uDiv_d = 0;
            double vDiv_d = 0;

            DA.GetData(0, ref srf);
            DA.GetData(1, ref uDiv_d);
            DA.GetData(2, ref vDiv_d);

            int uDiv = (int)Math.Round(uDiv_d);
            int vDiv = (int)Math.Round(vDiv_d);

            List<Curve> patternCurves = new List<Curve>();

            // Reparameterize surface
            srf.SetDomain(0, new Interval(0, 1));
            srf.SetDomain(1, new Interval(0, 1));

            // Divide the surface into a grid of quads
            Point3d[,] grid = DivideSurfaceIntoQuads(srf, uDiv, vDiv);

            for (int i = 0; i < uDiv - 1; i++)
            {
                for (int j = 0; j < vDiv - 1; j++)
                {
                    if ((i + j) % 2 == 0)
                    {
                        Point3d ptA = grid[i, j];
                        Point3d B = grid[i, j + 1];
                        Point3d C = grid[i + 1, j + 1];
                        Point3d D = grid[i + 1, j];

                        Point3d X = (ptA + D) / 2;
                        Point3d Y = (B + C) / 2;

                        patternCurves.Add(new Line(ptA, X).ToNurbsCurve());
                        patternCurves.Add(new Line(B, X).ToNurbsCurve());
                        patternCurves.Add(new Line(C, Y).ToNurbsCurve());
                        patternCurves.Add(new Line(D, Y).ToNurbsCurve());
                    }
                }
            }

            DA.SetDataList(0, patternCurves);
        }

        private Point3d[,] DivideSurfaceIntoQuads(Surface srf, int uDiv, int vDiv)
        {
            Point3d[,] grid = new Point3d[uDiv, vDiv];

            Interval uDomain = srf.Domain(0);
            Interval vDomain = srf.Domain(1);

            for (int i = 0; i < uDiv; i++)
            {
                for (int j = 0; j < vDiv; j++)
                {
                    double u = uDomain.ParameterAt((double)i / (uDiv - 1));
                    double v = vDomain.ParameterAt((double)j / (vDiv - 1));
                    grid[i, j] = srf.PointAt(u, v);
                }
            }

            return grid;
        }

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
                get
            {
                    try
                    {
                        var assembly = System.Reflection.Assembly.GetExecutingAssembly();
                        using (var stream = assembly.GetManifestResourceStream("KERFIN.PATTERN2.png"))
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
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("81889F57-33E8-4692-A7E6-940BCCC11373"); }
        }
    }
}