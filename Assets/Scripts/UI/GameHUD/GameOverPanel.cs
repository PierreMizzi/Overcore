using Bitfrost.Application;
using PierreMizzi.SoundManager;
using PierreMizzi.Useful;
using PierreMizzi.Useful.UI;
using UnityEngine;
using UnityEngine.UIElements;

namespace Bitfrost.Gameplay.UI
{

	public class GameOverPanel : APanel
	{

		#region Fields

		[SerializeField]
		private ApplicationChannel m_applicationChannel = null;

		[SerializeField]
		private LevelChannel m_levelChannel;

		private Label m_timeLabel;
		private Label m_scoreLabel;

		private Button m_restartButton;
		private Button m_menuButton;

		private const string k_timeLabel = "time-label";
		private const string k_scoreLabel = "score-label";

		private const string k_restartButton = "restart-button";
		private const string k_menuBUtton = "menu-button";

		#endregion

		#region Methods

		protected override void Awake()
		{
			base.Awake();

			m_timeLabel = m_root.Q<Label>(k_timeLabel);
			m_scoreLabel = m_root.Q<Label>(k_scoreLabel);
			m_restartButton = m_root.Q<Button>(k_restartButton);
			m_menuButton = m_root.Q<Button>(k_menuBUtton);

			m_restartButton.clicked += CallbackRestartButton;
			m_menuButton.clicked += CallbackMenuButton;

			m_restartButton.RegisterCallback<MouseOverEvent>(CallbackOnMouseOver);
			m_menuButton.RegisterCallback<MouseOverEvent>(CallbackOnMouseOver);
		}

		private void Start()
		{
			if (m_levelChannel != null)
				m_levelChannel.onGameOverPanel += CallbackGameOverPanel;
		}

		private void OnDestroy()
		{
			if (m_levelChannel != null)
				m_levelChannel.onGameOverPanel -= CallbackGameOverPanel;
		}

		private void CallbackOnMouseOver(MouseOverEvent evt)
		{
			SoundManager.PlaySFX(SoundDataID.U_I_HOVER);
		}


		private void CallbackRestartButton()
		{
			m_levelChannel.onRestart?.Invoke();
			Hide();
			SoundManager.PlaySFX(SoundDataID.U_I_CLICK);
		}

		private void CallbackMenuButton()
		{
			m_applicationChannel.onGameToTitlecard?.Invoke();
			SoundManager.PlaySFX(SoundDataID.U_I_CLICK);
		}

		private void CallbackGameOverPanel(GameOverData data)
		{
			m_timeLabel.text = UtilsClass.SecondsToTextTime(data.totalTime);
			m_scoreLabel.text = data.killCount.ToString();

			Display();
		}

		protected override void Display()
		{
			m_root.style.display = DisplayStyle.Flex;
		}

		protected override void Hide()
		{
			m_root.style.display = DisplayStyle.None;
		}



		#endregion

	}
}