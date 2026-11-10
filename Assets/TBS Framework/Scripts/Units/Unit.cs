using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections;
using TbsFramework.Cells;
using TbsFramework.Pathfinding.Algorithms;
using TbsFramework.Units.UnitStates;

namespace TbsFramework.Units
{
    /// <summary>
    /// Base class for all units in the game.
    /// </summary>
    [ExecuteInEditMode]
    public abstract class Unit : MonoBehaviour
    {
        Dictionary<Cell, List<Cell>> cachedPaths = null;
        /// <summary>
        /// Invoked when the board pointer clicks this unit.
        /// </summary>
        public event EventHandler UnitClicked;
        /// <summary>
        /// Invoked when the active player selects this unit.
        /// </summary>
        public event EventHandler UnitSelected;
        /// <summary>
        /// Invoked when this unit is no longer selected.
        /// </summary>
        public event EventHandler UnitDeselected;
        /// <summary>
        /// Invoked when the board pointer enters this unit.
        /// </summary>
        public event EventHandler UnitHighlighted;
        /// <summary>
        /// Invoked when the board pointer leaves this unit.
        /// </summary>
        public event EventHandler UnitDehighlighted;
        /// <summary>
        /// UnitAttacked event is invoked when the unit is attacked.
        /// </summary>
        public event EventHandler<AttackEventArgs> UnitAttacked;
        /// <summary>
        /// UnitDestroyed event is invoked when unit's hitpoints drop below 0.
        /// </summary>
        public event EventHandler<AttackEventArgs> UnitDestroyed;
        /// <summary>
        /// UnitMoved event is invoked when unit moves from one cell to another.
        /// </summary>
        public event EventHandler<MovementEventArgs> UnitMoved;

        public UnitState UnitState { get; set; }
        public void SetState(UnitState state)
        {
            UnitState.MakeTransition(state);
        }

        /// <summary>
        /// A list of buffs that are applied to the unit.
        /// </summary>
        public List<Buff> Buffs { get; private set; }

        public int TotalHitPoints { get; private set; }
        public float TotalMovementPoints { get; private set; }
        public float TotalActionPoints { get; private set; }

        /// <summary>
        /// Cell that the unit is currently occupying.
        /// </summary>
        [SerializeField]
        [HideInInspector]
        private Cell cell;
        public Cell Cell
        {
            get
            {
                return cell;
            }
            set
            {
                cell = value;
            }
        }

        public int HitPoints;
        public int AttackRange;
        public int AttackFactor;
        public int DefenceFactor;
        /// <summary>
        /// Determines how far on the grid the unit can move.
        /// </summary>
        [SerializeField]
        private float movementPoints;
        public virtual float MovementPoints
        {
            get
            {
                return movementPoints;
            }
            protected set
            {
                movementPoints = value;
            }
        }
        /// <summary>
        /// Determines speed of movement animation.
        /// </summary>
        public float MovementAnimationSpeed;
        /// <summary>
        /// Determines how many attacks unit can perform in one turn.
        /// </summary>
        [SerializeField]
        private float actionPoints = 1;
        public float ActionPoints
        {
            get
            {
                return actionPoints;
            }
            protected set
            {
                actionPoints = value;
            }
        }

        /// <summary>
        /// Indicates the player that the unit belongs to. 
        /// Should correspoond with PlayerNumber variable on Player script.
        /// </summary>
        public int PlayerNumber;

        /// <summary>
        /// Indicates if movement animation is playing.
        /// </summary>
        public bool IsMoving { get; set; }

        private static DijkstraPathfinding _pathfinder = new DijkstraPathfinding();
        private static IPathfinding _fallbackPathfinder = new AStarPathfinding();

        /// <summary>
        /// Method called after object instantiation to initialize fields etc. 
        /// </summary>
        public virtual void Initialize()
        {
            Buffs = new List<Buff>();

            UnitState = new UnitStateNormal(this);

            TotalHitPoints = HitPoints;
            TotalMovementPoints = MovementPoints;
            TotalActionPoints = ActionPoints;
        }

        internal void PointerDown()
        {
            if (UnitClicked != null)
            {
                UnitClicked.Invoke(this, new EventArgs());
            }
        }
        internal void PointerEnter()
        {
            if (UnitHighlighted != null)
            {
                UnitHighlighted.Invoke(this, new EventArgs());
            }
        }
        internal void PointerExit()
        {
            if (UnitDehighlighted != null)
            {
                UnitDehighlighted.Invoke(this, new EventArgs());
            }
        }

