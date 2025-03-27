using StarterAssets;
using UnityEngine;
using UnityEngine.UI;

public class PlayerHealth : MonoBehaviour
{
    [SerializeField] private PlayerHealthData m_playerHealthData;
    [SerializeField] private Slider m_healthBar;
    [SerializeField] private AudioSource m_audioSource;
    [SerializeField] private AudioClip m_deadSfx;
    [SerializeField] private AudioClip m_hurtSfx;

    private int _currentHealth;
    private ThirdPersonController _player = null;

    private void Start()
    {
        _currentHealth = m_playerHealthData.MaxHealth;
        _player = gameObject.GetComponent<ThirdPersonController>();

        if (m_healthBar != null)
        {
            m_healthBar.maxValue = _currentHealth;
            m_healthBar.value = _currentHealth;
        }
    }

    public void TakeDamage(int amount)
    {
        PlayHurtSfx();
        
        _currentHealth -= amount;

        if (m_healthBar != null)
        {
            m_healthBar.value = _currentHealth;
        }

        if (_currentHealth <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        PlayDeadSfx();
        _player.Die();
        ZombieManager.Instance.CanSpawnZombies = false;
        GameManager.Instance.SetState(GameManager.GameState.GameOver);
    }

    private void PlayDeadSfx()
    {
        if (m_audioSource != null && m_deadSfx != null)
        {
            m_audioSource.PlayOneShot(m_deadSfx);
        }
    }

    private void PlayHurtSfx()
    {
        if (m_audioSource != null && m_hurtSfx != null)
        {
            m_audioSource.PlayOneShot(m_hurtSfx);
        }
    }
}
