using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

using Common.Unity.Drawing;
using Common.Unity.Utility;
using CGALDotNet;
using CGALDotNet.Geometry;
using CGALDotNet.Polygons;
using CGALDotNet.Arrangements;
using CGALDotNet.DCEL;

namespace CGALDotNetUnity.Arrangements
{

    public class DCELExample : InputBehaviour
    {

        public enum CLICK_MODE
        {
            CLICK_TO_ADD_POINT,
            CLICK_TO_ADD_EDGE,
            CLICK_TO_ADD_POLYGON,
            CLICK_TO_SELECT_VERTEX,
            CLICK_TO_SELECT_EDGE,
            CLICK_TO_SELECT_FACE
        }

        private Color pointColor = new Color32(80, 80, 200, 255);

        private Color redColor = new Color32(200, 80, 80, 255);

        private Color faceColor = new Color32(80, 80, 200, 128);

        private Color lineColor = new Color32(0, 0, 0, 255);

        private Dictionary<string, CompositeRenderer> Renderers;

        private Arrangement2<EEK> arrangement;

        private DCELMesh mesh;
            
        private CLICK_MODE ClickMode = CLICK_MODE.CLICK_TO_ADD_POINT;

        private DCELVertex? SelectedVertex;

        private DCELHalfEdge? SelectedEdge;

        private DCELFace? SelectedFace;

        protected override void Start()
        {
            base.Start();
            Renderers = new Dictionary<string, CompositeRenderer>();
            SetInputMode(INPUT_MODE.POINT_CLICK);

            CreateArrangement();
        }

        private void CreateArrangement()
        {
            arrangement = new Arrangement2<EEK>();
            var box = PolygonFactory<EEK>.FromBox(-5, 5);

            arrangement.InsertPolygon(box, true);

            Renderers.Clear();
            BuildArrangementRenderer();
        }

        protected override void OnInputComplete(List<Point2d> points)
        {
            if (Mode == INPUT_MODE.SEGMENT)
            {
                var a = InputPoints[0];
                var b = InputPoints[1];

                AddEdge(a, b);
                InputPoints.Clear();
            }
            else if (Mode == INPUT_MODE.POLYGON)
            {
                AddPolygon(points);
                InputPoints.Clear();
            }
        }

        protected override void OnLeftClickDown(Point2d point)
        {
            switch (ClickMode)
            {
                case CLICK_MODE.CLICK_TO_ADD_POINT:
                    AddPoint(point);
                    break;

                case CLICK_MODE.CLICK_TO_SELECT_VERTEX:
                    SelectVertex(point);
                    break;

                case CLICK_MODE.CLICK_TO_SELECT_EDGE:
                    SelectEdge(point);
                    break;

                case CLICK_MODE.CLICK_TO_SELECT_FACE:
                    SelectFace(point);
                    break;
            }

        }

        private void AddPoint(Point2d point)
        {
            UnselectAll();

            arrangement.InsertPoint(point);
            BuildArrangementRenderer();
        }

        private void AddEdge(Point2d a, Point2d b)
        {
            UnselectAll();

            arrangement.InsertSegment(a, b, false);
            BuildArrangementRenderer();
        }

        private void AddPolygon(List<Point2d> points)
        {
            var polygon = new Polygon2<EEK>(points.ToArray());

            arrangement.InsertPolygon(polygon, false);
            BuildArrangementRenderer();
        }
        private void SelectVertex(Point2d point)
        {
            UnselectAll();
            if (mesh == null) return;

            if (mesh.LocateVertex(point, out DCELVertex vertex))
            {
                if (Point2d.Distance(vertex.Point.xy, point) < 0.2)
                {
                    SelectedVertex = vertex;
                    BuildSelectionRenderer();
                }
                    
            }
        }

        private void SelectEdge(Point2d point)
        {
            UnselectAll();
            if (mesh == null) return;

            if (mesh.LocateEdge(point, out DCELHalfEdge edge))
            {
                if (edge.Segment2.Distance(point) < 0.2)
                {
                    SelectedEdge = edge;
                    BuildSelectionRenderer();
                }
                    
            }
        }

