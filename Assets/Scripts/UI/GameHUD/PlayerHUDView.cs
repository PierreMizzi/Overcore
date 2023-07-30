using UnityEngine;
using UnityEngine.UIElements;

public class PlayerHUDView : MonoBehaviour
{
	#region Fields

    [SerializeField]
    private UIDocument m_document = null;

    private const string k_progressBar = "health-progress-bar__progress";
    private float m_containerPixelSize;

    private VisualElement m_progressBar = null;

    private Length m_progressBarLength;

    [SerializeField]
    private LevelChannel m_levelChannel = null;

    private HealthEntity m_playerHealth = null;

    [Range(0f, 1f)]
    [SerializeField]
    private float d_normalizedHealth = 0f;

	#endregion

	#region Methods

    private void Awake()
    {
        m_playerHealth = m_levelChannel.player.GetComponent<HealthEntity>();
    }

    private void Start()
    {
        m_playerHealth.onHealedHealth += CallbackHealthChanged;
        m_playerHealth.onLostHealth += CallbackHealthChanged;

        InitializeVisualElements();
    }

    // private void OnValidate()
    // {
    //     UpdateProgessBar(d_normalizedHealth);
    // }

    private void OnDestroy()
    {
        m_playerHealth.onHealedHealth -= CallbackHealthChanged;
        m_playerHealth.onLostHealth -= CallbackHealthChanged;
    }

    private void InitializeVisualElements()
    {
        m_progressBar = m_document.rootVisualElement.Q(k_progressBar);
        m_progressBarLength = new Length(0, LengthUnit.Percent);
    }

    public void CallbackHealthChanged()
    {
        UpdateProgessBar(m_playerHealth.normalizedHealth);
    }

    public void UpdateProgessBar(float normalized)
    {
        m_progressBarLength.value = (1f - normalized) * 100;
        m_progressBar.style.right = m_progressBarLength;
    }

	#endregion
}