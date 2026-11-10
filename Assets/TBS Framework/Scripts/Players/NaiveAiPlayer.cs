using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TbsFramework.Cells;
using TbsFramework.Grid;
using TbsFramework.Grid.GridStates;
using TbsFramework.Units;
using UnityEngine;

namespace TbsFramework.Players
{
    /// <summary>
    /// Simple implementation of AI for the game.
    /// </summary>
    public class NaiveAiPlayer : Player
    {
        private readonly System.Random _random = new System.Random();

        public override void Play(CellGrid cellGrid)
        {
            cellGrid.CellGridState = new CellGridStateBlockInput(cellGrid);
            StartCoroutine(TakeTurn(cellGrid));
        }

        private IEnumerator TakeTurn(CellGrid cellGrid)
        {
            var myUnits = cellGrid.Units.Where(unit => unit.PlayerNumber == PlayerNumber)
                .OrderByDescending(unit => unit.Cell.GetNeighbours(cellGrid.Cells).Count(unit.IsCellTraversable))
                .ToList();
            foreach (var unit in myUnits)
            {
                if (cellGrid.GameFinished)
                    yield break;

                var enemies = cellGrid.Units.FindAll(other => other.PlayerNumber != PlayerNumber);
                var targets = enemies.FindAll(enemy => unit.IsUnitAttackable(enemy, unit.Cell));
                if (targets.Count == 0 && unit.MovementPoints > 0 && unit.ActionPoints >= 1)
                {
                    var path = ChooseApproach(unit, cellGrid.Cells, enemies);
                    if (path != null)
                    {
                        unit.Move(path[0], path);
                        while (unit.IsMoving)
                            yield return null;
                        targets = enemies.FindAll(enemy => unit.IsUnitAttackable(enemy, unit.Cell));
                    }
                }

                if (targets.Count > 0)
                {
                    unit.AttackHandler(targets[_random.Next(targets.Count)]);
                    yield return new WaitForSeconds(0.5f);
                }
                else
                {
                    yield return null;
                }
            }
            cellGrid.EndTurn(this);
        }

        private List<Cell> ChooseApproach(Unit unit, List<Cell> cells, List<Unit> enemies)
        {
            var attackPaths = unit.FindAllPaths(cells)
                .Where(entry => entry.Value.Count > 0 &&
                    entry.Value[entry.Value.Count - 1].MovementCost <= unit.MovementPoints &&
                    unit.IsCellMovableTo(entry.Key) &&
                    enemies.Any(enemy => unit.IsUnitAttackable(enemy, entry.Key)))
                .Select(entry => (path: entry.Value, cost: entry.Value.Sum(cell => cell.MovementCost)))
                .ToList();

            var inRange = attackPaths.FindAll(entry => entry.cost <= unit.MovementPoints);
            if (inRange.Count > 0)
                return inRange[_random.Next(inRange.Count)].path;

            var closestPath = attackPaths.OrderBy(entry => entry.cost).FirstOrDefault().path;
            if (closestPath == null)
                return null;

            // The path starts at the destination. Keep the steps the unit can take from its end.
            var remaining = unit.MovementPoints;
            int index = closestPath.Count;
            while (index > 0 && closestPath[index - 1].MovementCost <= remaining)
            {
                index--;
                remaining -= closestPath[index].MovementCost;
            }
            return closestPath.GetRange(index, closestPath.Count - index);
        }
    }
}