        /// <summary>
        /// Method is called at the start of each turn.
        /// </summary>
        public virtual void OnTurnStart()
        {
            MovementPoints = TotalMovementPoints;
            ActionPoints = TotalActionPoints;

            SetState(new UnitStateMarkedAsFriendly(this));
        }
        /// <summary>
        /// Method is called at the end of each turn.
        /// </summary>
        public virtual void OnTurnEnd()
        {
            cachedPaths = null;
            Buffs.FindAll(b => b.Duration == 0).ForEach(b => { b.Undo(this); });
            Buffs.RemoveAll(b => b.Duration == 0);
            Buffs.ForEach(b => { b.Duration--; });

            SetState(new UnitStateNormal(this));
        }
        /// <summary>
        /// Method is called when units HP drops below 1.
        /// </summary>
        protected virtual void OnDestroyed()
        {
            MarkAsDestroyed();
            Destroy(gameObject);
        }

        /// <summary>
        /// Method is called when unit is selected.
        /// </summary>
        public virtual void OnUnitSelected()
        {
            SetState(new UnitStateMarkedAsSelected(this));
            if (UnitSelected != null)
            {
                UnitSelected.Invoke(this, new EventArgs());
            }
        }
        /// <summary>
        /// Method is called when unit is deselected.
        /// </summary>
        public virtual void OnUnitDeselected()
        {
            SetState(new UnitStateMarkedAsFriendly(this));
            if (UnitDeselected != null)
            {
                UnitDeselected.Invoke(this, new EventArgs());
            }
        }

        /// <summary>
        /// Method indicates if it is possible to attack a unit from given cell.
        /// </summary>
        /// <param name="other">Unit to attack</param>
        /// <param name="sourceCell">Cell to perform an attack from</param>
        /// <returns>Boolean value whether unit can be attacked or not</returns>
        public virtual bool IsUnitAttackable(Unit other, Cell sourceCell)
        {
            return other != null && other.HitPoints > 0
                && other.PlayerNumber != PlayerNumber
                && sourceCell.GetDistance(other.Cell) <= AttackRange
                && ActionPoints >= 1;
        }

        /// <summary>
        /// Method performs an attack on given unit.
        /// </summary>
        public void AttackHandler(Unit unitToAttack)
        {
            if (IsMoving || !IsUnitAttackable(unitToAttack, Cell))
            {
                return;
            }

            AttackAction attackAction = DealDamage(unitToAttack);
            MarkAsAttacking(unitToAttack);
            unitToAttack.DefendHandler(this, attackAction.Damage);
            AttackActionPerformed(attackAction.ActionCost);

            PlaySound(PlayerNumber == 0 ? "HumanAttack" : "CreatureAttack");
        }
        /// <summary>
        /// Method for calculating damage and action points cost of attacking given unit
        /// </summary>
        /// <returns></returns>
        protected virtual AttackAction DealDamage(Unit unitToAttack)
        {
            return new AttackAction(AttackFactor, 1f);
        }
        /// <summary>
        /// Method called after unit performed an attack.
        /// </summary>
        /// <param name="actionCost">Action point cost of performed attack</param>
        protected virtual void AttackActionPerformed(float actionCost)
        {
            ActionPoints -= actionCost;
            if (ActionPoints == 0)
            {
                MovementPoints = 0;
                SetState(new UnitStateMarkedAsFinished(this));
            }
        }

        /// <summary>
        /// Handler method for defending against an attack.
        /// </summary>
        /// <param name="aggressor">Unit that performed the attack</param>
        /// <param name="damage">Amount of damge that the attack caused</param>
        public void DefendHandler(Unit aggressor, int damage)
        {
            if (HitPoints <= 0)
                return;

            MarkAsDefending(aggressor);
            int damageTaken = Defend(aggressor, damage);
            HitPoints -= damageTaken;
            DefenceActionPerformed();

            if (UnitAttacked != null)
            {
                UnitAttacked.Invoke(this, new AttackEventArgs(aggressor, this, damage));
            }
            if (HitPoints <= 0)
            {
                ReleaseCell();
                if (UnitDestroyed != null)
                {
                    UnitDestroyed.Invoke(this, new AttackEventArgs(aggressor, this, damage));
                }
                OnDestroyed();
            }
        }
        /// <summary>
        /// Method for calculating actual damage taken by the unit.
        /// </summary>
        /// <param name="aggressor">Unit that performed the attack</param>
        /// <param name="damage">Amount of damge that the attack caused</param>
        /// <returns>Amount of damage that the unit has taken</returns>        
        protected virtual int Defend(Unit aggressor, int damage)
        {
            return Mathf.Clamp(damage - DefenceFactor, 1, damage);
        }
        /// <summary>
        /// Method callef after unit performed defence.
        /// </summary>
        protected virtual void DefenceActionPerformed() { }

