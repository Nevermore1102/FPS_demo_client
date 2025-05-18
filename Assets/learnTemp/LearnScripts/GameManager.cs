using UnityEngine;
using UnityEngine.SceneManagement;

namespace Unity.FPS.zzy.player
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("游戏设置")]
        public int playerHealth = 100;
        public int playerScore = 0;
        public bool isGameOver = false;

        [Header("UI引用")]
        public GameObject gameOverUI;
        public GameObject pauseMenuUI;

        private void Awake()
        {
            // 单例模式
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            // 初始化游戏
            InitializeGame();
        }

        private void Update()
        {
            // 检查游戏状态
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                TogglePause();
            }

            if (playerHealth <= 0 && !isGameOver)
            {
                GameOver();
            }
        }

        private void InitializeGame()
        {
            // 初始化游戏状态
            playerHealth = 100;
            playerScore = 0;
            isGameOver = false;

            // 初始化UI
            if (gameOverUI != null) gameOverUI.SetActive(false);
            if (pauseMenuUI != null) pauseMenuUI.SetActive(false);

            // 初始化光标状态
            // CursorIgnore.Initialize();
        }

        public void TakeDamage(int damage)
        {
            playerHealth -= damage;
            // 可以在这里添加受伤效果、声音等
        }

        public void AddScore(int points)
        {
            playerScore += points;
            // 可以在这里添加得分效果、声音等
        }

        private void GameOver()
        {
            isGameOver = true;
            if (gameOverUI != null)
            {
                gameOverUI.SetActive(true);
            }

        }

        public void RestartGame()
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        public void QuitGame()
        {
            #if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
            #else
                Application.Quit();
            #endif
        }

        private void TogglePause()
        {
            bool isPaused = Time.timeScale == 0;
            Time.timeScale = isPaused ? 1 : 0;
            if (pauseMenuUI != null)
            {
                pauseMenuUI.SetActive(!isPaused);
            }
          
        }
    }
}