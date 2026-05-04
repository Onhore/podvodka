using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Центральный менеджер состояний игры.
/// Singleton. Хранит HP, управляет переходами Gameplay ↔ Death ↔ Victory.
/// </summary>
public class GameController : MonoBehaviour
{
    // ── Singleton ────────────────────────────────────────────
    public static GameController Instance { get; private set; }

    // ── Inspector ────────────────────────────────────────────
    [Header("References")]
    public SubmarineController submarine;
    public HpScript            hpScript;
    public FullscreenImage     deathScreen;
    public FullscreenImage     victoryScreen;

    [Header("HP Settings")]
    [Tooltip("Максимальное HP")]
    public int maxHp               = 12;
    [Tooltip("Каждые N единиц урона меняется шаг анимации")]
    public int hpAnimationThreshold = 4;

    // ── Runtime ───────────────────────────────────────────────
    int _hp;

    public enum State { Gameplay, Death, Victory }
    public State CurrentState { get; private set; } = State.Gameplay;

    // ── Unity ─────────────────────────────────────────────────
    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        _hp = maxHp;
        RefreshHpAnimator();
    }

    // ── Публичный API ─────────────────────────────────────────

    /// <summary>Нанести урон игроку</summary>
    public void TakeDamage(int amount)
    {
        if (CurrentState != State.Gameplay) return;
        _hp = Mathf.Max(0, _hp - amount);
        Debug.Log($"[GameController] HP {_hp}/{maxHp}");
        RefreshHpAnimator();
        if (_hp <= 0) SetState(State.Death);
    }

    /// <summary>Восстановить HP</summary>
    public void Heal(int amount)
    {
        _hp = Mathf.Min(maxHp, _hp + amount);
        RefreshHpAnimator();
    }

    /// <summary>Вызвать победный экран (финальный уровень)</summary>
    public void TriggerVictory()
    {
        if (CurrentState != State.Gameplay) return;
        SetState(State.Victory);
    }

    /// <summary>Перезапустить сцену (кнопка на Death screen)</summary>
    public void RestartGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public int   GetHp()           => _hp;
    public float GetHpNormalized() => (float)_hp / maxHp;

    // ── Internal ──────────────────────────────────────────────

    void SetState(State next)
    {
        CurrentState = next;
        switch (next)
        {
            case State.Gameplay:
                if (submarine) submarine.enabled = true;
                break;

            case State.Death:
                if (submarine) submarine.enabled = false;
                if (deathScreen) deathScreen.Open();
                Debug.Log("[GameController] → DEATH");
                break;

            case State.Victory:
                if (submarine) submarine.enabled = false;
                if (victoryScreen) victoryScreen.Open();
                Debug.Log("[GameController] → VICTORY");
                break;
        }
    }

    void RefreshHpAnimator()
    {
        if (hpScript == null || hpScript.animator == null) return;
        // Пример: maxHp=12, threshold=4 → шаги 3,2,1,0
        int step = _hp / hpAnimationThreshold;
        hpScript.animator.SetInteger("hp", step);
    }
}
