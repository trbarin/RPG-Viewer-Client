﻿using Cysharp.Threading.Tasks;
using Networking;
using UnityEngine;
using UnityEngine.UI;

namespace RPG
{
    public class Session : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer sceneSprite;
        [SerializeField] private Image landingPage;

        public LightingManager LightingManager { get; private set; }
        public TokenManager TokenManager { get; private set; }
        public GridManager Grid { get; private set; }
        public SceneData Settings { get; private set; }
        public static Session Instance { get; private set; }

        private void OnEnable()
        {
            // Add event listeners
            Events.OnStateChanged.AddListener(ChangeState);
            Events.OnLandingPageChanged.AddListener(UpdateLandingPage);
        }
        private void OnDisable()
        {
            // Remove event listeners
            Events.OnStateChanged.RemoveListener(ChangeState);
            Events.OnLandingPageChanged.RemoveListener(UpdateLandingPage);
        }
        private void Awake()
        {
            // Create instance
            if (Instance == null) Instance = this;
            else Destroy(gameObject);

            // Get reference of the managers
            LightingManager = FindObjectOfType<LightingManager>();
            TokenManager = FindObjectOfType<TokenManager>();
            Grid = FindObjectOfType<GridManager>();
        }
        private void Start()
        {
            // Update landing page
            if (ConnectionManager.Info.background != null) landingPage.sprite = ConnectionManager.Info.background;
        }

        private void ChangeState(SessionState oldState, SessionState newState)
        {
            if (newState.scene == null)
            {
                // Return if there's no scene active
                landingPage.gameObject.SetActive(true);
                return;
            }

            // Check if we are the master client
            if (ConnectionManager.Info.isMaster)
            {
                // Activate or deactivate landing page based on state
                landingPage.gameObject.SetActive(string.IsNullOrEmpty(newState.scene));

                // Return if no scene was changed
                if (oldState.scene == newState.scene) return;

                // Load new scene
                if (!string.IsNullOrEmpty(newState.scene)) LoadScene(newState.scene);
            }
            else
            {
                // Return if syncing is disabled
                if (!newState.synced)
                {
                    landingPage.gameObject.SetActive(true);
                    return;
                }

                // Load new scene
                if (!string.IsNullOrEmpty(newState.scene)) LoadScene(newState.scene);
            }
        }
        private void UpdateLandingPage(string id)
        {
            MessageManager.QueueMessage("Loading new landing page");

            WebManager.Download(id, true, async (bytes) =>
            {
                // Check if landing page exists
                if (bytes == null)
                {
                    MessageManager.QueueMessage("Failed to load landing page, please try again");
                    return;
                }

                await UniTask.SwitchToMainThread();

                // Create landing page texture
                Texture2D texture = await AsyncImageLoader.CreateFromImageAsync(bytes);
                Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));

                ConnectionManager.Info.background = sprite;
                landingPage.sprite = sprite;
                MessageManager.RemoveMessage("Loading new landing page");
            });
        }

        private void LoadScene(string id)
        {
            landingPage.gameObject.SetActive(true);
            SocketManager.EmitAsync("get-scene", async (callback) =>
            {
                // Check if the event was successful
                if (callback.GetValue().GetBoolean())
                {
                    await UniTask.SwitchToMainThread();

                    // Load scene data
                    SceneData settings = JsonUtility.FromJson<SceneData>(callback.GetValue(1).ToString());
                    settings.id = id;

                    MessageManager.QueueMessage("Loading scene");
                    Settings = settings;

                    // Send load event
                    WebManager.Download(settings.info.image, true, async (bytes) =>
                    {
                        await UniTask.SwitchToMainThread();

                        // Generate and apply texture
                        Texture2D texture = await AsyncImageLoader.CreateFromImageAsync(bytes);
                        sceneSprite.sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));

                        Events.OnSceneLoaded?.Invoke(settings);

                        // Remove message when loading is completed
                        MessageManager.RemoveMessage("Loading scene");

                        landingPage.gameObject.SetActive(false);
                    });

                    return;
                }

                // Send error message
                MessageManager.QueueMessage(callback.GetValue(1).GetString());
            }, id);
        }
    }
}