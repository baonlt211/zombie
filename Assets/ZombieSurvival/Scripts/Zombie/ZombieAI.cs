using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class ZombieAI : MonoBehaviour
{
    [SerializeField] private AudioSource m_audioSource;
    [SerializeField] private AudioClip m_deadSfx;
    [SerializeField] private AudioClip m_attackSfx;

    private Transform _player;
    private Animator _animator;
    private CharacterController _controller;
    private ZombieManager _zombieManager;
    private PlayerHealth _playerHealth;
    private ZombieData _zombieData;
    private float _verticalVelocity;
    private float _attackTimer;
    private int _currentHealth;
    private bool _isDead;
    private ZombieManager.DummyZombieData _dummyZombieData;
    private Collider _myCollider;

    public void Initialize(Transform playerTarget, PlayerHealth playerHealthComponent, ZombieManager.DummyZombieData dummyZombieData)
    {
        _player = playerTarget;
        _playerHealth = playerHealthComponent;
        _dummyZombieData = dummyZombieData;
        _zombieData = Resources.Load<ZombieData>("ScriptableObjects/ZombieData_1");
        _controller = GetComponent<CharacterController>();
        _animator = GetComponent<Animator>();
        _zombieManager = FindObjectOfType<ZombieManager>();
        _currentHealth = _zombieData.MaxHealth;
        _myCollider = GetComponent<Collider>();
    }

    public void ManualUpdate()
    {
        if (_player == null || _zombieData == null || _isDead)
        {
            return;
        }

        float distance = Vector3.Distance(transform.position, _player.position);
        Vector3 direction = (_player.position - transform.position).normalized;
        direction.y = 0;
        _controller.enabled = true;

        // Handle attacking the player
        if (distance <= _zombieData.AttackRange)
        {
            _animator.SetBool("IsWalking", false);
            _attackTimer += Time.deltaTime;

            if (_attackTimer >= _zombieData.AttackInterval)
            {
                PlayAttackSfx();
                _animator.SetTrigger("Attack");
                _playerHealth?.TakeDamage(_zombieData.Damage);
                _attackTimer = 0f;
            }

            return;
        }

        _animator.SetBool("IsWalking", true);

        if (IsPathBlocked(direction))
        {
            return;
        }

        Vector3 move = direction * _zombieData.Speed;

        // Gravity for grounded-only movement (no jump)
        if (!_controller.isGrounded)
        {
            _verticalVelocity += Physics.gravity.y * Time.deltaTime;
        }
        else
        {
            _verticalVelocity = -1f; // Small value to keep controller grounded
        }

        move.y = _verticalVelocity;
        _controller.Move(move * Time.deltaTime);
        transform.rotation = Quaternion.LookRotation(direction);
    }

    private bool IsPathBlocked(Vector3 direction)
    {
        // Check for another zombie within a small radius in the forward direction
        Vector3 checkPosition = transform.position + direction.normalized * 0.5f;
        Collider[] colliders = Physics.OverlapSphere(checkPosition, 0.3f);
        foreach (var collider in colliders)
        {
            var name = collider.name;
            if (collider != null && collider.name != _myCollider.name && collider.CompareTag("Zombie"))
            {
                return true; // Another zombie is too close
            }
        }
        return false;
    }

    public void TakeDamage(int damage)
    {
        if (_isDead)
        {
            return;
        }

        _currentHealth -= damage;

        if (_currentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        ++ZombieManager.Instance.KilledZombieCount;
        --ZombieManager.Instance.ActiveZombieCount;
        _isDead = true;
        _animator.SetTrigger("Dead");
        PlayDeadSfx();
        Invoke("ReturnToPool", 1f);

        if (ZombieManager.Instance.KilledZombieCount >= GameManager.Instance.ZombieTarget)
        {
            ZombieManager.Instance.CanSpawnZombies = false;
            GameManager.Instance.SetState(GameManager.GameState.Victory);
        }
    }

    private void ReturnToPool()
    {
        _currentHealth = _zombieData.MaxHealth;
        _isDead = false;
        _zombieManager?.ReturnToPool(gameObject, _dummyZombieData);
    }

    private void PlayDeadSfx()
    {
        if (m_audioSource != null && m_deadSfx != null)
        {
            m_audioSource.PlayOneShot(m_deadSfx);
        }
    }

    private void PlayAttackSfx()
    {
        if (m_audioSource != null && m_attackSfx != null)
        {
            m_audioSource.PlayOneShot(m_attackSfx);
        }
    }
}
