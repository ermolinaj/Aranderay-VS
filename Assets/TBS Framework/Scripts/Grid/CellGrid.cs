using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System;
using TbsFramework.Cells;
using TbsFramework.Grid.GridStates;
using TbsFramework.Grid.UnitGenerators;
using TbsFramework.Players;
using TbsFramework.Units;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TbsFramework.Grid
{
    /// <summary>
    /// CellGrid class keeps track of the game, stores cells, units and players objects. It starts the game and makes turn transitions. 
    /// It reacts to user interacting with units or cells, and raises events related to game progress. 
    /// </summary>
    public class CellGrid : MonoBehaviour
    {
        /// <summary>
        /// LevelLoading event is invoked before Initialize method is run.
        /// </summary>
        public event EventHandler LevelLoading;
        /// <summary>
        /// LevelLoadingDone event is invoked after Initialize method has finished running.
        /// </summary>
        public event EventHandler LevelLoadingDone;
        /// <summary>
        /// GameStarted event is invoked at the beggining of StartGame method.
        /// </summary>
        public event EventHandler GameStarted;
        /// <summary>
        /// GameEnded event is invoked when there is a single player left in the game.
        /// </summary>
        public event EventHandler GameEnded;
        /// <summary>
        /// Turn ended event is invoked at the end of each turn.
        /// </summary>
        public event EventHandler TurnEnded;

        /// <summary>
        /// UnitAdded event is invoked each time AddUnit method is called.
        /// </summary>
        public event EventHandler<UnitCreatedEventArgs> UnitAdded;
        internal event EventHandler<AttackEventArgs> UnitHit;

        private int? _scoutedPlayerNumber;
        private HashSet<Cell> _dangerCells = new HashSet<Cell>();
        private GridPointer _pointer;
        internal Unit HoveredUnit => _pointer?.HoveredUnit;
        internal bool ScoutActive => _scoutedPlayerNumber.HasValue;
        private CellGridState _cellGridState; //The grid delegates some of its behaviours to cellGridState object.
        public CellGridState CellGridState
        {
            get
            {
                return _cellGridState;
            }
            set
            {
                if (_cellGridState != null)
                    _cellGridState.OnStateExit();
                _cellGridState = value;
                _cellGridState.OnStateEnter();
            }
        }

        public int NumberOfPlayers { get; private set; }

        public Player CurrentPlayer
        {
            get { return Players.Find(p => p.PlayerNumber.Equals(CurrentPlayerNumber)); }
        }
        public int CurrentPlayerNumber { get; private set; }

        /// <summary>
        /// GameObject that holds player objects.
        /// </summary>
        public Transform PlayersParent;

        [SerializeField]
        public string NextLevelSceneName;

        public bool GameFinished { get; private set; }
        internal int? WinningPlayer { get; private set; }
        public List<Player> Players { get; private set; }
        public List<Cell> Cells { get; private set; }
        public List<Unit> Units { get; private set; }
        public Text GameFinishedMessage { get; private set; }
        


        private void Start()
        {
            if (LevelLoading != null)
                LevelLoading.Invoke(this, new EventArgs());

            Initialize();
            _pointer = new GridPointer(this);

            if (LevelLoadingDone != null)
                LevelLoadingDone.Invoke(this, new EventArgs());

            StartGame();
        }

        private void Update()
        {
            _pointer?.Process(Input.mousePosition, Input.GetMouseButtonDown(0));
        }

        private void Initialize()
        {
            GameFinished = false;
            Players = new List<Player>();
            for (int i = 0; i < PlayersParent.childCount; i++)
            {
                var player = PlayersParent.GetChild(i).GetComponent<Player>();
                if (player != null)
                    Players.Add(player);
                else
                    Debug.LogError("Invalid object in Players Parent game object");
            }
            NumberOfPlayers = Players.Count;
            CurrentPlayerNumber = Players.Min(p => p.PlayerNumber);

            Cells = new List<Cell>();
            for (int i = 0; i < transform.childCount; i++)
            {
                var cell = transform.GetChild(i).gameObject.GetComponent<Cell>();
                if (cell != null)
                    Cells.Add(cell);
                else
                    Debug.LogError("Invalid object in cells paretn game object");
            }

            foreach (var cell in Cells)
            {
                cell.CellClicked += OnCellClicked;
                cell.CellHighlighted += OnCellHighlighted;
                cell.CellDehighlighted += OnCellDehighlighted;
                if (cell.IsWalkable)
                    cell.GetNeighbours(Cells);
            }

            Units = new List<Unit>();
            var unitGenerator = GetComponent<IUnitGenerator>();
            if (unitGenerator != null)
            {
                var units = unitGenerator.SpawnUnits(Cells);
                foreach (var unit in units)
                {
                    AddUnit(unit.GetComponent<Transform>());
                }
            }
            else
                Debug.LogError("No IUnitGenerator script attached to cell grid");
        }

        private void OnCellDehighlighted(object sender, EventArgs e)
        {
            CellGridState.OnCellDeselected(sender as Cell);
        }
        private void OnCellHighlighted(object sender, EventArgs e)
        {
            CellGridState.OnCellSelected(sender as Cell);
        }
        private void OnCellClicked(object sender, EventArgs e)
        {
            CellGridState.OnCellClicked(sender as Cell);
        }

        private void OnUnitClicked(object sender, EventArgs e)
        {
            CellGridState.OnUnitClicked(sender as Unit);
        }
        private void OnUnitDestroyed(object sender, AttackEventArgs e)
        {
            var unit = (Unit)sender;
            if (!Units.Remove(unit))
                return;

            unit.UnitClicked -= OnUnitClicked;
            unit.UnitAttacked -= OnUnitHit;
            unit.UnitDestroyed -= OnUnitDestroyed;
            unit.UnitMoved -= OnUnitMoved;

            var survivingPlayers = Units.Select(survivor => survivor.PlayerNumber).Distinct().ToList();
            if (survivingPlayers.Count == 1)
                EndGame(survivingPlayers[0]);
            else if (_scoutedPlayerNumber.HasValue)
                RefreshDangerZone();
        }

        private void OnUnitMoved(object sender, MovementEventArgs e)
        {
            if (_scoutedPlayerNumber.HasValue)
                RefreshDangerZone();
        }

        private void OnUnitHit(object sender, AttackEventArgs e) => UnitHit?.Invoke(this, e);
        /// <summary>
        /// Adds unit to the game
        /// </summary>
        /// <param name="unit">Unit to add</param>
        public void AddUnit(Transform unit)
        {
            var addedUnit = unit.GetComponent<Unit>();
            Units.Add(addedUnit);
            addedUnit.UnitClicked += OnUnitClicked;
            addedUnit.UnitAttacked += OnUnitHit;
            addedUnit.UnitDestroyed += OnUnitDestroyed;
            addedUnit.UnitMoved += OnUnitMoved;

            if (UnitAdded != null)
                UnitAdded.Invoke(this, new UnitCreatedEventArgs(unit));
        }

        /// <summary>
        /// Method is called once, at the beggining of the game.
        /// </summary>
        public void StartGame()
        {
            if (GameStarted != null)
                GameStarted.Invoke(this, new EventArgs());

            Units.FindAll(u => u.PlayerNumber.Equals(CurrentPlayerNumber)).ForEach(u => { u.OnTurnStart(); });
            Players.Find(p => p.PlayerNumber.Equals(CurrentPlayerNumber)).Play(this);
            Debug.Log("Game started");
        }
        /// <summary>
        /// Method makes turn transitions. It is called by player at the end of his turn.
        /// </summary>
        public void EndTurn()
        {
            if (CellGridState != null && !(CellGridState is CellGridStateBlockInput))
                EndTurn(CurrentPlayer);
        }

        internal void EndTurn(Player player)
        {
            if (GameFinished || player == null || player != CurrentPlayer || Units.Exists(unit => unit.IsMoving))
                return;

            var survivingPlayers = Players.Where(p => Units.Exists(unit => unit.PlayerNumber == p.PlayerNumber))
                .OrderBy(p => p.PlayerNumber).ToList();
            if (survivingPlayers.Count < 2)
                return;

            var nextPlayer = survivingPlayers.Find(p => p.PlayerNumber > CurrentPlayerNumber) ?? survivingPlayers[0];
            CellGridState = new CellGridStateBlockInput(this);

            Units.FindAll(u => u.PlayerNumber.Equals(CurrentPlayerNumber)).ForEach(u => { u.OnTurnEnd(); });

            CurrentPlayerNumber = nextPlayer.PlayerNumber;

            if (TurnEnded != null)
                TurnEnded.Invoke(this, new EventArgs());

            Debug.Log(string.Format("Player {0} turn", CurrentPlayerNumber));
            Units.FindAll(u => u.PlayerNumber.Equals(CurrentPlayerNumber)).ForEach(u => { u.OnTurnStart(); });
            nextPlayer.Play(this);
        }

        public void ResetLevel()
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        public void LoadNextLevel()
        {
            if (string.IsNullOrEmpty(NextLevelSceneName))
                return;

            if (!Application.CanStreamedLevelBeLoaded(NextLevelSceneName))
            {
                Debug.LogError($"Level destination '{NextLevelSceneName}' is missing from the build scene list.", this);
                return;
            }

            SceneManager.LoadScene(NextLevelSceneName);
        }

        public void MarkZoneOfDanger(int playerNumber)
        {
            if (GameFinished || CellGridState == null || CellGridState is CellGridStateBlockInput ||
                playerNumber == CurrentPlayerNumber)
                return;

            _scoutedPlayerNumber = _scoutedPlayerNumber == playerNumber ? (int?)null : playerNumber;
            RefreshDangerZone();
        }

        private void RefreshDangerZone()
        {
            var dangerCells = new HashSet<Cell>();
            if (_scoutedPlayerNumber.HasValue)
            {
                foreach (var unit in Units)
                {
                    if (unit.PlayerNumber == _scoutedPlayerNumber.Value)
                        dangerCells.UnionWith(unit.GetPosibleDestinations(Cells));
                }
            }

            foreach (var cell in _dangerCells)
            {
                if (!dangerCells.Contains(cell))
                    cell.SetDanger(false);
            }
            foreach (var cell in dangerCells)
            {
                if (!_dangerCells.Contains(cell))
                    cell.SetDanger(true);
            }
            _dangerCells = dangerCells;
        }

        public void EndGame(int playerNumber)
        {
            if (GameFinished)
                return;

            GameFinished = true;
            WinningPlayer = playerNumber;
            _scoutedPlayerNumber = null;
            RefreshDangerZone();
            CellGridState = new CellGridStateBlockInput(this);
            GameEnded?.Invoke(this, EventArgs.Empty);

            string soundName = playerNumber == 0 ? "WinSound" : "LoseSound";
            var audio = GameObject.Find("AudioManager/" + soundName)?.GetComponent<AudioSource>();
            if (audio != null)
                audio.Play();

            if (playerNumber == 0 && !string.IsNullOrEmpty(NextLevelSceneName))
                StartCoroutine(ContinueAfterVictory());
        }

        private IEnumerator ContinueAfterVictory()
        {
            // Give the final hit and the victory readout a moment before the interlude.
            yield return new WaitForSecondsRealtime(0.9f);
            LoadNextLevel();
        }
    }
}

