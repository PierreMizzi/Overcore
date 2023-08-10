using UnityEngine;
using System;

namespace Bitfrost.Gameplay
{

    public class HealthEntity : MonoBehaviour
    {
        #region Fields

        public float currentHealth { get; private set; }
        public float maxHealth { get; set; }

        public float normalizedHealth
        {
            get { return Mathf.Clamp01(currentHealth / maxHealth); }
        }

        public bool isMaxHealth
        {
            get
            {
                return currentHealth == maxHealth;
            }
        }

        public Action onLostHealth = null;
        public Action onHealedHealth = null;
        public Action onNoHealth = null;

        public Action onChangeHealth = null;

        #endregion

        #region Methods

        private void Awake()
        {
            onLostHealth = () => { };
            onHealedHealth = () => { };
            onNoHealth = () => { };
            onChangeHealth = () => { };
        }

        public void Initialize(float maxHealth)
        {
            this.maxHealth = maxHealth;
            Reset();
        }

        public void Reset()
        {
            currentHealth = maxHealth;
            onChangeHealth?.Invoke();
        }

        public void LoseHealth(float lost)
        {
            currentHealth -= lost;
            onLostHealth.Invoke();
            onChangeHealth.Invoke();

            if (currentHealth <= 0)
            {
                currentHealth = 0;
                onNoHealth.Invoke();
            }
        }

        public void HealHealth(float healed)
        {
            currentHealth += healed;
            onHealedHealth.Invoke();
            onChangeHealth.Invoke();

            if (currentHealth > maxHealth)
                currentHealth = maxHealth;
        }

        #endregion
    }
}
