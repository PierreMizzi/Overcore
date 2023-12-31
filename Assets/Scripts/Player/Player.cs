using System;
using System.Collections.Generic;
using Bitfrost.Gameplay.Turrets;
using PierreMizzi.Pause;
using PierreMizzi.SoundManager;
using UnityEngine;

namespace Bitfrost.Gameplay.Players
{

    [RequireComponent(typeof(PlayerController))]
    [RequireComponent(typeof(HealthEntity))]
    public class Player : MonoBehaviour, IPausable
    {
        #region Fields

        [SerializeField]
        private LevelChannel m_levelChannel;

        private Animator m_animator;

        private const string k_isDead = "IsDead";

        private HealthEntity m_healthEntity;

        public HealthEntity healthEntity { get { return m_healthEntity; } }

        public bool isPaused { get; set; }

        [SerializeField]
        private PlayerSettings m_settings;

        public PlayerSettings settings => m_settings;

        [SerializeField]
        private TurretSettings m_turretSettings;
        public TurretSettings turretSettings => m_turretSettings;

        private PlayerController m_controller;

        private List<string> m_hitSounds = new List<string>();

        #endregion

        #region Methods

        private void Awake()
        {
            m_levelChannel.player = this;

            m_controller = GetComponent<PlayerController>();
            m_healthEntity = GetComponent<HealthEntity>();

            m_hitSounds = new List<string>(){
                SoundDataID.PLAYER_HIT_01,
                SoundDataID.PLAYER_HIT_02,
            };

            m_animator = GetComponent<Animator>();
        }

        private void Start()
        {
            m_healthEntity.Initialize(m_settings.maxHealth);
            SubscribeHealthEntity();

            if (m_levelChannel != null)
            {
                m_levelChannel.onDisablePlayerControls += DisableControls;

                m_levelChannel.onReset += CallbackReset;
                m_levelChannel.onPauseGame += Pause;
                m_levelChannel.onResumeGame += Resume;

                m_levelChannel.onTurretRetrieved += CallbackTurretRetrieved;
            }
        }


        private void OnDestroy()
        {
            UnsubscribeHealthEntity();

            if (m_levelChannel != null)
            {
                m_levelChannel.onDisablePlayerControls -= DisableControls;

                m_levelChannel.onReset -= CallbackReset;
                m_levelChannel.onPauseGame -= Pause;
                m_levelChannel.onResumeGame -= Resume;

                m_levelChannel.onTurretRetrieved -= CallbackTurretRetrieved;
            }
        }

        #region Health

        private void SubscribeHealthEntity()
        {
            m_healthEntity.onLostHealth += CallbackLostHealth;
            m_healthEntity.onHealedHealth += CallbackHealedHealth;
        }

        private void UnsubscribeHealthEntity()
        {
            m_healthEntity.onLostHealth -= CallbackLostHealth;
            m_healthEntity.onHealedHealth -= CallbackHealedHealth;
        }

        private void CallbackHealedHealth()
        {
            SoundManager.PlaySFX(SoundDataID.PLAYER_HEALED);
        }

        private void CallbackLostHealth()
        {
            m_levelChannel.onPlayerHit.Invoke();
            SoundManager.PlayRandomSFX(m_hitSounds);
        }

        #endregion

        #region Death

        public void SetDead()
        {
            m_animator.SetBool(k_isDead, true);
        }

        public void SetAlive()
        {
            m_animator.SetBool(k_isDead, false);
        }

        // Function called at the end of Dead.anim
        public void CallbackDeadAnimation()
        {
            m_levelChannel.onGameOver.Invoke();
        }

        #endregion

        #region Reset - Restart

        public void CallbackReset()
        {
            SetAlive();
            m_healthEntity.Reset();
            transform.position = Vector3.zero;
            EnableControls();
        }

        public void Pause()
        {
            m_controller.Pause();
            m_animator.speed = 0;
        }

        public void Resume()
        {
            m_controller.Resume();
            m_animator.speed = 1;
        }

        #endregion

        private void DisableControls()
        {
            m_controller.enabled = false;
        }

        private void EnableControls()
        {
            m_controller.enabled = true;
        }

        private void CallbackTurretRetrieved(int storedEnergy)
        {
            if (!m_healthEntity.isMaxHealth && storedEnergy > 0)
                m_healthEntity.GainHealth(storedEnergy * m_settings.healedHealthPerStoredEnergy);
        }

        #endregion
    }
}
