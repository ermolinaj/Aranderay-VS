using System.Collections.Generic;
using Aranderay;
using TbsFramework.Grid;
using TbsFramework.Grid.GridStates;
using TbsFramework.Units;
using UnityEngine;

namespace TbsFramework.Gui
{
    internal sealed class BattleHud
    {
        private const float ImpactDuration = 0.32f;
        private static readonly Color32 ScoutSource = new Color32(226, 185, 116, 218);
        private readonly CellGrid grid;
        private readonly Canvas screen;
        private readonly BattleMarkers markers;
        private readonly BattleFloor floor;
        private readonly BattleCasualties casualties;
        private readonly Material rimMaterial;
        private readonly UnityEngine.UI.Button endTurnButton;
        private readonly UnityEngine.UI.Button scoutButton;
        private readonly BattleCommand turnCommand;
        private readonly BattleCommand scoutCommand;

        private HashSet<BasicUnit> visibleRims = new HashSet<BasicUnit>();
        private HashSet<BasicUnit> nextRims = new HashSet<BasicUnit>();
        private bool canRetry;
        private float hitAt = float.NegativeInfinity;
        private Vector3 hitPosition;

        internal static BattleHud Create(CellGrid grid)
        {
            UnityEngine.UI.Button endTurn = null;
            UnityEngine.UI.Button scout = null;
            foreach (var button in Object.FindObjectsByType<UnityEngine.UI.Button>(FindObjectsSortMode.None))
            {
                for (int i = 0; i < button.onClick.GetPersistentEventCount(); i++)
                {
                    if (button.onClick.GetPersistentTarget(i) != grid)
                        continue;
                    string method = button.onClick.GetPersistentMethodName(i);
                    if (method == nameof(CellGrid.EndTurn))
                        endTurn = button;
                    else if (method == nameof(CellGrid.MarkZoneOfDanger))
                        scout = button;
                }
            }

            return endTurn != null && scout != null ? new BattleHud(grid, endTurn, scout) : null;
        }

        private BattleHud(CellGrid grid, UnityEngine.UI.Button endTurn, UnityEngine.UI.Button scout)
        {
            this.grid = grid;
            endTurnButton = endTurn;
            scoutButton = scout;

            var overlay = new GameObject("Combat screen UI", typeof(RectTransform), typeof(Canvas),
                typeof(UnityEngine.UI.CanvasScaler));
            overlay.layer = LayerMask.NameToLayer("UI");
            screen = overlay.GetComponent<Canvas>();
            screen.renderMode = RenderMode.ScreenSpaceOverlay;
            screen.sortingOrder = 20;
            var scaler = overlay.GetComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720);
            scaler.matchWidthOrHeight = 0.5f;
            var root = (RectTransform)overlay.transform;

            var markerLayer = TvOverlay.CreateRect(root, "Tactical marks");
            TvOverlay.Place(markerLayer, 0, 0, 1, 1);
            markers = markerLayer.gameObject.AddComponent<BattleMarkers>();
            markers.raycastTarget = false;
            // Both buttons are authored under the TV's control panel.
            turnCommand = AttachCommand(endTurn, 0.54f, 0.83f);
            scoutCommand = AttachCommand(scout, 0.17f, 0.46f);
            endTurn.onClick.AddListener(Retry);

            var shader = Resources.Load<Shader>("CombatFeedback");
            casualties = new BattleCasualties(overlay, shader);
            if (shader != null && shader.isSupported)
            {
                rimMaterial = new Material(shader);
                floor = new BattleFloor(shader);
            }
        }

        private static BattleCommand AttachCommand(UnityEngine.UI.Button button, float bottom, float top)
        {
            foreach (var graphic in button.GetComponentsInChildren<UnityEngine.UI.Graphic>(true))
                graphic.enabled = false;

            var rect = (RectTransform)button.transform;
            TvOverlay.Place(rect, 0.095f, bottom, 0.905f, top);

            var face = TvOverlay.CreateRect(rect, "Command face");
            TvOverlay.Place(face, 0, 0, 1, 1);
            var icon = face.gameObject.AddComponent<BattleCommand>();
            button.targetGraphic = icon;
            button.transition = UnityEngine.UI.Selectable.Transition.ColorTint;
            var colors = button.colors;
            colors.normalColor = new Color32(223, 231, 224, 255);
            colors.highlightedColor = Color.white;
            colors.selectedColor = Color.white;
            colors.pressedColor = new Color32(164, 185, 171, 255);
            colors.disabledColor = new Color32(171, 181, 177, 255);
            colors.colorMultiplier = 1;
            colors.fadeDuration = 0.08f;
            button.colors = colors;
            return icon;
        }

