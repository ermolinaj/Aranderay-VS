using System;
using TbsFramework.Grid;
using TbsFramework.Grid.GridStates;
using UnityEngine;

namespace TbsFramework.Gui
{
    public class GUIController : MonoBehaviour
    {
        public CellGrid CellGrid;

        void Awake()
        {
            CellGrid.LevelLoading += onLevelLoading;
            CellGrid.LevelLoadingDone += onLevelLoadingDone;
        }

        private void onLevelLoading(object sender, EventArgs e)
        {
            Debug.Log("Level is loading");
        }

        private void onLevelLoadingDone(object sender, EventArgs e)
        {
            Debug.Log("Level loading done");
            Debug.Log("Press 'n' to end turn");
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.N) && !(CellGrid.CellGridState is CellGridStateBlockInput))
            {
                CellGrid.EndTurn();//User ends his turn by pressing "n" on keyboard.
            }
            if (Input.GetKeyDown(KeyCode.R) && !(CellGrid.CellGridState is CellGridStateBlockInput))
            {
                CellGrid.ResetLevel();
            }
            if (Input.GetKeyDown(KeyCode.P) && !(CellGrid.CellGridState is CellGridStateBlockInput))
            {
                CellGrid.LoadNextLevel();
            }
            if (Input.GetKeyDown(KeyCode.S) && !(CellGrid.CellGridState is CellGridStateBlockInput))
            {
                CellGrid.MarkZoneOfDanger(1);
            }
        }
    }
}