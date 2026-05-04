using UnityEngine;

/// <summary>
/// Центральный менеджер состояний игры.
/// Управляет переходами: Gameplay → Death → Victory.
/// </summary>
public class GameController : MonoBehaviour
{
    public static GameController Instance { get; private set; }

    [Header("References")]
    public SubmarineController submarine;
    public HpScript hpScript;
    public FullscreenImage deathScreen;
    public FullscreenImage victoryScreen;

    [Header("HP Settings")]
    [Tooltip("Максимальное HP игрока")]
    public int maxHp = 12;

    [Tooltip("Сколько урона меняет шаг анимации HP (threshold=4 → 3 шага при maxHp=12)")]
    public int hpAnimationThreshold = 4;

    private int currentHp;

    public GameState CurrentState { get; private set; } = GameState.Gameplay;

    public enum GameState { Gameplay, Death, Victory }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        currentHp = maxHp;
        // Скрываем экраны смерти/победы при старте
        if (deathScreen != null && deathScreen.IsOpen)   deathScreen.Close();
        if (victoryScreen != null && victoryScreen.IsOpen) victoryScreen.Close();
    }

    // ─── СОСТОЯНИЯ ────────────────────────────────────────

    public void SetState(GameState newState)
    {
        CurrentState = newState;

        switch (newState)
        {
            case GameState.Gameplay:
                if (deathScreen != null && deathScreen.IsOpen)     deathScreen.Close();
                if (victoryScreen != null && victoryScreen.IsOpen) victoryScreen.Close();
                if (submarine != null) submarine.enabled = true;
                break;

            case GameState.Death:
                if (submarine != null) submarine.enabled = false;
                if (deathScreen != null) deathScreen.Open();
                Debug.Log("[GameController] → DEATH");
                break;

            case GameState.Victory:
                if (submarine != null) submarine.enabled = false;
                if (victoryScreen != null) victoryScreen.Open();
                Debug.Log("[GameController] → VICTORY");
                break;
        }
    }

    // ─── HP ──────────────────────────────────────────────

    public void TakeDamage(int amount)
    {
        if (CurrentState != GameState.Gameplay) return;

        currentHp = Mathf.Max(0, currentHp - amount);
        Debug.Log($"[GameController] HP: {currentHp}/{maxHp}");
        UpdateHpAnimator();

        if (currentHp <= 0) SetState(GameState.Death);
    }

    public void HealDamage(int amount)
    {
        currentHp = Mathf.Min(maxHp, currentHp + amount);
        UpdateHpAnimator();
    }

    private void UpdateHpAnimator()
    {
        if (hpScript == null) return;
        // Шаг анимации: 12 HP / threshold 4 = шаги 0,1,2,3
        int animStep = currentHp / hpAnimationThreshold;
        hpScript.animator.SetInteger("hp", animStep);
    }

    public int GetCurrentHp() => currentHp;
    public float GetHpNormalized() => (float)currentHp / maxHp;

    // ─── ПОБЕДА ──────────────────────────────────────────

    public void TriggerVictory()
    {
        if (CurrentState != GameState.Gameplay) return;
        SetState(GameState.Victory);
    }

    // ─── РЕСТАРТ ─────────────────────────────────────────

    public void RestartGame()
    {
        currentHp = maxHp;
        UpdateHpAnimator();
        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex
        );
    }
}
