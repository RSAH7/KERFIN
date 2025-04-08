using System;
using System.Collections.Generic;
using Grasshopper.Kernel;
using Rhino.Geometry;

namespace KERFIN
{
    public class CNC : GH_Component
    {
        public CNC()
          : base("CNC FABRICATION",
                 "CNC",
                 "Components for CNC fabrication",
                 "KERF-IN",
                 "Fabrication")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddSurfaceParameter("Surface", "srf", "Surface in which you want the pattern", GH_ParamAccess.item);
            pManager.AddCurveParameter("Pattern Curves", "Patterns", "Pattern curves", GH_ParamAccess.list);
            pManager.AddNumberParameter("Material Thickness", "Thickness", "Thickness of the material", GH_ParamAccess.item);
            pManager.AddNumberParameter("Drill bit size", "DrillBit", "Size of the drill bit", GH_ParamAccess.item);
            pManager.AddNumberParameter("Offset Distance", "Offset", "Offset of the distance you want", GH_ParamAccess.item);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddCurveParameter("Offset Curves", "Curves", "Curves which we offset", GH_ParamAccess.list);
            pManager.AddSurfaceParameter("Split Surface", "SplitSrf", "Surface split with the curves", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            Surface surface = null;
            List<Curve> patternCurves = new List<Curve>();
            double materialThickness = double.NaN;
            double drillBitSize = double.NaN;
            double offsetDistance = double.NaN;

            if (!DA.GetData(0, ref surface)) return;
            if (!DA.GetDataList(1, patternCurves)) return;
            if (!DA.GetData(2, ref materialThickness)) return;
            if (!DA.GetData(3, ref drillBitSize)) return;
            if (!DA.GetData(4, ref offsetDistance)) return;

            double totalOffset = (drillBitSize / 2.0) + offsetDistance;
            List<Curve> closedPolylines = new List<Curve>();

            foreach (Curve curve in patternCurves)
            {
                if (curve == null) continue;

                Curve[] offsetResult1 = curve.Offset(Plane.WorldXY, totalOffset, 0.01, CurveOffsetCornerStyle.Sharp);
                Curve[] offsetResult2 = curve.Offset(Plane.WorldXY, -totalOffset, 0.01, CurveOffsetCornerStyle.Sharp);

                if (offsetResult1 != null && offsetResult1.Length > 0 && offsetResult2 != null && offsetResult2.Length > 0)
                {
                    Curve posOffset = offsetResult1[0];
                    Curve negOffset = offsetResult2[0];

                    Point3d startPoint1 = posOffset.PointAtStart;
                    Point3d startPoint2 = negOffset.PointAtStart;
                    Point3d endPoint1 = posOffset.PointAtEnd;
                    Point3d endPoint2 = negOffset.PointAtEnd;

                    Curve line1 = new Line(startPoint1, startPoint2).ToNurbsCurve();
                    Curve line2 = new Line(endPoint1, endPoint2).ToNurbsCurve();

                    List<Curve> curvesToJoin = new List<Curve> { posOffset, negOffset, line1, line2 };
                    Curve[] joinedCurves = Curve.JoinCurves(curvesToJoin, 0.01);

                    if (joinedCurves.Length > 0 && joinedCurves[0].IsClosed)
                    {
                        closedPolylines.Add(joinedCurves[0]);
                    }
                }
            }

            // Convert Surface to Brep
            Brep brepSurface = surface.ToBrep();

            // Split the brep surface with closed polylines
            Brep[] splitSurfaces = (closedPolylines.Count > 0)
                ? brepSurface.Split(closedPolylines, 0.01)
                : new Brep[] { brepSurface };

            // Find the largest surface (optional, based on area)
            Brep largestSurface = null;
            double maxArea = 0;

            foreach (Brep brep in splitSurfaces)
            {
                if (brep != null)
                {
                    double area = brep.GetArea();
                    if (area > maxArea)
                    {
                        maxArea = area;
                        largestSurface = brep;
                    }
                }
            }

            DA.SetDataList(0, closedPolylines);
            DA.SetData(1, largestSurface);
        }

        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                try
                {
                    var assembly = System.Reflection.Assembly.GetExecutingAssembly();
                    using (var stream = assembly.GetManifestResourceStream("KERFIN.CNC.png"))
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

        public override Guid ComponentGuid => new Guid("EFD5E6B3-E83D-4CFA-B9DB-9B5B5342E43F");
    }
}
