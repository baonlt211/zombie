using UnityEngine;
using System.Collections.Generic;
using StarterAssets;

public class WeaponController : MonoBehaviour
{
    [SerializeField] private Transform m_firePointMoving;
    [SerializeField] private Transform m_firePointIdle;
    [SerializeField] private GameObject m_fireEffectPrefab;
    [SerializeField] private ThirdPersonController m_thirdPersonController;
    [SerializeField] private WeaponData[] m_weaponDataList;

    private float _timer;
    private Queue<GameObject> _bulletPool = new Queue<GameObject>();
    private Transform _firePoint = null;
    private int _currentWeaponIndex = 0;

    private void Update()
    {
        _timer += Time.deltaTime;
    }

    public void Shoot()
    {
        _firePoint = m_thirdPersonController.IsMoving ? m_firePointMoving : m_firePointIdle;
        var currentWeaponData = m_weaponDataList[_currentWeaponIndex];

        if (_timer >= currentWeaponData.FireRate && _firePoint != null)
        {
            // Create or reuse a bullet
            var bullet = GetBullet();
            bullet.transform.position = _firePoint.position;
            bullet.transform.rotation = _firePoint.rotation;

            // Initialize bullet with WeaponData
            var bulletComponent = bullet.GetComponent<Bullet>();
            if (bulletComponent != null)
            {
                bulletComponent.Initialize(currentWeaponData);
                bulletComponent.Fire(_firePoint.forward);
            }

            // Activate bullet
            bullet.SetActive(true);
            _timer = 0;
        }
    }

    private GameObject GetBullet()
    {
        if (_bulletPool.Count > 0)
        {
            return _bulletPool.Dequeue();
        }

        // If the pool is empty, instantiate a new bullet
        return Instantiate(m_fireEffectPrefab, _firePoint);
    }

    public void ReturnBulletToPool(GameObject bullet)
    {
        bullet.SetActive(false);
        _bulletPool.Enqueue(bullet);
    }

    public void ChangeWeapon()
    {
        _currentWeaponIndex = (_currentWeaponIndex + 1) % m_weaponDataList.Length;
    }
}
