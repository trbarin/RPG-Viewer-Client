using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Networking;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace RPG
{
    public class TokenUI : MonoBehaviour
    {
        [Header("Canvas")]
        [SerializeField] private Canvas canvas;
        [SerializeField] private Canvas uICanvas;
        [SerializeField] private CanvasGroup canvasGroup;

        [Header("UI")]
        [SerializeField] private Image image;
        [SerializeField] private Image outlineImage;
        [SerializeField] private TMP_Text label;
        [SerializeField] private GameObject rotateButton;
        [SerializeField] private CanvasGroup buttonsGroup;
        [SerializeField] private GameObject lockButton;
        [SerializeField] private GameObject visibilityButton;


        [Space]
        [SerializeField] private Image lockedImage;
        [SerializeField] private Sprite lockedSprite;
        [SerializeField] private Sprite unlockedSprite;

        [Space]
        [SerializeField] private TMP_InputField elevationInput;
        [SerializeField] private GameObject elevationPanel;

        [Space]
        [SerializeField] private TMP_InputField healthInput;
        [SerializeField] private GameObject healthPanel;

        [Space]
        [SerializeField] private TokenConfiguration configPrefab;

        public RectTransform Rect { get { return canvas.GetComponent<RectTransform>(); } }
        public bool MouseOver { get { return RectTransformUtility.RectangleContainsScreenPoint(Rect, Camera.main.ScreenToWorldPoint(Input.mousePosition)); } }
        public bool Editing { get { return false /* TODO: */; } }

        private Token token;
        private Vector2 screenPos;
        private Outline outline;
        private List<Token> rotatedTokens = new List<Token>();
        private TokenConfiguration config;

        private Shader outlineShader;
        private Shader regularShader;

        private void OnEnable()
        {
            // Get reference of main class and outline
            if (token == null) token = GetComponent<Token>();
            if (outline == null) outline = outlineImage.GetComponent<Outline>();

            outlineShader = Shader.Find("GUI/Text Shader");

            // Add event listeners
            Events.OnToolChanged.AddListener(HandleRaycast);
            Events.OnTokenSelected.AddListener(HandleSelection);
        }
        private void OnDisable()
        {
            // Remove event listeners
            Events.OnToolChanged.RemoveListener(HandleRaycast);
            Events.OnTokenSelected.RemoveListener(HandleSelection);
        }
        private void Awake()
        {
            lockButton.SetActive(ConnectionManager.Info.isMaster);
            visibilityButton.SetActive(ConnectionManager.Info.isMaster);
        }
        private void Update()
        {
            // Return if token is not selected
            if (!token.Selected) return;

            if (Input.GetMouseButtonDown(0) && !MouseOver) Events.OnTokenSelected?.Invoke(null);

            HandleElevation();
        }
        private void HandleSelection(Token _token)
        {
            outline.enabled = _token == token;
            buttonsGroup.alpha = outline.enabled ? 1.0f : 0.0f;
            buttonsGroup.blocksRaycasts = outline.enabled ? true : false;
            UpdateSorting();
        }

        private void HandleRaycast(Tool tool)
        {
            // Return if we aren't the owner
            if (!token.IsOwner)
            {
                image.raycastTarget = false;
                return;
            }

            // Enable / disable raycasting
            image.raycastTarget = tool == Tool.Move;
        }
        private void HandleElevation()
        {
            if (Input.GetKeyDown(KeyCode.PageUp) || Input.GetKeyDown(KeyCode.PageDown))
            {
                // Trim out the 'ft' part of the elevation and parse it to integer
                string elevation = elevationInput.text.TrimEnd('f', 't', ' ');
                int elevationValue = string.IsNullOrEmpty(elevation) ? 0 : int.Parse(elevation);

                // Modify elevation value
                elevationValue += Input.GetKey(KeyCode.PageUp) ? 5 : -5;
                string newElevation = $"{elevationValue} ft";
            }
        }

        public void SetAlpha(float value)
        {
            // Update group alpha
            canvasGroup.alpha = value;
        }
        public void DisableOutline()
        {
            outlineImage.gameObject.SetActive(false);
        }
        public void SetRotation(float value)
        {
            image.transform.eulerAngles = new Vector3(0.0f, 0.0f, value);
        }
        public void SetImage(Sprite sprite)
        {
            // Update image sprite and color
            image.sprite = sprite;
            outlineImage.sprite = sprite;
            outlineImage.material.shader = outlineShader;
            image.color = Color.white;
        }
        public void Reload()
        {
            // Reload all UI elements
            SetRotation(token.Data.rotation);
            UpdateSorting();
            Resize();
            EnableRaycasting(token.Enabled);

            LoadUI();
        }
        private void LoadUI()
        {
            label.text = token.Data.name;
            label.transform.parent.gameObject.SetActive(!string.IsNullOrEmpty(label.text));
        }
        public void EnableRaycasting(bool enable)
        {
            // Enable / disable raycasting
            image.raycastTarget = enable;
        }

        public void OnPointerClick(BaseEventData eventData)
        {
            // Get pointer data
            PointerEventData pointerData = eventData as PointerEventData;
            if (pointerData.button == PointerEventData.InputButton.Left) Events.OnTokenSelected?.Invoke(token);
            else if (pointerData.button == PointerEventData.InputButton.Right) ModifyToken();
        }
        public void OnPointerEnter(BaseEventData eventData)
        {
            outline.enabled = true;
        }
        public void OnPointerExit(BaseEventData eventData)
        {
            if (!token.Selected) outline.enabled = false;
        }

        private void ModifyToken()
        {
            if (config != null) return;

            config = Instantiate(configPrefab);
            config.transform.SetParent(UICanvas.Instance.transform);
            config.transform.localPosition = Vector3.zero;
            config.transform.SetAsLastSibling();
            config.LoadData(token.Data, token.Lighting, image.sprite.texture.GetRawTextureData(), "Modify Token", (data, image, lighting) =>
            {
                bool imageChanged = !image.SequenceEqual(this.image.sprite.texture.GetRawTextureData());
                SocketManager.EmitAsync("modify-token", async (callback) =>
                {
                    await UniTask.SwitchToMainThread();
                    if (callback.GetValue().GetBoolean()) return;

                    // Send error message
                    MessageManager.QueueMessage(callback.GetValue(1).GetString());
                }, token.Id, JsonUtility.ToJson(data), JsonUtility.ToJson(lighting), imageChanged ? Convert.ToBase64String(image) : null);

            });
        }

        public void BeginRotate(BaseEventData eventData)
        {
            // Clear previous nearby tokens
            rotatedTokens.Clear();

            // Return if we aren't dragging whit LMB
            PointerEventData pointerData = eventData as PointerEventData;
            if (pointerData.button != PointerEventData.InputButton.Left) return;

            // Store our position in screen space
            screenPos = Camera.main.WorldToScreenPoint(transform.position);
            Vector2 mouseDistance = (Vector2)Input.mousePosition - screenPos;

            // Calculate angle offset
            float offset = (Mathf.Atan2(transform.right.y, transform.right.x) - Mathf.Atan2(mouseDistance.y, mouseDistance.x)) * Mathf.Rad2Deg;

            // Return if this is not a mount
            if (token.Data.type != TokenType.Mount) return;

            // Check for nearby tokens
            rotatedTokens = token.GetNearbyTokens();

            for (int i = 0; i < rotatedTokens.Count; i++)
            {
                // Initialise rotation
                rotatedTokens[i].transform.parent = image.transform;
            }
        }
        public void Rotate(BaseEventData eventData)
        {
            // Calculate new angle
            Vector2 mouseDistance = (Vector2)Input.mousePosition - screenPos;
            float angle = Mathf.Atan2(mouseDistance.y, mouseDistance.x) * Mathf.Rad2Deg;

            // Rotate token
            image.transform.eulerAngles = new Vector3(0, 0, angle);
        }
        public void EndRotate(BaseEventData eventData)
        {
            for (int i = 0; i < rotatedTokens.Count; i++)
            {
                // Check if token is still there
                if (rotatedTokens[i] == null) continue;

                // Update nearby tokens position
                token.transform.parent = transform.parent;
                token.Movement.MountedRotation();
            }

            // Update our rotation
            token.Movement.FinishRotation(image.transform.eulerAngles.z, false);
        }

        public void UpdateSorting()
        {
            // Move token to correct sorting layer
            switch (token.Data.type)
            {
                case TokenType.Character:
                    canvas.sortingLayerName = "Characters";
                    // Check if we are dead
                    if (token.Conditions.IsDead) canvas.sortingLayerName = "Dead Characters";

                    // Check if we are owners
                    if (token.IsOwner) canvas.sortingLayerName = "Owned Characters";
                    break;
                case TokenType.Mount:
                    canvas.sortingLayerName = "Mounts";

                    // Check if we are dead
                    if (token.Conditions.IsDead) canvas.sortingLayerName = "Dead Mounts";

                    // Check if we are owners
                    if (token.IsOwner) canvas.sortingLayerName = "Owned Mounts";
                    break;
                case TokenType.Item:
                    canvas.sortingLayerName = "Items";

                    // Check if we are owners
                    if (token.IsOwner) canvas.sortingLayerName = "Owned Items";
                    break;
            }
        }
        public void Resize()
        {
            // Calculate correct canvas scale
            float cellSize = 1;
            Vector2Int dimensions = token.Data.dimensions;
            float targetSize = dimensions.x >= dimensions.y ? cellSize * (dimensions.y / 5.0f) : cellSize * (dimensions.x / 5.0f);

            // Apply scaling
            Rect.sizeDelta = new Vector2(100 * cellSize * (dimensions.x / 5.0f), 100 * cellSize * (dimensions.y / 5.0f));
            uICanvas.transform.localScale = new Vector3(targetSize, targetSize, 1.0f);
        }
    }
}