        private void SelectFace(Point2d point)
        {
            UnselectAll();
            if (mesh == null) return;

            if (mesh.LocateFace(point, out DCELFace face))
            {
                if (face.Index != -1)
                    SelectedFace = face;
            }
        }

        private void UnselectAll()
        {
            SelectedVertex = null;
            SelectedEdge = null;
            SelectedFace = null;

            Renderers.Remove("Vertex");
            Renderers.Remove("Edge");
        }

        private void BuildArrangementRenderer()
        {
            mesh = new DCELMesh(arrangement);
            var segments = CreateSegments();

            var points = new Point2d[arrangement.VertexCount];
            arrangement.GetPoints(points);

            AddSnapTargets(points);

            Renderers["Mesh"] = Draw().
                Outline(segments, lineColor).
                Points(points, lineColor, pointColor).
                PopRenderer();
        }

        private void BuildSelectionRenderer()
        {
            if (SelectedVertex != null)
            {
                var point = SelectedVertex.Value.Point;
                Renderers["Vertex"] = Draw().Points(point.xy, lineColor, redColor).PopRenderer();
            }

            if (SelectedEdge != null)
            {
                var seg = SelectedEdge.Value.Segment2;
                Renderers["Edge"] = Draw().Outline(seg, redColor).PopRenderer();
            }
        }

        private List<Segment2d> CreateSegments()
        {
            double min = 0.5;
            double offset = 0.2;
            double arrowLen = 0.5;

            var segments = new List<Segment2d>();

            foreach(var halfEdge in mesh.EnumerateEdges())
            {
                var a = halfEdge.SourcePoint2;
                var b = halfEdge.TargetPoint2;
                var segment = new Segment2d(a, b);

                if (segment.Length < min) continue;

                var tangent = (b - a).Vector2d.Normalized;
                var normal = tangent.PerpendicularCW;

                segment.A += tangent.Point2d * min;
                segment.B -= tangent.Point2d * min;

                segment.A += normal.Point2d * offset;
                segment.B += normal.Point2d * offset;

                segments.Add(segment);

                var n = (tangent + normal).Normalized;
                var c = segment.A;
                var d = c + n.Point2d * arrowLen;
                var arrow = new Segment2d(c, d);

                segments.Add(arrow);
            }


            return segments;
        }

        protected override void OnCleared()
        {
            UnselectAll();
            CreateArrangement();
            ClearSnapTargets();
        }

        protected override void Update()
        {
            base.Update();

            if (Input.GetKeyDown(KeyCode.Tab))
            {
                ClickMode = CGALEnum.Next(ClickMode);

                if (ClickMode == CLICK_MODE.CLICK_TO_ADD_EDGE)
                    SetInputMode(INPUT_MODE.SEGMENT);
                else if (ClickMode == CLICK_MODE.CLICK_TO_ADD_POLYGON)
                    SetInputMode(INPUT_MODE.POLYGON);
                else
                    SetInputMode(INPUT_MODE.POINT_CLICK);

            }

        }

        private void OnPostRender()
        {
            DrawGrid();
            DrawInput(lineColor, pointColor, PointSize);

            foreach (var renderer in Renderers.Values)
                renderer.Draw();

        }

        protected void OnGUI()
        {
            int textLen = 1000;
            int textHeight = 25;
            GUI.color = Color.black;

            GUI.Label(new Rect(10, 10, textLen, textHeight), "Space to clear.");
            GUI.Label(new Rect(10, 30, textLen, textHeight), "Tab to change mode.");
            GUI.Label(new Rect(10, 50, textLen, textHeight), "Current mode = " + ClickMode);

            if (SelectedVertex != null)
            {
                GUI.Label(new Rect(10, 70, textLen, textHeight), "Selected Vertex = " + SelectedVertex.Value);
            }
            else if (SelectedEdge != null)
            {
                var edge = SelectedEdge.Value;
                GUI.Label(new Rect(10, 70, textLen, textHeight), "Selected Edge = " + edge);

                var twin = mesh.GetHalfEdge(edge.TwinIndex);
                GUI.Label(new Rect(10, 90, textLen, textHeight), "Selected Twin = " + twin);
            }
            else if (SelectedFace != null)
            {
                GUI.Label(new Rect(10, 70, textLen, textHeight), "Selected Face = " + SelectedFace.Value);
            }
        }

    }
}