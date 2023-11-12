using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace RPG
{
    public class SettingsHandler : MonoBehaviour
    {
        [SerializeField] private CanvasGroup canvasGroup;

        [Header("Buttons")]
        [SerializeField] private ToolButton gridButton;
        [SerializeField] private ToolButton wallsButton;
        [SerializeField] private ToolButton regularButton;
        [SerializeField] private ToolButton invisibleButton;
        [SerializeField] private ToolButton doorsButton;
        [SerializeField] private ToolButton hiddenButton;
        [SerializeField] private ToolButton fogButton;
        [SerializeField] private ToolButton lightingButton;
        [SerializeField] private ToolButton createButton;
        [SerializeField] private ToolButton deleteButton;
        [SerializeField] private ToolButton viewButton;
        [SerializeField] private ToolButton playerButton;
        [SerializeField] private ToolButton visionButton;
        [SerializeField] private ToolButton clearButton;

        [Header("Masks")]
        [SerializeField] private RectMask2D gridMask;
        [SerializeField] private RectMask2D wallsMask;
        [SerializeField] private RectMask2D lightingMask;
        [SerializeField] private RectMask2D viewMask;

        [Header("Configuration")]
        [SerializeField] private GridConfiguration gridConfiguration;
        [SerializeField] private DarknessConfiguration lightConfiguration;

        public static SettingsHandler Instance { get; private set; }
        public Setting Setting { get { return activeSetting; } }

        public GameView LastView = GameView.Clear;

        private Setting activeSetting;
        private Setting lastSetting;
        private Setting lastWalls = Setting.Walls_Regular;
        private Setting lastLighting = Setting.Lighting_Create;

        private void Awake()
        {
            // Create instance
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }
        private void OnEnable()
        {
            // Add event listeners
            // Events.OnStateChanged.AddListener(HandleStateChange);
            Events.Test.AddListener(Test);
            Events.OnToolChanged.AddListener(HandleToolChanged);
        }

        private async void Test(SessionState oldState, SessionState newState)
        {
            await UniTask.SwitchToMainThread();

            if (!ConnectionManager.Info.isMaster) return;

            canvasGroup.alpha = string.IsNullOrEmpty(newState.scene) ? 0.0f : 1.0f;
            canvasGroup.blocksRaycasts = !string.IsNullOrEmpty(newState.scene);
        }

        private void OnDisable()
        {
            // Remove event listeners
            Events.OnStateChanged.RemoveListener(HandleStateChange);
            Events.OnToolChanged.RemoveListener(HandleToolChanged);
        }

        private void Update()
        {
            // Send tool change event whenever the user changes the tool
            if (activeSetting != lastSetting)
            {
                lastSetting = activeSetting;
                Events.OnSettingChanged?.Invoke(activeSetting);
            }
        }

        private void HandleStateChange(SessionState oldState, SessionState newState)
        {
            Debug.Log("R");
        }
        private void HandleToolChanged(Tool tool)
        {
            if (tool == Tool.Move) return;
            CloseWalls();
            CloseLighting();
            CloseView();
            CloseGrid();
            activeSetting = Setting.None;
        }

        public void ConfigureGrid()
        {
            if (gridConfiguration.gameObject.activeInHierarchy) return;

            gridConfiguration.transform.SetAsLastSibling();
            gridConfiguration.OpenPanel();
        }
        public void SelectRegular()
        {
            // Update selections
            regularButton.Select();
            invisibleButton.Deselect();
            doorsButton.Deselect();
            hiddenButton.Deselect();
            fogButton.Deselect();

            // Update tool states
            activeSetting = Setting.Walls_Regular;
            lastWalls = activeSetting;
        }
        public void SelectInvisible()
        {
            // Update selections
            invisibleButton.Select();
            regularButton.Deselect();
            doorsButton.Deselect();
            hiddenButton.Deselect();
            fogButton.Deselect();

            // Update tool states
            activeSetting = Setting.Walls_Invisible;
            lastWalls = activeSetting;
        }
        public void SelectDoors()
        {
            // Update selections
            doorsButton.Select();
            regularButton.Deselect();
            invisibleButton.Deselect();
            hiddenButton.Deselect();
            fogButton.Deselect();

            // Update tool states
            activeSetting = Setting.Walls_Door;
            lastWalls = activeSetting;
        }
        public void SelectHidden()
        {
            // Update selections
            hiddenButton.Select();
            doorsButton.Deselect();
            regularButton.Deselect();
            invisibleButton.Deselect();
            fogButton.Deselect();

            // Update tool states
            activeSetting = Setting.Walls_Hidden_Door;
            lastWalls = activeSetting;
        }
        public void SelectFog()
        {
            // Update selections
            hiddenButton.Deselect();
            doorsButton.Deselect();
            regularButton.Deselect();
            invisibleButton.Deselect();
            fogButton.Select();

            // Update tool states
            activeSetting = Setting.Walls_Fog;
            lastWalls = activeSetting;
        }
        public void SelectPlayer()
        {
            // Update selections
            playerButton.Select();
            visionButton.Deselect();
            clearButton.Deselect();

            // Update tool states
            LastView = GameView.Player;
            activeSetting = Setting.Visibility;
            Events.OnViewChanged?.Invoke(GameView.Player);
        }
        public void SelectVision()
        {
            // Update selections
            visionButton.Select();
            playerButton.Deselect();
            clearButton.Deselect();

            // Update tool states
            LastView = GameView.Vision;
            Events.OnViewChanged?.Invoke(GameView.Vision);
        }
        public void SelectClear()
        {
            // Update selections
            clearButton.Select();
            playerButton.Deselect();
            visionButton.Deselect();

            // Update tool states
            LastView = GameView.Clear;
            Events.OnViewChanged?.Invoke(GameView.Clear);
        }
        public void ConfigureLighting()
        {
            if (lightConfiguration.gameObject.activeInHierarchy) return;

            lightConfiguration.transform.SetAsLastSibling();
            lightConfiguration.OpenPanel(Session.Instance.Settings.darkness);
        }
        public void SelectCreate()
        {
            // Update selections
            createButton.Select();
            deleteButton.Deselect();

            // Update tool states
            activeSetting = Setting.Lighting_Create;
            lastLighting = activeSetting;
        }
        public void SelectDelete()
        {
            // Update selections
            deleteButton.Select();
            createButton.Deselect();

            // Update tool states
            activeSetting = Setting.Lighting_Delete;
            lastLighting = activeSetting;
        }

        public void OpenGrid()
        {
            // Update selections
            CloseWalls();
            CloseLighting();
            CloseView();

            // Close panel if it's open
            if (!gridMask.enabled)
            {
                CloseGrid();
                activeSetting = Setting.None;
                return;
            }

            gridMask.enabled = false;
            gridButton.Select();

            // Update tool state
            activeSetting = Setting.Grid;
        }
        public void CloseGrid()
        {
            gridMask.enabled = true;
            gridButton.Deselect();
        }
        public void OpenWalls()
        {
            // Update selections
            CloseGrid();
            CloseLighting();
            CloseView();

            // Close panel if it's open
            if (!wallsMask.enabled)
            {
                CloseWalls();
                activeSetting = Setting.None;
                return;
            }

            // Update rect size
            wallsMask.enabled = false;
            wallsButton.Select();


            // Activate last tool selection
            if (lastWalls == Setting.Walls_Regular) SelectRegular();
            else if (lastWalls == Setting.Walls_Invisible) SelectInvisible();
            else SelectDoors();
        }
        public void CloseWalls()
        {
            wallsMask.enabled = true;
            wallsButton.Deselect();
        }
        public void OpenLighting()
        {
            // Update selections
            CloseGrid();
            CloseWalls();
            CloseView();

            // Close panel if it's open
            if (!lightingMask.enabled)
            {
                CloseLighting();
                activeSetting = Setting.None;
                return;
            }

            // Update rect size
            lightingMask.enabled = false;
            lightingButton.Select();

            // Activate last tool selection
            if (lastLighting == Setting.Lighting_Create) SelectCreate();
            else SelectDelete();
        }
        public void CloseLighting()
        {
            lightingMask.enabled = true;
            lightingButton.Deselect();
        }
        public void OpenView()
        {
            // Update selections
            CloseGrid();
            CloseLighting();
            CloseWalls();

            // Close panel if it's open
            if (!viewMask.enabled)
            {
                CloseView();
                activeSetting = Setting.None;
                return;
            }

            // Update rect size
            viewMask.enabled = false;
            viewButton.Select();


            // Activate last tool selection
            if (LastView == GameView.Player) SelectPlayer();
            else if (LastView == GameView.Vision) SelectVision();
            else SelectClear();
        }
        public void CloseView()
        {
            viewMask.enabled = true;
            viewButton.Deselect();
        }
    }

    public enum Setting
    {
        None,
        Grid,
        Walls_Regular,
        Walls_Invisible,
        Walls_Door,
        Walls_Hidden_Door,
        Walls_Fog,
        Lighting_Create,
        Lighting_Delete,
        Visibility
    }
}