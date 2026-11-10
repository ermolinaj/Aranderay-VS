using TbsFramework.Grid;
using TbsFramework.Grid.GridStates;
using TbsFramework.Units;
using UnityEngine;

namespace TbsFramework.Gui
{
    public class GUIController : MonoBehaviour
    {
        public CellGrid CellGrid;
        private BattleHud hud;

        private void Start()
        {
            if (CellGrid == null)
                return;
            hud = BattleHud.Create(CellGrid);
            CellGrid.UnitHit += OnUnitHit;
        }

        private void OnDestroy()
        {
            if (CellGrid != null)
                CellGrid.UnitHit -= OnUnitHit;
            hud?.Dispose();
        }

        private void OnUnitHit(object sender, AttackEventArgs hit) => hud?.ShowImpact(hit);
        private void LateUpdate() => hud?.Refresh();

        private void Update()
        {
            if (CellGrid == null)
                return;
            if (Input.GetKeyDown(KeyCode.N))
            {
                CellGrid.EndTurn();//User ends his turn by pressing "n" on keyboard.
            }
            if (Input.GetKeyDown(KeyCode.R))
            {
                CellGrid.ResetLevel();
            }
            if (Input.GetKeyDown(KeyCode.P) && !(CellGrid.CellGridState is CellGridStateBlockInput))
            {
                CellGrid.LoadNextLevel();
            }
            if (Input.GetKeyDown(KeyCode.S))
            {
                CellGrid.MarkZoneOfDanger(1);
            }
        }
    }
}
