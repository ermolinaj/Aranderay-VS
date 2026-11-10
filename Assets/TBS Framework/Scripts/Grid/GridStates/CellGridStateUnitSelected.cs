using System.Collections.Generic;
using TbsFramework.Cells;
using TbsFramework.Units;

namespace TbsFramework.Grid.GridStates
{
    class CellGridStateUnitSelected : CellGridState
    {
        private readonly Unit _unit;
        private HashSet<Cell> _pathsInRange;
        private readonly List<Unit> _markedTargets = new List<Unit>();
        private Cell _hoveredCell;
        private List<Cell> _previewPath;

        public CellGridStateUnitSelected(CellGrid cellGrid, Unit unit) : base(cellGrid)
        {
            _unit = unit;
        }

        internal override bool PrefersCell(Cell cell, Unit unit)
        {
            return unit == _unit && cell != null && !_unit.IsMoving &&
                _pathsInRange.Contains(cell) && _unit.IsCellMovableTo(cell);
        }

        public override void OnCellClicked(Cell cell)
        {
            if (_unit.IsMoving)
                return;
            if (!_unit.IsCellMovableTo(cell) || !_pathsInRange.Contains(cell))
            {
                _cellGrid.CellGridState = new CellGridStateWaitingForInput(_cellGrid);
                return;
            }

            var path = _unit.FindPath(_cellGrid.Cells, cell);
            _unit.Move(cell, path);
            _cellGrid.CellGridState = new CellGridStateUnitSelected(_cellGrid, _unit);
        }
        public override void OnUnitClicked(Unit unit)
        {
            if (unit == _unit || _unit.IsMoving)
                return;

            if (unit.PlayerNumber == _unit.PlayerNumber)
            {
                _cellGrid.CellGridState = new CellGridStateUnitSelected(_cellGrid, unit);
                return;
            }

            if (!_unit.IsUnitAttackable(unit, _unit.Cell))
                return;

            _unit.AttackHandler(unit);
            if (!_cellGrid.GameFinished)
                _cellGrid.CellGridState = new CellGridStateUnitSelected(_cellGrid, _unit);
        }

        public override void OnCellDeselected(Cell cell)
        {
            if (_hoveredCell != cell)
                return;

            ClearCellPreview();
            ShowTargetsFrom(_unit.Cell);
        }

        public override void OnCellSelected(Cell cell)
        {
            ClearCellPreview();
            _hoveredCell = cell;
            if (!_unit.IsCellMovableTo(cell) || !_pathsInRange.Contains(cell))
            {
                cell.MarkAsHighlighted();
                ShowTargetsFrom(_unit.Cell);
                return;
            }

            _previewPath = _unit.FindPath(_cellGrid.Cells, cell);
            foreach (var step in _previewPath)
                step.MarkAsPath();
            ShowTargetsFrom(cell);
        }

        public override void OnStateEnter()
        {
            base.OnStateEnter();

            _unit.OnUnitSelected();
            _pathsInRange = _unit.GetAvailableDestinations(_cellGrid.Cells);
            foreach (var cell in _pathsInRange)
                cell.MarkAsReachable();
            ShowTargetsFrom(_unit.Cell);
        }

        public override void OnStateExit()
        {
            _unit.OnUnitDeselected();
            ClearTargets();
        }

        private void ClearCellPreview()
        {
            if (_hoveredCell != null)
                RestoreCellMark(_hoveredCell);
            _hoveredCell = null;

            if (_previewPath != null)
            {
                foreach (var cell in _previewPath)
                    RestoreCellMark(cell);
                _previewPath = null;
            }
        }

        private void RestoreCellMark(Cell cell)
        {
            if (_pathsInRange.Contains(cell))
                cell.MarkAsReachable();
            else
                cell.UnMark();
        }

        private void ShowTargetsFrom(Cell origin)
        {
            ClearTargets();
            foreach (var target in _cellGrid.Units)
            {
                if (!_unit.IsUnitAttackable(target, origin))
                    continue;

                target.MarkAsReachableEnemy();
                _markedTargets.Add(target);
            }
        }

        private void ClearTargets()
        {
            foreach (var target in _markedTargets)
            {
                if (target != null)
                    target.UnitState.Apply();
            }
            _markedTargets.Clear();
        }
    }
}
