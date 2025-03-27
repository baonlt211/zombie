using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public enum GameState
    {
        StartMenu,
        Playing,
        GameOver,
        Victory
    }

    private static GameManager _instance;

    public static GameManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<GameManager>();

                if (_instance == null)
                {
                    GameObject newInstance = new GameObject("GameManager");
                    _instance = newInstance.AddComponent<GameManager>();
                }
            }
            return _instance;
        }
    }

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
        }
    }

    [SerializeField] private int m_zombieTarget;

    private GameObject _menuUI = null;
    private AudioSource _ambientAudio = null;
    private Button _playButton = null;
    private TextMeshProUGUI _menuTitle = null;
    private TextMeshProUGUI _playButtonText = null;
    private TextMeshProUGUI _targetText = null;
    private GameState _gameState = GameState.StartMenu;
    public GameState CurrentGameState => _gameState;

    public int ZombieTarget => m_zombieTarget;

    private void Start()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        InitializeUI();
        SetState(GameState.StartMenu);
    }

    public void SetState(GameState gameState)
    {
        _gameState = gameState;
        UpdateMenuUI();

        if (_gameState == GameState.Playing)
        {
            _ambientAudio.Play();
        }
        else
        {
            _ambientAudio.Stop();
        }
    }

    private void UpdateMenuUI()
    {
        _menuUI.SetActive(_gameState != GameState.Playing);

        switch (_gameState)
        {
            case GameState.StartMenu:
                {
                    _menuTitle.SetText("Zombie Survival");
                    _playButtonText.SetText("Start");
                }
                break;
            case GameState.Victory:
                {
                    _menuTitle.SetText("You Win!");
                    _playButtonText.SetText("Restart");
                }
                break;
            case GameState.GameOver:
                {
                    _menuTitle.SetText("Game Over!");
                    _playButtonText.SetText("Try Again");
                }
                break;
        }
    }

    private void InitializeUI()
    {
        _menuUI = GameObject.Find("Menu");
        if (_menuUI == null)
        {
            return;
        }

        _playButton = GameObject.Find("PlayButton")?.GetComponent<Button>();
        _menuTitle = GameObject.Find("MenuTitle")?.GetComponent<TextMeshProUGUI>();
        _playButtonText = GameObject.Find("PlayButtonText")?.GetComponent<TextMeshProUGUI>();
        _targetText = GameObject.Find("Target")?.GetComponent<TextMeshProUGUI>();
        _ambientAudio = GameObject.Find("AmbientSound")?.GetComponent<AudioSource>();

        _targetText.SetText(string.Format("Target: {0}", m_zombieTarget));

        if (_playButton != null)
        {
            _playButton.onClick.RemoveAllListeners();
            _playButton.onClick.AddListener(PlayGame);
        }

        if (_gameState == GameState.Victory || _gameState == GameState.GameOver)
        {
            StartGame();
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        InitializeUI();
    }

    private void PlayGame()
    {
        if (_gameState == GameState.StartMenu)
        {
            StartGame();
        }
        else
        {
            RestartGame();
        }
    }

    public void StartGame()
    {
        SetState(GameState.Playing);
        ZombieManager.Instance.StartSpawnZombies();
    }

    public void RestartGame()
    {
        SceneManager.LoadScene("game");
    }
}
