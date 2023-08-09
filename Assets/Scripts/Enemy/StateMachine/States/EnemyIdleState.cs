using DG.Tweening;
using PierreMizzi.Useful.StateMachines;
using UnityEngine;

namespace Bitfrost.Gameplay.Enemies
{
    public class EnemyIdleState : AState
    {
        public EnemyIdleState(IStateMachine stateMachine)
            : base(stateMachine)
        {
            type = (int)EnemyStateType.Idle;
        }

        protected Tween m_delayTween;

        protected override void DefaultEnter()
        {
            base.DefaultEnter();
            float delay = Random.Range(1f, 2f);
            m_delayTween = DOVirtual.DelayedCall(
                delay,
                () =>
                {
                    ChangeState((int)EnemyStateType.Move);
                }
            );
        }

        public override void Pause()
        {
            if (m_delayTween != null && m_delayTween.IsPlaying())
                m_delayTween.Pause();
        }

        public override void Resume()
        {
            if (m_delayTween != null && !m_delayTween.IsPlaying())
                m_delayTween.Play();
        }
    }
}
