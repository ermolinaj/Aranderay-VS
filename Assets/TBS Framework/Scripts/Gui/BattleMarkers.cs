using System.Collections.Generic;
using TbsFramework.Grid;
using TbsFramework.Grid.GridStates;
using TbsFramework.Units;
using UnityEngine;

namespace TbsFramework.Gui
{
    [RequireComponent(typeof(CanvasRenderer))]
    internal sealed class BattleMarkers : UnityEngine.UI.Graphic
    {
        internal static readonly Color32 Ally = new Color32(126, 228, 196, 255);
        internal static readonly Color32 HoverAlly = new Color32(196, 255, 230, 255);
        internal static readonly Color32 Target = new Color32(255, 137, 108, 255);
        private static readonly Color32 Available = new Color32(255, 171, 141, 212);
        private static readonly Color32 Unavailable = new Color32(185, 178, 169, 223);
        private static readonly Color32 Shadow = new Color32(9, 13, 14, 230);

        private enum Kind { Selected, FriendlyHover, AvailableEnemy, Aim, Blocked, Impact }
        private struct Mark
        {
            internal Vector2 Position;
            internal Kind Kind;
            internal float Progress;
        }

        private List<Mark> visible = new List<Mark>();
        private List<Mark> pending = new List<Mark>();

        internal void Refresh(Camera camera, CellGrid grid, CellGridStateUnitSelected selection, Unit hovered,
            Vector3 hitPosition, float hitProgress)
        {
            pending.Clear();
            if (camera != null)
            {
                var actor = selection?.SelectedUnit;
                if (actor != null)
                {
                    Add(camera, actor.BoardPosition + Vector3.up * 0.40f, Kind.Selected);
                    if (!actor.IsMoving && actor.ActionPoints >= 1)
                    {
                        foreach (var enemy in grid.Units)
                        {
                            if (enemy != hovered && GridPointer.IsVisible(enemy, camera.cullingMask) && selection.CanAttack(enemy))
                                Add(camera, HeadOf(enemy), Kind.AvailableEnemy);
                        }
                    }
                }

                if (hovered != null && hovered != actor)
                {
                    if (hovered.PlayerNumber == grid.CurrentPlayerNumber)
                        Add(camera, HeadOf(hovered), Kind.FriendlyHover);
                    else if (selection != null)
                        Add(camera, SilhouettePosition(hovered) + Vector3.up * 0.28f,
                            selection.CanAttack(hovered) ? Kind.Aim : Kind.Blocked);
                }

                if (hitProgress < 1)
                    Add(camera, hitPosition, Kind.Impact, hitProgress);
            }

            if (Changed())
            {
                (visible, pending) = (pending, visible);
                SetVerticesDirty();
            }
        }

        private static Vector3 HeadOf(Unit unit)
        {
            Renderer renderer = (unit as BasicUnit)?.PresentedSprite;
            if (renderer == null)
                renderer = unit.GetComponent<Renderer>();
            if (renderer != null)
                return new Vector3(renderer.bounds.center.x, renderer.bounds.max.y + 0.20f, renderer.bounds.center.z);
            return unit.transform.position + Vector3.up;
        }

        private static Vector3 SilhouettePosition(Unit unit)
        {
            var sprite = (unit as BasicUnit)?.PresentedSprite;
            return sprite != null ? sprite.transform.position : unit.transform.position;
        }