        internal void Refresh()
        {
            var state = grid.CellGridState;
            if (state == null)
                return;

            bool finished = grid.GameFinished;
            bool playerTurn = !finished && grid.CurrentPlayerNumber == 0 && !(state is CellGridStateBlockInput);
            bool moving = grid.Units.Exists(IsMoving);
            canRetry = finished && grid.WinningPlayer != 0;
            endTurnButton.interactable = canRetry || (playerTurn && !moving);
            scoutButton.interactable = playerTurn;
            scoutCommand.Show(grid.ScoutActive ? BattleCommand.Symbol.ScoutActive : BattleCommand.Symbol.Scout);
            turnCommand.Show(finished
                ? (canRetry ? BattleCommand.Symbol.Retry : BattleCommand.Symbol.Victory)
                : (playerTurn ? BattleCommand.Symbol.Advance : BattleCommand.Symbol.Waiting));

            var camera = Camera.main;
            int visibleLayers = camera != null ? camera.cullingMask : 0;
            var selection = playerTurn ? state as CellGridStateUnitSelected : null;
            var hovered = playerTurn && !moving ? grid.HoveredUnit : null;
            if (!GridPointer.IsVisible(hovered, visibleLayers))
                hovered = null;

            floor?.Refresh(selection, visibleLayers);
            RefreshRims(selection, hovered, visibleLayers, playerTurn && !moving);
            casualties.Refresh();
            float sinceImpact = Time.time - hitAt;
            float progress = sinceImpact >= 0 ? Mathf.Clamp01(sinceImpact / ImpactDuration) : 1;
            markers.Refresh(camera, grid, selection, hovered, hitPosition, progress);
        }

        private void RefreshRims(CellGridStateUnitSelected selection, Unit hovered, int visibleLayers, bool canPoint)
        {
            if (rimMaterial == null)
                return;

            rimMaterial.SetFloat("_RimWidth", 2.85f * Mathf.Clamp(screen.scaleFactor, 0.85f, 1.6f));
            if (canPoint && grid.ScoutActive)
            {
                foreach (var source in grid.ScoutSourcesAt(grid.HoveredCell))
                    PaintRim(source, ScoutSource, visibleLayers);
            }

            float cueProgress = (Time.time - grid.EnemyCueAt) / CellGrid.EnemyCueDuration;
            if (!grid.GameFinished && grid.CurrentPlayerNumber != 0 && cueProgress < 1f)
            {
                float alpha = 0.22f + Mathf.Sin(cueProgress * Mathf.PI) * 0.78f;
                PaintRim(grid.CuedEnemy, new Color(1f, 0.91f, 0.73f, alpha), visibleLayers);
            }

            var selected = selection?.SelectedUnit;
            PaintRim(selected, BattleMarkers.Ally, visibleLayers);
            bool friendly = hovered != null && hovered.PlayerNumber == grid.CurrentPlayerNumber;
            if (hovered != selected && (friendly || selection?.CanAttack(hovered) == true))
                PaintRim(hovered, friendly ? BattleMarkers.HoverAlly : BattleMarkers.Target, visibleLayers);

            foreach (var rim in visibleRims)
            {
                if (rim != null && !nextRims.Contains(rim))
                    rim.HideFeedbackRim();
            }
            (visibleRims, nextRims) = (nextRims, visibleRims);
            nextRims.Clear();
        }

        private void PaintRim(Unit unit, Color color, int visibleLayers)
        {
            if (unit is BasicUnit basic && GridPointer.IsVisible(unit, visibleLayers))
            {
                basic.ShowFeedbackRim(rimMaterial, color);
                nextRims.Add(basic);
            }
        }

        internal void ShowImpact(AttackEventArgs hit)
        {
            var camera = Camera.main;
            if (camera == null || (camera.cullingMask & (1 << hit.Defender.gameObject.layer)) == 0)
                return;
            hitPosition = hit.Defender.transform.position + Vector3.up * 0.28f;
            hitAt = Time.time + SilhouetteMotion.ContactTime;
            if (hit.Defender.HitPoints <= 0)
                casualties.Queue(hit.Defender, hitAt, camera.cullingMask);
        }

        internal void Dispose()
        {
            if (endTurnButton != null)
                endTurnButton.onClick.RemoveListener(Retry);
            foreach (var rim in visibleRims)
            {
                if (rim != null)
                    rim.HideFeedbackRim();
            }
            casualties.Dispose();
            floor?.Dispose();
            if (rimMaterial != null)
                Object.Destroy(rimMaterial);
            if (screen != null)
                Object.Destroy(screen.gameObject);
        }

        private static bool IsMoving(Unit unit) => unit.IsMoving;

        private void Retry()
        {
            if (canRetry)
                grid.ResetLevel();
        }
    }
}
