using UnityEngine;

public class Bullet : MonoBehaviour
{
    [SerializeField] private GameObject m_effect;
    [SerializeField] private AudioSource m_audioSource;
    [SerializeField] private AudioClip m_hitSfx;

    private Vector3 _direction;
    private Vector3 _startPosition;
    private WeaponData _weaponData;
    private bool _hasHit = false;

    public void Initialize(WeaponData weaponData)
    {
        _weaponData = weaponData;
    }

    public void Fire(Vector3 direction)
    {
        if (_weaponData == null)
        {
            return;
        }

        _direction = direction.normalized;
        _startPosition = transform.position;
        _hasHit = false;

        if (m_effect != null)
        {
            m_effect.SetActive(true);

            if (_weaponData.fireType == WeaponData.FireType.Cone)
            {
                float radius = _weaponData.DamageRange;
                float angle = _weaponData.DamageConeAngle;
                float halfAngleRad = angle * 0.5f * Mathf.Deg2Rad;
                float width = 2f * radius * Mathf.Tan(halfAngleRad);
                m_effect.transform.localScale = new Vector3(width, 1f, radius);
            }
            else // Single
            {
                m_effect.transform.localScale = Vector3.one;
            }
        }
    }

    private void Update()
    {
        if (_weaponData == null || _hasHit)
        {
            return;
        }

        transform.position += _direction * _weaponData.BulletSpeed * Time.deltaTime;

        if (_weaponData.fireType == WeaponData.FireType.Cone)
        {
            float traveled = Vector3.Distance(_startPosition, transform.position);
            if (traveled >= _weaponData.Range)
            {
                ApplyAreaDamageCone();
                ReturnToPool();
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_weaponData == null || _hasHit || _weaponData.fireType != WeaponData.FireType.Single)
        {
            return;
        }

        if (other.CompareTag("Zombie"))
        {
            var zombie = other.GetComponent<ZombieAI>();
            if (zombie != null)
            {
                zombie.TakeDamage(_weaponData.Damage);
            }

            PlayFireSfx();
            _hasHit = true;
            ReturnToPool();
        }
    }

    private void ApplyAreaDamageCone()
    {
        float radius = _weaponData.DamageRange;
        float coneAngle = _weaponData.DamageConeAngle;

        Vector3 origin = transform.position;
        Vector3 forward = _direction;

        Collider[] hitColliders = Physics.OverlapSphere(origin, radius);
        foreach (var hit in hitColliders)
        {
            if (hit.CompareTag("Zombie"))
            {
                Vector3 toTarget = hit.transform.position - origin;
                float angle = Vector3.Angle(forward, toTarget.normalized);

                if (angle <= coneAngle * 0.5f)
                {
                    var zombie = hit.GetComponent<ZombieAI>();
                    if (zombie != null)
                    {
                        PlayFireSfx();
                        zombie.TakeDamage(_weaponData.Damage);
                    }
                }
            }
        }
    }

    private void ReturnToPool()
    {
        if (m_effect != null)
        {
            m_effect.transform.localScale = Vector3.one;
            m_effect.SetActive(false);
        }

        gameObject.SetActive(false);

        var weaponController = FindObjectOfType<WeaponController>();
        if (weaponController != null)
        {
            weaponController.ReturnBulletToPool(gameObject);
        }
    }

    private void PlayFireSfx()
    {
        if (m_audioSource != null && m_hitSfx != null)
        {
            m_audioSource.PlayOneShot(m_hitSfx);
        }
    }
}
