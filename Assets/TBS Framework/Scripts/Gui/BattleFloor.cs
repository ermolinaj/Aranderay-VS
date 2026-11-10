using System.Collections.Generic;
using TbsFramework.Cells;
using TbsFramework.Grid.GridStates;
using UnityEngine;

namespace TbsFramework.Gui
{
    // Draw on the floor so the marks follow tile perspective and disappear behind props.
    internal sealed class BattleFloor
    {
        private static readonly Color32 EdgeShadow = new Color32(24, 17, 10, 226);
        private static readonly Color32 MoveEdge = new Color32(255, 215, 142, 228);
        private static readonly Color32 RouteEdge = new Color32(255, 246, 200, 255);

        private readonly GameObject markObject;
        private readonly Mesh mesh;
        private readonly Material material;
        private readonly List<Vector3> vertices = new List<Vector3>();
        private readonly List<Color32> colors = new List<Color32>();
        private readonly List<int> indices = new List<int>();
        private CellGridStateUnitSelected previousSelection;
        private IReadOnlyList<Cell> previousPath;
        private int previousLayers;

        internal BattleFloor(Shader shader)
        {
            markObject = new GameObject("Movement field", typeof(MeshFilter), typeof(MeshRenderer));
            mesh = new Mesh { name = "Reachable tiles and route" };
            mesh.MarkDynamic();
            markObject.GetComponent<MeshFilter>().sharedMesh = mesh;
            material = new Material(shader) { renderQueue = 2995 };
            var renderer = markObject.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }

        internal void Refresh(CellGridStateUnitSelected selection, int layers)
        {
            if (selection != null && selection.SelectedUnit.IsMoving)
                selection = null;
            var path = selection?.PreviewPath;
            if (previousSelection == selection && previousPath == path && previousLayers == layers)
                return;

            previousSelection = selection;
            previousPath = path;
            previousLayers = layers;
            mesh.Clear();
            vertices.Clear();
            colors.Clear();
            indices.Clear();

            if (selection == null)
                return;

            foreach (var cell in selection.ReachableCells)
            {
                if (TryGetFloor(cell, layers, out var floor))
                    AddBorder(floor, MoveEdge, 0.94f, 0.81f);
            }

            if (path != null && path.Count > 0)
            {
                var previous = selection.SelectedUnit.Cell;
                for (int i = path.Count - 1; i >= 0; i--)
                {
                    var step = path[i];
                    if (TryGetFloor(previous, layers, out var start) && TryGetFloor(step, layers, out var end))
                    {
                        var from = SurfaceCenter(start) + Vector3.up * 0.012f;
                        var to = SurfaceCenter(end) + Vector3.up * 0.012f;
                        float size = Mathf.Min(end.size.x, end.size.z);
                        AddRoute(from, to, size);
                    }
                    previous = step;
                }

                if (TryGetFloor(path[0], layers, out var destination))
                {
                    destination.center += Vector3.up * 0.018f;
                    AddBorder(destination, RouteEdge, 0.95f, 0.74f);
                }
            }

            mesh.SetVertices(vertices);
            mesh.SetColors(colors);
            mesh.SetTriangles(indices, 0);
        }

        internal static bool TryGetFloor(Cell cell, int layers, out Bounds floor)
        {
            floor = default;
            if (cell == null || (layers & (1 << cell.gameObject.layer)) == 0 ||
                !cell.TryGetComponent<Renderer>(out var renderer) || !renderer.enabled)
                return false;
            floor = renderer.bounds;
            return floor.size.x > 0.01f && floor.size.z > 0.01f;
        }

        private static Vector3 SurfaceCenter(Bounds floor) =>
            new Vector3(floor.center.x, floor.max.y + 0.025f, floor.center.z);

        private void AddBorder(Bounds floor, Color32 color, float outer, float inner)
        {
            var center = SurfaceCenter(floor);
            AddRing(center, floor.extents, outer + 0.035f, inner - 0.035f, EdgeShadow);
            AddRing(center + Vector3.up * 0.005f, floor.extents, outer, inner, color);
        }

        private void AddRing(Vector3 center, Vector3 extents, float outer, float inner, Color32 color)
        {
            var x = Vector3.right * extents.x;
            var z = Vector3.forward * extents.z;
            AddSide(center, x + z, -x + z, outer, inner, color);
            AddSide(center, -x + z, -x - z, outer, inner, color);
            AddSide(center, -x - z, x - z, outer, inner, color);
            AddSide(center, x - z, x + z, outer, inner, color);
        }

        private void AddSide(Vector3 center, Vector3 a, Vector3 b, float outer, float inner, Color32 color) =>
            AddQuad(center + a * outer, center + b * outer, center + b * inner, center + a * inner, color);

        private void AddRoute(Vector3 from, Vector3 to, float size)
        {
            var across = Vector3.Cross(Vector3.up, (to - from).normalized) * size;
            AddQuad(from - across * 0.082f, to - across * 0.082f,
                to + across * 0.082f, from + across * 0.082f, EdgeShadow);
            from += Vector3.up * 0.004f;
            to += Vector3.up * 0.004f;
            AddQuad(from - across * 0.043f, to - across * 0.043f,
                to + across * 0.043f, from + across * 0.043f, RouteEdge);
        }

        private void AddQuad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Color32 color)
        {
            int start = vertices.Count;
            vertices.Add(a);
            vertices.Add(b);
            vertices.Add(c);
            vertices.Add(d);
            for (int i = 0; i < 4; i++)
                colors.Add(color);
            indices.Add(start);
            indices.Add(start + 1);
            indices.Add(start + 2);
            indices.Add(start);
            indices.Add(start + 2);
            indices.Add(start + 3);
        }

        internal void Dispose()
        {
            Object.Destroy(markObject);
            Object.Destroy(mesh);
            Object.Destroy(material);
        }
    }
}
