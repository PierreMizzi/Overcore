using System;
using System.Collections.Generic;
using PierreMizzi.SoundManager;
using PierreMizzi.Useful;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

/*

    - Controller : Inputs from mouse, checks closer Module
    - Model : Manages available controllers
    - View : Creates ModuleHUD VisualElements, links them to Module

*/

public class ModuleManager : MonoBehaviour, IPausable
{
    #region Fields

    [SerializeField]
    private LevelChannel m_levelChannel = null;

    public bool isPaused { get; set; }

    #region Inputs

    private Camera m_camera;

    [SerializeField]
    private ModuleSettings m_settings = null;

    [Header("Inputs")]

    [SerializeField]
    private InputActionReference m_mousePositionInput = null;

    [SerializeField]
    private InputActionReference m_dropRetrieveTurretInput = null;

    [SerializeField]
    private InputActionReference m_fireInput = null;

    [SerializeField]
    private InputActionReference m_switchModeInput = null;

    #endregion

    #region Modules

    [Header("Modules")]
    [SerializeField]
    private Module m_modulePrefab = null;

    [SerializeField]
    private Transform m_moduleContainer = null;

    [SerializeField]
    private List<Module> m_turrets = new List<Module>();

    private int m_remainingModuleCount = 0;

    private bool hasAvailableTurret
    {
        get { return m_remainingModuleCount > 0; }
    }

    #endregion

    #region Module Views

    [Header("Turret Views")]
    [SerializeField]
    private UIDocument m_document = null;

    private const string k_moduleViewVisualContainer = "module-container";

    public VisualElement m_moduleViewsContainer;

    [SerializeField]
    private VisualTreeAsset m_moduleViewTemplate;

    public List<ModuleView> m_moduleViews = new List<ModuleView>();

    #endregion

    #region Module Target

    [Header("Module Target")]
    [SerializeField]
    private ModuleTargeter m_moduleTargeter;

    [SerializeField]
    private ContactFilter2D m_targetFilter;

    private ATarget m_currentTarget;

    List<RaycastHit2D> potentialTargets = new List<RaycastHit2D>();

    #endregion

    #endregion

    #region Methods

    #region MonoBehaviour

    private void OnEnable()
    {
        m_camera = Camera.main;
    }

    private void Start()
    {
        m_remainingModuleCount = m_settings.startingModuleCount;
        SubscribeInputs();

        m_moduleViewsContainer = m_document.rootVisualElement.Q(k_moduleViewVisualContainer);

        CreateTurret();

        if (m_levelChannel != null)
        {
            m_levelChannel.onReset += CallbackReset;
            m_levelChannel.onPauseGame += Pause;
            m_levelChannel.onResumeGame += Resume;
        }

    }

    private void OnDestroy()
    {
        UnsubscribeInputs();

        if (m_levelChannel != null)
        {
            m_levelChannel.onReset -= CallbackReset;
            m_levelChannel.onPauseGame -= Pause;
            m_levelChannel.onResumeGame -= Resume;
        }
    }

    #endregion

    #region Inputs

    private void SubscribeInputs()
    {
        if (m_mousePositionInput != null)
            m_mousePositionInput.action.performed += CallbackMousePosition;

        if (m_dropRetrieveTurretInput != null)
            m_dropRetrieveTurretInput.action.performed += CallbackDropRetrieveTurret;

        if (m_fireInput != null)
            m_fireInput.action.performed += CallbackFire;

        if (m_switchModeInput != null)
            m_switchModeInput.action.performed += CallbackSwitchMode;
    }



    private void UnsubscribeInputs()
    {
        if (m_mousePositionInput != null)
            m_mousePositionInput.action.performed -= CallbackMousePosition;

        if (m_dropRetrieveTurretInput != null)
            m_dropRetrieveTurretInput.action.performed -= CallbackDropRetrieveTurret;

        if (m_fireInput != null)
            m_fireInput.action.performed -= CallbackFire;

        if (m_switchModeInput != null)
            m_switchModeInput.action.performed -= CallbackSwitchMode;
    }

    private void CallbackDropRetrieveTurret(InputAction.CallbackContext context)
    {
        if (m_currentTarget == null)
            return;

        switch (m_currentTarget.type)
        {
            case TargetType.CrystalShard:
                DropTurret();
                break;
            case TargetType.Turret:
                RetrieveTurret();
                break;
            default:
                break;
        }
    }

    private void CallbackFire(InputAction.CallbackContext context)
    {
        foreach (Module turret in m_turrets)
            turret.Fire();
    }

    private void CallbackSwitchMode(InputAction.CallbackContext context)
    {
        if (m_currentTarget == null)
            return;

        switch (m_currentTarget.type)
        {
            case TargetType.Turret:
                SwitchTurretMode();
                break;
            default:
                break;
        }
    }

    private void SwitchTurretMode()
    {
        Module turret = ((ModuleTarget)m_currentTarget).turret;

        turret.SwitchMode();
    }

    #endregion

    #region Turret

