using System;
using System.Collections.Generic;
using TbsFramework.Cells;
using TbsFramework.Grid.GridStates;
using TbsFramework.Units;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TbsFramework.Grid
{
    internal sealed class GridPointer
    {
        private readonly CellGrid _grid;
        private readonly Dictionary<Sprite, Vector2[][]> _outlines = new Dictionary<Sprite, Vector2[][]>();
        private readonly List<RaycastResult> _uiHits = new List<RaycastResult>();
        private RaycastHit[] _worldHits = new RaycastHit[16];
        private EventSystem _eventSystem;
        private PointerEventData _pointerEvent;
        private CellGridState _hoverState;
        private MonoBehaviour _hovered;

        internal Unit HoveredUnit => _hoverState == _grid.CellGridState ? _hovered as Unit : null;
        internal Cell HoveredCell => _hoverState == _grid.CellGridState ? _hovered as Cell : null;

        public GridPointer(CellGrid grid)
        {
            _grid = grid;
        }

        public void Process(Vector2 position, bool clicked)
        {
            var camera = Camera.main;
            var state = _grid.CellGridState;
            if (state == null || state is CellGridStateBlockInput || camera == null ||
                !camera.pixelRect.Contains(position) || IsOverControl(position))
            {
                SetHover(null);
                return;
            }

            var target = Pick(camera, position, state);
            SetHover(target);
            if (!clicked)
                return;

            if (target is Unit unit)
                unit.PointerDown();
            else if (target is Cell cell)
                cell.PointerDown();
        }

        private bool IsOverControl(Vector2 position)
        {
            var eventSystem = EventSystem.current;
            if (eventSystem == null)
                return false;

            if (_eventSystem != eventSystem)
            {
                _eventSystem = eventSystem;
                _pointerEvent = new PointerEventData(eventSystem);
            }

            _pointerEvent.Reset();
            _pointerEvent.position = position;
            eventSystem.RaycastAll(_pointerEvent, _uiHits);
            foreach (var hit in _uiHits)
            {
                if (hit.module is GraphicRaycaster && hit.gameObject.GetComponentInParent<Selectable>() != null)
                    return true;
            }
            return false;
        }

        private MonoBehaviour Pick(Camera camera, Vector2 position, CellGridState state)
        {
            var ray = camera.ScreenPointToRay(position);
            float distance = (camera.farClipPlane - camera.nearClipPlane) / Vector3.Dot(ray.direction, camera.transform.forward);
            int layers = camera.cullingMask & camera.eventMask & Physics.DefaultRaycastLayers;
            int count = Physics.RaycastNonAlloc(ray, _worldHits, distance, layers);
            while (count == _worldHits.Length)
            {
                Array.Resize(ref _worldHits, _worldHits.Length * 2);
                count = Physics.RaycastNonAlloc(ray, _worldHits, distance, layers);
            }

            Cell nearestCell = null;
            Unit nearestUnit = null;
            float cellDistance = distance;
            float unitDistance = distance;
            for (int i = 0; i < count; i++)
            {
                var hit = _worldHits[i];
                if (hit.distance < cellDistance && hit.collider.TryGetComponent<Cell>(out var cell) &&
                    cell.transform.parent == _grid.transform)
                {
                    nearestCell = cell;
                    cellDistance = hit.distance;
                }
                else if (hit.distance < unitDistance && hit.collider.TryGetComponent<Unit>(out var unit) &&
                    IsVisible(unit, layers) && _grid.Units.Contains(unit) && !unit.TryGetComponent<SpriteRenderer>(out _))
                {
                    nearestUnit = unit;
                    unitDistance = hit.distance;
                }
            }

            foreach (var unit in _grid.Units)
            {
                if (!IsVisible(unit, layers))
                    continue;

                var renderer = (unit as BasicUnit)?.PresentedSprite;
                if (renderer == null && !unit.TryGetComponent(out renderer))
                    continue;
                if (!renderer.enabled || renderer.forceRenderingOff || renderer.color.a <= 0f || renderer.sprite == null)
                    continue;

                if (HitsSprite(ray, renderer, Mathf.Min(cellDistance, unitDistance), out float spriteDistance))
                {
                    nearestUnit = unit;
                    unitDistance = spriteDistance;
                }
            }

            if (nearestUnit != null && unitDistance < cellDistance && !state.PrefersCell(nearestCell, nearestUnit))
                return nearestUnit;

            if (nearestCell != null && IsVisible(nearestCell.CurrentUnit, layers))
                return nearestCell.CurrentUnit;

            return nearestCell;
        }

        internal static bool IsVisible(Unit unit, int layers)
        {
            return unit != null && unit.isActiveAndEnabled && unit.HitPoints > 0 &&
                (layers & (1 << unit.gameObject.layer)) != 0;
        }

        private bool HitsSprite(Ray ray, SpriteRenderer renderer, float limit, out float distance)
        {
            var origin = renderer.transform.InverseTransformPoint(ray.origin);
            var direction = renderer.transform.InverseTransformVector(ray.direction);
            distance = 0f;
            if (Mathf.Abs(direction.z) < 0.00001f)
                return false;

            distance = -origin.z / direction.z;
            if (distance < 0f || distance >= limit)
                return false;

            Vector2 point = origin + direction * distance;
            if (renderer.flipX)
                point.x = -point.x;
            if (renderer.flipY)
                point.y = -point.y;

            var sprite = renderer.sprite;
            if (!sprite.bounds.Contains(new Vector3(point.x, point.y, 0f)))
                return false;

            if (!_outlines.TryGetValue(sprite, out var shapes))
            {
                shapes = new Vector2[sprite.GetPhysicsShapeCount()][];
                var points = new List<Vector2>();
                for (int i = 0; i < shapes.Length; i++)
                {
                    sprite.GetPhysicsShape(i, points);
                    shapes[i] = points.ToArray();
                }
                _outlines.Add(sprite, shapes);
            }

            foreach (var shape in shapes)
            {
                if (ContainsPoint(shape, point))
                    return true;
            }
            return shapes.Length == 0;
        }

        private static bool ContainsPoint(Vector2[] shape, Vector2 point)
        {
            // Each edge crossed by a ray to the right alternates inside and outside.
            bool inside = false;
            for (int i = 0, previous = shape.Length - 1; i < shape.Length; previous = i++)
            {
                var a = shape[previous];
                var b = shape[i];
                if ((a.y > point.y) != (b.y > point.y) &&
                    point.x < a.x + (b.x - a.x) * (point.y - a.y) / (b.y - a.y))
                    inside = !inside;
            }
            return inside;
        }

        private void SetHover(MonoBehaviour target)
        {
            var state = _grid.CellGridState;
            if (_hovered == target && _hoverState == state)
                return;

            if (_hovered != null)
            {
                if (_hovered is Unit previousUnit)
                    previousUnit.PointerExit();
                else if (_hoverState == state && _hovered is Cell previousCell)
                    previousCell.PointerExit();
            }

            _hovered = target;
            _hoverState = state;
            if (target is Unit unit)
                unit.PointerEnter();
            else if (target is Cell cell)
                cell.PointerEnter();
        }
    }
}