        /// <summary>
        /// Handler method for moving the unit.
        /// </summary>
        /// <param name="destinationCell">Cell to move the unit to</param>
        /// <param name="path">A list of cells, path from source to destination cell</param>
        public virtual void Move(Cell destinationCell, List<Cell> path)
        {
            if (IsMoving || path == null || path.Count == 0 || path[0] != destinationCell ||
                !IsCellMovableTo(destinationCell) || !path.All(IsCellTraversable))
                return;

            var totalMovementCost = path.Sum(h => h.MovementCost);
            if (totalMovementCost > MovementPoints)
                return;

            var originCell = Cell;
            // Keep the unit's placement relative to the tile, even on a rotated grid.
            var cellOffset = transform.position - originCell.transform.position;
            MovementPoints -= totalMovementCost;
            cachedPaths = null;

            ReleaseCell();
            Cell = destinationCell;
            destinationCell.IsTaken = true;
            destinationCell.CurrentUnit = this;

            if (MovementAnimationSpeed > 0)
            {
                PlaySound(PlayerNumber == 0 ? "HumanWalk" : "CreatureWalk");
                StartCoroutine(MovementAnimation(path, cellOffset));
            }
            else
            {
                foreach (var cell in path)
                    RevealOnVisibleCell(cell);
                transform.position = destinationCell.transform.position + cellOffset;
            }

            if (UnitMoved != null)
            {
                UnitMoved.Invoke(this, new MovementEventArgs(originCell, destinationCell, path));
            }
        }

        private static void PlaySound(string soundName)
        {
            var audio = GameObject.Find("AudioManager/" + soundName)?.GetComponent<AudioSource>();
            if (audio != null)
                audio.Play();
        }

        private void RevealOnVisibleCell(Cell cell)
        {
            if (cell.gameObject.layer == 0 && gameObject.layer != 0)
                gameObject.layer = 0;
        }

        protected virtual IEnumerator MovementAnimation(List<Cell> path, Vector3 cellOffset)
        {
            IsMoving = true;
            // Paths are stored from the destination back to the origin.
            for (int i = path.Count - 1; i >= 0; i--)
            {
                var cell = path[i];
                Vector3 destination = cell.transform.position + cellOffset;
                RevealOnVisibleCell(cell);
                while (transform.position != destination)
                {
                    transform.position = Vector3.MoveTowards(transform.position, destination, Time.deltaTime * MovementAnimationSpeed);
                    yield return null;
                }
                transform.position = destination;
            }
            IsMoving = false;
            OnMoveFinished();
        }
        /// <summary>
        /// Method called after movement animation has finished.
        /// </summary>
        protected virtual void OnMoveFinished() { }

        ///<summary>
        /// Method indicates if unit is capable of moving to cell given as parameter.
        /// </summary>
        public virtual bool IsCellMovableTo(Cell cell)
        {
            return cell.IsWalkable && !cell.IsTaken;
        }
        /// <summary>
        /// Method indicates if unit is capable of moving through cell given as parameter.
        /// </summary>
        public virtual bool IsCellTraversable(Cell cell)
        {
            return cell.IsWalkable && !cell.IsTaken;
        }
        /// <summary>
        /// Method returns all cells that the unit is capable of moving to.
        /// </summary>
        public HashSet<Cell> GetAvailableDestinations(List<Cell> cells)
        {
            cachedPaths = GetPathsInRange(cells, MovementPoints);
            return new HashSet<Cell>(cachedPaths.Keys);
        }
        /// <summary>
        /// Method returns all cells that the unit is capable of moving with full movement points.
        /// </summary>
        public HashSet<Cell> GetPosibleDestinations(List<Cell> cells)
        {
            return new HashSet<Cell>(GetPathsInRange(cells, TotalMovementPoints).Keys);
        }

