using UnityEngine;
using PierreMizzi.Useful;
using Bitfrost.Gameplay.Bullets;
using Bitfrost.Gameplay.Energy;

namespace Bitfrost.Gameplay.Turrets
{
    public class TurretBullet : Bullet
    {
        [Header("Turret Bullet")]
        [SerializeField]
        private TurretSettings m_settings = null;

        private CrystalShard m_originCrystal;

        public override void AssignLauncher(IBulletLauncher launcher)
        {
            base.AssignLauncher(launcher);
            Turret turret = m_launcher.gameObject.GetComponent<Turret>();
            m_originCrystal = turret.crystal;
            m_speed = m_settings.bulletSpeed;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (m_hasHit)
                return;

            if (UtilsClass.CheckLayer(m_collisionFilter.layerMask.value, other.gameObject.layer))
            {
                if (other.gameObject.TryGetComponent(out CrystalShard crystal))
                {
                    if (crystal == m_originCrystal)
                        return;

                    HitCrystal(crystal);
                }
                else if (other.gameObject.TryGetComponent(out HealthEntity healthEntity))
                    HitHealthEntity(healthEntity);
            }
        }

        private void HitCrystal(CrystalShard crystal)
        {
            crystal.DecrementEnergy();
            Release();
            m_hasHit = true;
        }

        private void HitHealthEntity(HealthEntity healthEntity)
        {
            healthEntity.LoseHealth(m_settings.bulletDamage);
            Release();
            m_hasHit = true;
        }
    }
}