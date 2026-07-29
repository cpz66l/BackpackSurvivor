using BS.GamePlay.Combat;
using BS.GamePlay.Loot;
using BS.GamePlay.Player;
using System;
using UnityEngine;
using static BS.Data.LootTableData;
namespace BS.GamePlay.Run
{
    public class GameSession : MonoBehaviour
    {
        [SerializeField] private Health playerHealth;
        [SerializeField] private float runDurationSeconds = 900f;
        [SerializeField] private InputReader inputReader;

        private RunTimer timer;
        private GameState state = GameState.NotStarted;

        private int totalXp;
        private int level = 1;

        //对外只读属性，给HUD
        public GameState State => state;
        public float Elapsed => timer.Elapsed;
        public float Remaining => timer.Remaining;
        public float TimeNormalized => timer.Normalized;
        public int TotalXp => totalXp;
        public int Level => level;

        //HUD 要靠事件刷新
        public event Action<GameState> OnStateChanged;
        public event Action<float, float> OnTimeChanged; // elapsed, remaining
        public event Action<int, int> OnXpChanged;       // totalXp, level


        private void Awake()
        {
            if (playerHealth == null)
                playerHealth = FindAnyObjectByType<PlayerController>()?.GetComponent<Health>();
            timer = new RunTimer(runDurationSeconds);
            if(inputReader == null)
                inputReader = FindAnyObjectByType<InputReader>();
        }

        private void OnEnable()
        {
            if(playerHealth != null)
                playerHealth.OnDeath += HandlePlayerDeath;
            XpOrb.OnCollected += HandleXpCollected;
            if (inputReader != null)
                inputReader.OnPause += TogglePause;
        }
        private void OnDisable()
        {
            if (playerHealth != null)
                playerHealth.OnDeath -= HandlePlayerDeath;
            XpOrb.OnCollected -= HandleXpCollected;
            if (inputReader != null)
                inputReader.OnPause -= TogglePause;
            Time.timeScale = 1f;
        }

        private void Start()
        {
            StartRun();
        }

        private void Update()
        {
            if(state != GameState.Running) return;
            timer.Tick(Time.deltaTime);
            OnTimeChanged?.Invoke(timer.Elapsed ,timer.Remaining);
            if (timer.IsFinished)
                SetState(GameState.Victory);
        }

        public void StartRun()
        {
            timer.Reset();
            totalXp = 0;
            level = 1;
            //初始广播，对HUD进行初始化
            SetState(GameState.Running);
            OnXpChanged?.Invoke(totalXp, level);
            OnTimeChanged?.Invoke(timer.Elapsed,timer.Remaining);
        }

        private void SetState(GameState nextState)
        {
            if(state == nextState) return;
            state = nextState;
            OnStateChanged?.Invoke(state);
        }

        private void HandlePlayerDeath()
        {
            if(state != GameState.Running) return ;
            SetState(GameState.Defeat);
        }
        private void HandleXpCollected(LootEntry entry)
        {
            if (entry == null) return;
            if (state != GameState.Running) return;
            totalXp += entry.amount;
            OnXpChanged?.Invoke(totalXp, level);
        }
        private void TogglePause()
        {
            if(state == GameState.Running)
                PauseRun();
            else if (state == GameState.Paused)
                ResumeRun();
        }
        private void PauseRun()
        {
            if(state != GameState.Running) return;
            Time.timeScale = 0f;
            SetState(GameState.Paused);
        }
        private void ResumeRun()
        {
            if (state != GameState.Paused) return;
            Time.timeScale = 1f;
            SetState(GameState.Running);
        }
    }
}