        private Dictionary<Cell, List<Cell>> GetPathsInRange(List<Cell> cells, float movementBudget)
        {
            return FindAllPaths(cells)
                .Where(path => IsCellMovableTo(path.Key) && path.Value.Sum(cell => cell.MovementCost) <= movementBudget)
                .ToDictionary(path => path.Key, path => path.Value);
        }

        internal Dictionary<Cell, List<Cell>> FindAllPaths(List<Cell> cells)
        {
            return _pathfinder.findAllPaths(GetGraphEdges(cells), Cell);
        }

        public List<Cell> FindPath(List<Cell> cells, Cell destination)
        {
            if (Cell.Equals(destination) || !IsCellMovableTo(destination))
                return new List<Cell>();

            if (cachedPaths != null && cachedPaths.ContainsKey(destination))
            {
                return cachedPaths[destination];
            }
            else
            {
                return _fallbackPathfinder.FindPath(GetGraphEdges(cells), Cell, destination);
            }
        }
        /// <summary>
        /// Method returns graph representation of cell grid for pathfinding.
        /// </summary>
        protected virtual Dictionary<Cell, Dictionary<Cell, float>> GetGraphEdges(List<Cell> cells)
        {
            Dictionary<Cell, Dictionary<Cell, float>> ret = new Dictionary<Cell, Dictionary<Cell, float>>();
            foreach (var cell in cells)
            {
                if (IsCellTraversable(cell) || cell.Equals(Cell))
                {
                    ret[cell] = new Dictionary<Cell, float>();
                    foreach (var neighbour in cell.GetNeighbours(cells).FindAll(IsCellTraversable))
                    {
                        ret[cell][neighbour] = neighbour.MovementCost;
                    }
                }
            }
            return ret;
        }

        /// <summary>
        /// Gives visual indication that the unit is under attack.
        /// </summary>
        /// <param name="aggressor">
        /// Unit that is attacking.
        /// </param>
        public abstract void MarkAsDefending(Unit aggressor);
        /// <summary>
        /// Gives visual indication that the unit is attacking.
        /// </summary>
        /// <param name="target">
        /// Unit that is under attack.
        /// </param>
        public abstract void MarkAsAttacking(Unit target);
        /// <summary>
        /// Gives visual indication that the unit is destroyed. It gets called right before the unit game object is
        /// destroyed.
        /// </summary>
        public abstract void MarkAsDestroyed();

        /// <summary>
        /// Method marks unit as current players unit.
        /// </summary>
        public abstract void MarkAsFriendly();
        /// <summary>
        /// Method mark units to indicate user that the unit is in range and can be attacked.
        /// </summary>
        public abstract void MarkAsReachableEnemy();
        /// <summary>
        /// Method marks unit as currently selected, to distinguish it from other units.
        /// </summary>
        public abstract void MarkAsSelected();
        /// <summary>
        /// Method marks unit to indicate user that he can't do anything more with it this turn.
        /// </summary>
        public abstract void MarkAsFinished();
        /// <summary>
        /// Method returns the unit to its base appearance
        /// </summary>
        public abstract void UnMark();

        public void OnDestroy()
        {
            ReleaseCell();
        }

        private void ReleaseCell()
        {
            if (Cell != null && (!Application.isPlaying || Cell.CurrentUnit == this))
            {
                Cell.IsTaken = false;
                Cell.CurrentUnit = null;
            }
        }
    }

    public class AttackAction
    {
        public readonly int Damage;
        public readonly float ActionCost;

        public AttackAction(int damage, float actionCost)
        {
            Damage = damage;
            ActionCost = actionCost;
        }
    }

    public class MovementEventArgs : EventArgs
    {
        public Cell OriginCell;
        public Cell DestinationCell;
        public List<Cell> Path;

        public MovementEventArgs(Cell sourceCell, Cell destinationCell, List<Cell> path)
        {
            OriginCell = sourceCell;
            DestinationCell = destinationCell;
            Path = path;
        }
    }
    public class AttackEventArgs : EventArgs
    {
        public Unit Attacker;
        public Unit Defender;

        public int Damage;

        public AttackEventArgs(Unit attacker, Unit defender, int damage)
        {
            Attacker = attacker;
            Defender = defender;

            Damage = damage;
        }
    }
    public class UnitCreatedEventArgs : EventArgs
    {
        public Transform unit;

        public UnitCreatedEventArgs(Transform unit)
        {
            this.unit = unit;
        }
    }
}