    private void CreateTurret()
    {
        // Instantiate Model
        Module turret = Instantiate(m_modulePrefab, m_moduleContainer);
        turret.Initialize(this);
        m_turrets.Add(turret);

        // Instiate View, link it to model
        ModuleView turretView = new ModuleView();
        VisualElement turretViewVisual = m_moduleViewTemplate.Instantiate();
        m_moduleViewsContainer.Add(turretViewVisual);
        turretView.Initialize(turret, turretViewVisual);
        m_moduleViews.Add(turretView);
    }

    private void DropTurret()
    {
        CrystalShard crystal = ((CrystalShardTarget)m_currentTarget).crystal;

        if (crystal.isAvailable)
        {
            if (hasAvailableTurret)
            {
                Module turret = GetInactiveTurret();

                if (turret != null)
                {
                    turret.AssignCrystal(crystal);
                    turret.ChangeState(TurretStateType.Offensive);
                    SoundManager.PlaySound(SoundDataIDStatic.TURRET_DROP);
                }
            }
            else
            { /* TODO : Redeploy Turret*/
            }
        }
    }

    private void RetrieveTurret()
    {
        Module turret = ((ModuleTarget)m_currentTarget).turret;

        turret.RemoveCrystal();
        turret.ChangeState(TurretStateType.Inactive);
        SoundManager.PlaySound(SoundDataIDStatic.TURRET_RETRIEVE);
    }

    private Module GetInactiveTurret()
    {
        int count = m_turrets.Count;
        for (int i = 0; i < count; i++)
        {
            if ((TurretStateType)m_turrets[i].currentState.type == TurretStateType.Inactive)
                return m_turrets[i];
        }
        return null;
    }

    #endregion

    #region ModuleTarget

    private void CallbackMousePosition(InputAction.CallbackContext context)
    {
        Vector3 mouseScreenPosition = context.ReadValue<Vector2>();
        Vector3 raycastOrigin = ScreenPositionToRaycastOrigin(mouseScreenPosition);

        if (Physics2D.Raycast(raycastOrigin, Vector3.forward, m_targetFilter, potentialTargets) > 0)
        {
            ATarget target = FindFirst(potentialTargets, TargetType.Turret);
            if (target != null)
            {
                ManageModuleTarget(target);
                return;
            }

            target = FindFirst(potentialTargets, TargetType.CrystalShard);
            if (target != null)
            {
                ManageCrystalTarget(target);
                return;
            }
        }
        else
        {
            if (m_currentTarget != null)
            {
                switch (m_currentTarget.type)
                {
                    case TargetType.CrystalShard:
                        UnsetCrystalTarget();
                        break;
                    case TargetType.Turret:
                        UnsetModuleTarget();
                        break;
                }
            }
        }
    }

    private void ManageCrystalTarget(ATarget target)
    {
        m_moduleTargeter.Target(target);
        m_currentTarget = target;

        if (m_remainingModuleCount > 0)
        {
            Module module = GetInactiveTurret();
            if (module != null)
                module.isDroppable = true;
        }
    }

    private void ManageModuleTarget(ATarget target)
    {
        m_moduleTargeter.Target(target);
        m_currentTarget = target;

        ((ModuleTarget)m_currentTarget).turret.isTargeted = true;
    }

    private void UnsetCrystalTarget()
    {
        m_moduleTargeter.Hide();
        m_currentTarget = null;

        if (m_remainingModuleCount > 0)
        {
            Module module = GetInactiveTurret();
            if (module != null)
                module.isDroppable = false;
        }
    }

    private void UnsetModuleTarget()
    {
        ((ModuleTarget)m_currentTarget).turret.isTargeted = false;

        m_moduleTargeter.Hide();
        m_currentTarget = null;
    }

    public ATarget FindFirst(List<RaycastHit2D> results, TargetType type)
    {
        foreach (RaycastHit2D hit in results)
        {
            if (hit.collider.TryGetComponent<ATarget>(out ATarget target))
            {
                if (target.type == type)
                    return target;
            }
        }
        return null;
    }

    public Vector3 ScreenPositionToRaycastOrigin(Vector2 screenPosition)
    {
        Vector3 raycastOrigin = m_camera.ScreenToWorldPoint(screenPosition);
        raycastOrigin.z = m_camera.transform.position.z;
        return raycastOrigin;
    }

    #endregion

    #region Reset

    public void CallbackReset()
    {
        UtilsClass.EmptyTransform(m_moduleContainer);
        m_turrets.Clear();

        // Turrets View
        m_moduleViews.Clear();
        m_moduleViewsContainer.Clear();

        // Turrets
        m_remainingModuleCount = m_settings.startingModuleCount;
        CreateTurret();

    }

    #endregion

    #region Pause

    public void Pause()
    {
        isPaused = true;
        UnsubscribeInputs();

        foreach (Module turret in m_turrets)
            turret.Pause();
    }

    public void Resume()
    {
        isPaused = false;
        SubscribeInputs();

        foreach (Module turret in m_turrets)
            turret.Resume();
    }

    #endregion

    #endregion
}
