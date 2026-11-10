using UnityEngine;

namespace TbsFramework.Gui
{
    [RequireComponent(typeof(CanvasRenderer))]
    internal sealed class BattleCommand : UnityEngine.UI.Graphic
    {
        internal enum Symbol { Scout, ScoutActive, Advance, Waiting, Retry, Victory }

        private Symbol symbol;
        private static readonly Color32 Recess = new Color32(17, 19, 17, 255);
        private static readonly Color32 Bevel = new Color32(69, 73, 65, 255);
        private static readonly Color32 Face = new Color32(40, 45, 39, 255);
        private static readonly Color32 Etching = new Color32(200, 207, 185, 255);
        private static readonly Color32 Unlit = new Color32(88, 91, 76, 255);

        internal void Show(Symbol next)
        {
            if (next == symbol)
                return;
            symbol = next;
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(UnityEngine.UI.VertexHelper mesh)
        {
            mesh.Clear();
            var frame = rectTransform.rect;
            Color32 accent = symbol == Symbol.ScoutActive || symbol == Symbol.Retry ? BattleMarkers.Target
                : symbol == Symbol.Waiting ? new Color32(237, 196, 129, 255)
                : BattleMarkers.Ally;
            Color32 icon = symbol == Symbol.Scout || symbol == Symbol.Advance ? Etching : accent;

            Plate(mesh, frame, 0, 8, Recess);
            Plate(mesh, frame, 3, 6, Bevel);
            Plate(mesh, frame, 6, 4, Face);
            HudInk.Stroke(mesh, new Vector2(frame.xMin + 10, frame.yMax - 7),
                new Vector2(frame.xMax - 10, frame.yMax - 7), 1.4f, Bevel);

            var lamp = new Vector2(frame.xMin + 23, frame.center.y);
            HudInk.Disc(mesh, lamp, 6, Recess);
            HudInk.Disc(mesh, lamp, 3.5f, symbol == Symbol.Scout ? Unlit : accent);
            var center = frame.center;

            switch (symbol)
            {
                case Symbol.Scout:
                case Symbol.ScoutActive:
                    DrawEye(mesh, center, icon, symbol == Symbol.ScoutActive);
                    break;
                case Symbol.Advance:
                    HudInk.Triangle(mesh, center + new Vector2(-13, 13), center + new Vector2(-13, -13),
                        center + new Vector2(6, 0), icon);
                    HudInk.Stroke(mesh, center + new Vector2(12, -13), center + new Vector2(12, 13), 4, icon);
                    break;
                case Symbol.Waiting:
                    DrawHourglass(mesh, center, icon);
                    break;
                case Symbol.Retry:
                    HudInk.Arc(mesh, center, 13, 142, 438, 3.4f, icon);
                    HudInk.Triangle(mesh, center + new Vector2(-12, 9), center + new Vector2(-18, 3),
                        center + new Vector2(-19, 14), icon);
                    break;
                case Symbol.Victory:
                    HudInk.Stroke(mesh, center + new Vector2(-14, -1), center + new Vector2(-4, -10), 4.2f, icon);
                    HudInk.Stroke(mesh, center + new Vector2(-4, -10), center + new Vector2(15, 12), 4.2f, icon);
                    break;
            }
        }

        private static void Plate(UnityEngine.UI.VertexHelper mesh, Rect rect, float inset, float cut, Color32 color)
        {
            float x = rect.xMin + inset;
            float y = rect.yMin + inset;
            float right = rect.xMax - inset;
            float top = rect.yMax - inset;
            HudInk.Quad(mesh, new Vector2(x, y + cut), new Vector2(x, top - cut),
                new Vector2(right, top - cut), new Vector2(right, y + cut), color);
            HudInk.Quad(mesh, new Vector2(x, top - cut), new Vector2(x + cut, top),
                new Vector2(right - cut, top), new Vector2(right, top - cut), color);
            HudInk.Quad(mesh, new Vector2(x + cut, y), new Vector2(x, y + cut),
                new Vector2(right, y + cut), new Vector2(right - cut, y), color);
        }

        private static void DrawEye(UnityEngine.UI.VertexHelper mesh, Vector2 center, Color32 color, bool active)
        {
            var previous = new Vector2(-19, 0);
            for (int i = 1; i <= 12; i++)
            {
                float t = i / 12f;
                var next = new Vector2(Mathf.Lerp(-19, 19, t), Mathf.Sin(t * Mathf.PI) * 11);
                HudInk.Stroke(mesh, center + previous, center + next, 2.9f, color);
                HudInk.Stroke(mesh, center + new Vector2(previous.x, -previous.y),
                    center + new Vector2(next.x, -next.y), 2.9f, color);
                previous = next;
            }
            HudInk.Arc(mesh, center, 5.6f, 0, 360, 2.4f, color);
            if (active)
                HudInk.Disc(mesh, center, 2.7f, color);
        }

        private static void DrawHourglass(UnityEngine.UI.VertexHelper mesh, Vector2 center, Color32 color)
        {
            HudInk.Stroke(mesh, center + new Vector2(-12, 14), center + new Vector2(12, 14), 2.6f, color);
            HudInk.Stroke(mesh, center + new Vector2(-12, -14), center + new Vector2(12, -14), 2.6f, color);
            HudInk.Stroke(mesh, center + new Vector2(-9, 12), center + new Vector2(9, -12), 2.6f, color);
            HudInk.Stroke(mesh, center + new Vector2(9, 12), center + new Vector2(-9, -12), 2.6f, color);
        }
    }
}
