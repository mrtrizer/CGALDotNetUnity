using System;
using System.Collections.Generic;
using UnityEngine;

using Common.Unity.Drawing;
using CGALDotNet;
using CGALDotNet.Polygons;
using CGALDotNet.Geometry;
using CGALDotNet.Marching;
using CGALDotNet.Triangulations;
using CGALDotNet.DCEL;
using CGALDotNet.CSG;

namespace CGALDotNetUnity.Polygons
{

    public class MarchingSquaresExample : InputBehaviour
    {
        private Color redColor = new Color32(200, 80, 80, 255);

        private Color pointColor = new Color32(80, 80, 200, 255);

        private Color faceColor = new Color32(80, 80, 200, 128);

        private Color lineColor = new Color32(0, 0, 0, 255);

        private Dictionary<string, CompositeRenderer> Renderers;

        private Node<Point2d, double> Root;

        protected override void Start()
        {
            base.Start();
            SetInputMode(INPUT_MODE.NONE);
            Renderers = new Dictionary<string, CompositeRenderer>();

            PerformMarching();
        }

        private void PerformMarching()
        {
            int size = 20;
            int half = size / 2;
            Point2d translate = new Point2d(-half);
            
            var bounds = new BoxNode2(new Point2d(1), new Point2d(size - 1));
            var circle = new CircleNode2(new Point2d(half), 5);
            var box = new BoxNode2(new Point2d(2), new Point2d(10));
            var union = new UnionNode2(circle, box);
            var subtract = new SubtractionNode2(bounds, union);

            Root = subtract;

            var ms = new MarchingSquares();
            var tri = new ConstrainedTriangulation2<EEK>();

            var vertices = new List<Point2d>();
            var indices = new List<int>();

            ms.Generate(SDF, size + 1, size+ 1, vertices, indices);

            var segments = new List<Segment2d>();
            for(int i = 0; i < indices.Count/2; i++)
            {
                int i0 = i * 2 + 0;
                int i1 = i * 2 + 1;

                var a = vertices[indices[i0]] + translate;
                var b = vertices[indices[i1]] + translate;

                segments.Add(new Segment2d(a, b));

                tri.InsertConstraint(a, b);
            }

            CreateRenderer(tri, translate);

            //Renderers["Tri"] = Draw().Outline(tri, lineColor).PopRenderer();
            Renderers["Segments"] = Draw().Outline(segments, lineColor).PopRenderer();
        }

        private void CreateRenderer(ConstrainedTriangulation2<EEK> tri, Point2d translate)
        {
            var points = new Point2d[tri.VertexCount];
            tri.GetPoints(points);

            var indices = new int[tri.IndiceCount];
            tri.GetIndices(indices);

            var indices2 = new List<int>();

            for (int i = 0; i < indices.Length / 3; i++)
            {
                int i0 = i * 3 + 0;
                int i1 = i * 3 + 1;
                int i2 = i * 3 + 2;

                var a = points[indices[i0]] - translate;
                var b = points[indices[i1]] - translate;
                var c = points[indices[i2]] - translate;

                var center = (a + b + c) / 3.0;

                if(SDF(center.x, center.y) > 0)
                {
                    indices2.Add(indices[i0]);
                    indices2.Add(indices[i1]);
                    indices2.Add(indices[i2]);
                }

            }

            Renderers["Trianguation"] = Draw().
                Faces(points, indices2, faceColor).
                PopRenderer();

        }

        private double SDF(double x, double y)
        {
            var point = new Point2d(x, y);
            return Root.Func(point);
        }

        private void OnPostRender()
        {
            DrawGrid();

            foreach (var renderer in Renderers.Values)
                renderer.Draw();

        }

    }
}
