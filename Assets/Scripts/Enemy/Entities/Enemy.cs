using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(HealthEntity))]
public class Enemy : MonoBehaviour, IStateMachine
{
	#region Fields

    [SerializeField]
    protected EnemyType m_type = EnemyType.None;
    public EnemyType type
    {
        get { return m_type; }
    }

    protected HealthEntity m_healthEntity;
    protected EnemyManager m_manager = null;

    [SerializeField]
    protected EnemySettings m_settings;

	#region StateMachine

    public List<AState> states { get; set; } = new List<AState>();
    public AState currentState { get; set; }

    #endregion

	#endregion

	#region Methods

    protected virtual void Awake()
    {
        m_healthEntity = GetComponent<HealthEntity>();
        InitiliazeState();

        ChangeState(EnemyStateType.None, EnemyStateType.Idle);
    }

    protected virtual void Start()
    {
        SubscribeHealth();
    }

    protected virtual void Update()
    {
        UpdateStateMachine();
    }

    #region StateMachine

    public virtual void InitiliazeState()
    {
        states = new List<AState>()
        {
            new EnemyIdleState(this),
            new EnemyMoveState(this),
            new EnemyAttackState(this)
        };
    }

    public void ChangeState(EnemyStateType previousState, EnemyStateType nextState)
    {
        ChangeState((int)previousState, (int)nextState);
    }

    public void ChangeState(int previousState, int nextState)
    {
        if (currentState != null)
            currentState.Exit();

        currentState = states.Find((AState newState) => newState.type == nextState);
        if (currentState != null)
            currentState.Enter(previousState);
        else
        {
            Debug.LogError($"Couldn't find a new state of type : {nextState}. Going GroundedIdle");
        }
    }

    public void UpdateStateMachine()
    {
        if (currentState != null)
            currentState.Update();
    }

	#endregion

    #region Behaviour

    public virtual void Initialize(EnemyManager manager)
    {
        m_healthEntity.maxHealth = m_settings.maxHealth;
        m_healthEntity.Reset();
        m_manager = manager;
    }

    #endregion

        #region Health

    protected virtual void SubscribeHealth()
    {
        m_healthEntity.onLostHealth += CallbackLostHealth;
        m_healthEntity.onNoHealth += CallbackNoHealth;
    }

    protected virtual void UnsubscribeHealth()
    {
        m_healthEntity.onLostHealth -= CallbackLostHealth;
        m_healthEntity.onNoHealth -= CallbackNoHealth;
    }

    protected virtual void CallbackLostHealth() { }

    protected virtual void CallbackNoHealth()
    {
        m_manager.Release(gameObject);
    }

    #endregion



	#endregion
}