        private void Add(Camera camera, Vector3 position, Kind kind, float progress = 0)
        {
            var screen = camera.WorldToScreenPoint(position);
            // Board marks stay over the glass.
            if (screen.z <= 0 || !camera.pixelRect.Contains(screen) ||
                screen.x < 0.085f * Screen.width || screen.x > 0.734f * Screen.width ||
                screen.y < 0.16f * Screen.height || screen.y > 0.9f * Screen.height)
                return;

            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, screen, null, out var point))
                pending.Add(new Mark { Position = point, Kind = kind, Progress = progress });
        }

        private bool Changed()
        {
            if (visible.Count != pending.Count)
                return true;
            for (int i = 0; i < visible.Count; i++)
            {
                if (visible[i].Kind != pending[i].Kind || visible[i].Position != pending[i].Position ||
                    visible[i].Progress != pending[i].Progress)
                    return true;
            }
            return false;
        }

        protected override void OnPopulateMesh(UnityEngine.UI.VertexHelper mesh)
        {
            mesh.Clear();
            foreach (var mark in visible)
            {
                Vector2 point = mark.Position;
                switch (mark.Kind)
                {
                    case Kind.Selected:
                        Line(mesh, point + new Vector2(-23, 1), point + new Vector2(-16, -5), 2.5f, Ally);
                        Line(mesh, point + new Vector2(-16, -5), point + new Vector2(-7, -8), 2.5f, Ally);
                        Line(mesh, point + new Vector2(23, 1), point + new Vector2(16, -5), 2.5f, Ally);
                        Line(mesh, point + new Vector2(16, -5), point + new Vector2(7, -8), 2.5f, Ally);
                        break;
                    case Kind.FriendlyHover:
                        Line(mesh, point + new Vector2(-7, 4), point + new Vector2(0, -1), 2.4f, HoverAlly);
                        Line(mesh, point + new Vector2(0, -1), point + new Vector2(7, 4), 2.4f, HoverAlly);
                        break;
                    case Kind.AvailableEnemy:
                        DrawAvailable(mesh, point);
                        break;
                    case Kind.Aim:
                        DrawAim(mesh, point);
                        break;
                    case Kind.Blocked:
                        HudInk.Disc(mesh, point, 8, Shadow);
                        HudInk.Arc(mesh, point, 7.2f, 0, 360, 1.5f, Unavailable);
                        HudInk.Stroke(mesh, point + new Vector2(-5, -5), point + new Vector2(5, 5), 1.5f, Unavailable);
                        break;
                    case Kind.Impact:
                        DrawImpact(mesh, point, mark.Progress);
                        break;
                }
            }
        }

        private static void DrawAvailable(UnityEngine.UI.VertexHelper mesh, Vector2 point)
        {
            var a = point + Vector2.left * 5;
            var b = point + Vector2.up * 5;
            var c = point + Vector2.right * 5;
            var d = point + Vector2.down * 5;
            HudInk.Quad(mesh, a, b, c, d, Shadow);
            Line(mesh, a, b, 1.5f, Available);
            Line(mesh, b, c, 1.5f, Available);
            Line(mesh, c, d, 1.5f, Available);
            Line(mesh, d, a, 1.5f, Available);
        }

        private static void DrawAim(UnityEngine.UI.VertexHelper mesh, Vector2 point)
        {
            for (int x = -1; x <= 1; x += 2)
            for (int y = -1; y <= 1; y += 2)
            {
                var corner = point + new Vector2(x * 19, y * 16);
                Line(mesh, corner - new Vector2(x * 8, 0), corner, 2.6f, Target);
                Line(mesh, corner, corner - new Vector2(0, y * 7), 2.6f, Target);
            }

            HudInk.Disc(mesh, point, 4.4f, Shadow);
            HudInk.Arc(mesh, point, 3.2f, 0, 360, 1.6f, Target);
        }

        private static void DrawImpact(UnityEngine.UI.VertexHelper mesh, Vector2 point, float progress)
        {
            float reach = Mathf.Lerp(7, 32, Mathf.Sqrt(progress));
            var color = new Color32(255, 223, 173, (byte)Mathf.RoundToInt(255 * (1 - progress)));
            for (int i = 0; i < 8; i++)
            {
                float angle = (i * 45 + 18) * Mathf.Deg2Rad;
                var axis = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                HudInk.Stroke(mesh, point + axis * reach, point + axis * (reach + 8), 2.7f, color);
            }
        }

        private static void Line(UnityEngine.UI.VertexHelper mesh, Vector2 from, Vector2 to, float width, Color32 color)
        {
            HudInk.Stroke(mesh, from, to, width + 2.2f, Shadow);
            HudInk.Stroke(mesh, from, to, width, color);
        }
    }

    internal static class HudInk
    {
        internal static void Stroke(UnityEngine.UI.VertexHelper mesh, Vector2 from, Vector2 to, float width, Color32 color)
        {
            var axis = (to - from).normalized;
            var edge = new Vector2(-axis.y, axis.x) * (width * 0.5f);
            Quad(mesh, from - edge, from + edge, to + edge, to - edge, color);
        }

        internal static void Quad(UnityEngine.UI.VertexHelper mesh, Vector2 a, Vector2 b, Vector2 c, Vector2 d, Color32 color)
        {
            int first = mesh.currentVertCount;
            mesh.AddVert(a, color, Vector2.zero);
            mesh.AddVert(b, color, Vector2.zero);
            mesh.AddVert(c, color, Vector2.zero);
            mesh.AddVert(d, color, Vector2.zero);
            mesh.AddTriangle(first, first + 1, first + 2);
            mesh.AddTriangle(first, first + 2, first + 3);
        }

        internal static void Triangle(UnityEngine.UI.VertexHelper mesh, Vector2 a, Vector2 b, Vector2 c, Color32 color)
        {
            int first = mesh.currentVertCount;
            mesh.AddVert(a, color, Vector2.zero);
            mesh.AddVert(b, color, Vector2.zero);
            mesh.AddVert(c, color, Vector2.zero);
            mesh.AddTriangle(first, first + 1, first + 2);
        }

        internal static void Arc(UnityEngine.UI.VertexHelper mesh, Vector2 center, float radius,
            float start, float end, float width, Color32 color)
        {
            int segments = Mathf.CeilToInt((end - start) / 18);
            var from = Point(center, radius, start);
            for (int i = 1; i <= segments; i++)
            {
                var to = Point(center, radius, Mathf.Lerp(start, end, i / (float)segments));
                Stroke(mesh, from, to, width, color);
                from = to;
            }
        }

        internal static void Disc(UnityEngine.UI.VertexHelper mesh, Vector2 center, float radius, Color32 color)
        {
            var from = Point(center, radius, 0);
            for (int i = 1; i <= 18; i++)
            {
                var to = Point(center, radius, i * 20);
                Triangle(mesh, center, from, to, color);
                from = to;
            }
        }

        private static Vector2 Point(Vector2 center, float radius, float degrees)
        {
            float radians = degrees * Mathf.Deg2Rad;
            return center + new Vector2(Mathf.Cos(radians), Mathf.Sin(radians)) * radius;
        }
    }
}
