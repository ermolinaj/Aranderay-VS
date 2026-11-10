using System.Collections.Generic;
using TbsFramework.Cells;
using TbsFramework.Units;

namespace TbsFramework.Grid.GridStates
{
    class CellGridStateUnitSelected : CellGridState
    {
        private readonly Unit _unit;
        private HashSet<Cell> _pathsInRange;
        private Cell _hoveredCell;
        private List<Cell> _previewPath;

        internal Unit SelectedUnit => _unit;
        internal IEnumerable<Cell> ReachableCells => _pathsInRange;
        internal IReadOnlyList<Cell> PreviewPath => _previewPath;
        internal bool CanAttack(Unit target) => !_unit.IsMoving && _unit.IsUnitAttackable(target, _unit.Cell);

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

            if (!CanAttack(unit))
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
        }

        public override void OnCellSelected(Cell cell)
        {
            ClearCellPreview();
            _hoveredCell = cell;
            if (!_unit.IsCellMovableTo(cell) || !_pathsInRange.Contains(cell))
            {
                cell.MarkAsHighlighted();
                return;
            }

            _previewPath = _unit.FindPath(_cellGrid.Cells, cell);
            foreach (var step in _previewPath)
                step.MarkAsPath();
        }

        public override void OnStateEnter()
        {
            base.OnStateEnter();

            _unit.OnUnitSelected();
            _pathsInRange = _unit.GetAvailableDestinations(_cellGrid.Cells);
            foreach (var cell in _pathsInRange)
                cell.MarkAsReachable();
        }

        public override void OnStateExit()
        {
            _unit.OnUnitDeselected();
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

    }
}
