using System;
using System.Collections.Generic;
using DedicatedServerMod.Client.Managers;
using MelonLoader;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DedicatedServerMod.Client.UI
{
    /// <summary>
    /// Simplified "Connect to Server" dialog.
    /// Allows direct connection via IP/port and displays a list of recent servers.
    /// </summary>
    public class ConnectDialog
    {
        private readonly MelonLogger.Instance logger;
        private readonly ClientConnectionManager connectionManager;
        private readonly RecentServersStore recentServersStore;

        // UI Elements
        private GameObject dialogOverlay;
        private GameObject dialogPanel;
        private CanvasGroup dialogCanvasGroup;
        private TMP_InputField ipInput;
        private TMP_InputField portInput;
        private Button connectButton;
        private Button closeButton;
        private TMP_Text statusText;
        
        // Recent servers UI
        private Transform recentServersContainer;
        private readonly List<GameObject> recentServerButtons = new List<GameObject>();

        // Theme colors
        private static readonly Color ACCENT = new Color(0.10f, 0.65f, 1f, 1f);
        private static readonly Color PANEL_BG = new Color(0.08f, 0.09f, 0.12f, 0.96f);
        private static readonly Color OVERLAY_DIM = new Color(0f, 0f, 0f, 0.45f);
        private static readonly Color INPUT_BG = new Color(0.12f, 0.13f, 0.16f, 1f);
        private static readonly Color BTN_BG = new Color(0.18f, 0.20f, 0.25f, 0.95f);
        private static readonly Color BTN_BG_HOVER = new Color(0.22f, 0.24f, 0.30f, 0.95f);
        private static readonly Color BTN_BG_PRESSED = new Color(0.14f, 0.16f, 0.20f, 0.95f);

        /// <summary>
        /// Initializes a new instance of the ConnectDialog class.
        /// </summary>
        /// <param name="logger">Logger instance.</param>
        /// <param name="connectionManager">Connection manager instance.</param>
        /// <param name="recentServersStore">Recent servers store instance.</param>
        public ConnectDialog(MelonLogger.Instance logger, ClientConnectionManager connectionManager, RecentServersStore recentServersStore)
        {
            this.logger = logger;
            this.connectionManager = connectionManager;
            this.recentServersStore = recentServersStore;
        }

        /// <summary>
        /// Creates the dialog UI.
        /// </summary>
        /// <param name="parent">Parent transform to attach the dialog to.</param>
        public void Create(Transform parent)
        {
            try
            {
                // Create overlay
                dialogOverlay = new GameObject("ConnectDialogOverlay", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                dialogOverlay.transform.SetParent(parent, false);
                var overlayRect = dialogOverlay.GetComponent<RectTransform>();
                overlayRect.anchorMin = new Vector2(0f, 0f);
                overlayRect.anchorMax = new Vector2(1f, 1f);
                overlayRect.pivot = new Vector2(0.5f, 0.5f);
                overlayRect.sizeDelta = Vector2.zero;
                var overlayImage = dialogOverlay.GetComponent<Image>();
                overlayImage.color = OVERLAY_DIM;

                // Create dialog panel
                dialogPanel = new GameObject("ConnectDialogPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(CanvasGroup));
                dialogPanel.transform.SetParent(dialogOverlay.transform, false);

                dialogCanvasGroup = dialogPanel.GetComponent<CanvasGroup>();
                dialogCanvasGroup.alpha = 1f;

                var panelRect = dialogPanel.GetComponent<RectTransform>();
                panelRect.anchorMin = new Vector2(0.5f, 0.5f);
                panelRect.anchorMax = new Vector2(0.5f, 0.5f);
                panelRect.pivot = new Vector2(0.5f, 0.5f);
                panelRect.sizeDelta = new Vector2(500f, 450f);
                panelRect.anchoredPosition = new Vector2(0f, 0f);

                var panelImage = dialogPanel.GetComponent<Image>();
                panelImage.color = PANEL_BG;
                panelImage.raycastTarget = true;

                // Create dialog content
                CreateTitle();
                CreateIPInput();
                CreatePortInput();
                CreateConnectButton();
                CreateRecentServersList();
                CreateStatusText();
                CreateCloseButton();

                // Hide initially
                dialogOverlay.SetActive(false);

                logger.Msg("Connect dialog created successfully");
            }
            catch (Exception ex)
            {
                logger.Error($"Error creating connect dialog: {ex}");
            }
        }

        /// <summary>
        /// Shows or hides the dialog.
        /// </summary>
        /// <param name="show">True to show, false to hide.</param>
        public void Show(bool show)
        {
            try
            {
                if (dialogOverlay != null)
                {
                    dialogOverlay.SetActive(show);
                    
                    if (show)
                    {
                        PrefillFields();
                        RefreshRecentServers();
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error($"Error showing/hiding connect dialog: {ex}");
            }
        }

        /// <summary>
        /// Cleans up the dialog UI.
        /// </summary>
        public void Cleanup()
        {
            try
            {
                if (dialogOverlay != null)
                {
                    GameObject.Destroy(dialogOverlay);
                    dialogOverlay = null;
                }
                
                recentServerButtons.Clear();
                logger.Msg("Connect dialog cleaned up");
            }
            catch (Exception ex)
            {
                logger.Error($"Error cleaning up connect dialog: {ex}");
            }
        }

        private void CreateTitle()
        {
            var titleGO = new GameObject("Title", typeof(RectTransform));
            titleGO.transform.SetParent(dialogPanel.transform, false);
            var titleRect = titleGO.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.5f, 1f);
            titleRect.anchorMax = new Vector2(0.5f, 1f);
            titleRect.pivot = new Vector2(0.5f, 1f);
            titleRect.sizeDelta = new Vector2(440f, 40f);
            titleRect.anchoredPosition = new Vector2(0f, -20f);
            var titleText = titleGO.AddComponent<TextMeshProUGUI>();
            titleText.text = "Connect to Server";
            titleText.alignment = TextAlignmentOptions.Center;
            titleText.fontSize = 28;
            titleText.color = Color.white;
            titleText.fontStyle = FontStyles.Bold;
        }

        private void CreateIPInput()
        {
            // Label
            var labelGO = new GameObject("IPLabel", typeof(RectTransform));
            labelGO.transform.SetParent(dialogPanel.transform, false);
            var labelRect = labelGO.GetComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0f, 1f);
            labelRect.anchorMax = new Vector2(0f, 1f);
            labelRect.pivot = new Vector2(0f, 1f);
            labelRect.sizeDelta = new Vector2(120f, 24f);
            labelRect.anchoredPosition = new Vector2(30f, -80f);
            var labelText = labelGO.AddComponent<TextMeshProUGUI>();
            labelText.text = "IP Address";
            labelText.alignment = TextAlignmentOptions.Left;
            labelText.fontSize = 18;
            labelText.color = new Color(1f, 1f, 1f, 0.9f);

            // Input background
            var inputBG = new GameObject("IPInputBG", typeof(RectTransform), typeof(Image));
            inputBG.transform.SetParent(dialogPanel.transform, false);
            var inputBGRect = inputBG.GetComponent<RectTransform>();
            inputBGRect.anchorMin = new Vector2(0f, 1f);
            inputBGRect.anchorMax = new Vector2(0f, 1f);
            inputBGRect.pivot = new Vector2(0f, 1f);
            inputBGRect.sizeDelta = new Vector2(300f, 36f);
            inputBGRect.anchoredPosition = new Vector2(30f, -110f);
            var inputBGImage = inputBG.GetComponent<Image>();
            inputBGImage.color = INPUT_BG;

            // Border accent
            var border = new GameObject("IPBorder", typeof(RectTransform), typeof(Image));
            border.transform.SetParent(inputBG.transform, false);
            var borderRect = border.GetComponent<RectTransform>();
            borderRect.anchorMin = new Vector2(0f, 0f);
            borderRect.anchorMax = new Vector2(1f, 0f);
            borderRect.pivot = new Vector2(0.5f, 0f);
            borderRect.sizeDelta = new Vector2(0f, 2f);
            borderRect.anchoredPosition = new Vector2(0f, 0f);
            var borderImage = border.GetComponent<Image>();
            borderImage.color = ACCENT;

            // Input field
            var inputGO = new GameObject("IPInput", typeof(RectTransform));
            inputGO.transform.SetParent(inputBG.transform, false);
            var inputRect = inputGO.GetComponent<RectTransform>();
            inputRect.anchorMin = new Vector2(0f, 0f);
            inputRect.anchorMax = new Vector2(1f, 1f);
            inputRect.pivot = new Vector2(0.5f, 0.5f);
            inputRect.sizeDelta = new Vector2(-12f, -12f);
            inputRect.anchoredPosition = new Vector2(0f, 0f);
            ipInput = inputGO.AddComponent<TMP_InputField>();
            var inputText = inputGO.AddComponent<TextMeshProUGUI>();
            inputText.fontSize = 16;
            inputText.color = Color.white;
            inputText.enableWordWrapping = false;
            inputText.alignment = TextAlignmentOptions.MidlineLeft;
            ipInput.textComponent = inputText;

            // Placeholder
            var placeholder = new GameObject("Placeholder", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
            placeholder.transform.SetParent(inputGO.transform, false);
            var phRect = placeholder.GetComponent<RectTransform>();
            phRect.anchorMin = new Vector2(0f, 0f);
            phRect.anchorMax = new Vector2(1f, 1f);
            phRect.offsetMin = new Vector2(0f, 0f);
            phRect.offsetMax = new Vector2(0f, 0f);
            placeholder.text = "127.0.0.1";
            placeholder.fontSize = 16;
            placeholder.color = new Color(1f, 1f, 1f, 0.35f);
            placeholder.alignment = TextAlignmentOptions.MidlineLeft;
            ipInput.placeholder = placeholder;
        }

        private void CreatePortInput()
        {
            // Label
            var labelGO = new GameObject("PortLabel", typeof(RectTransform));
            labelGO.transform.SetParent(dialogPanel.transform, false);
            var labelRect = labelGO.GetComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0f, 1f);
            labelRect.anchorMax = new Vector2(0f, 1f);
            labelRect.pivot = new Vector2(0f, 1f);
            labelRect.sizeDelta = new Vector2(80f, 24f);
            labelRect.anchoredPosition = new Vector2(350f, -80f);
            var labelText = labelGO.AddComponent<TextMeshProUGUI>();
            labelText.text = "Port";
            labelText.alignment = TextAlignmentOptions.Left;
            labelText.fontSize = 18;
            labelText.color = new Color(1f, 1f, 1f, 0.9f);

            // Input background
            var inputBG = new GameObject("PortInputBG", typeof(RectTransform), typeof(Image));
            inputBG.transform.SetParent(dialogPanel.transform, false);
            var inputBGRect = inputBG.GetComponent<RectTransform>();
            inputBGRect.anchorMin = new Vector2(0f, 1f);
            inputBGRect.anchorMax = new Vector2(0f, 1f);
            inputBGRect.pivot = new Vector2(0f, 1f);
            inputBGRect.sizeDelta = new Vector2(110f, 36f);
            inputBGRect.anchoredPosition = new Vector2(350f, -110f);
            var inputBGImage = inputBG.GetComponent<Image>();
            inputBGImage.color = INPUT_BG;

            // Border accent
            var border = new GameObject("PortBorder", typeof(RectTransform), typeof(Image));
            border.transform.SetParent(inputBG.transform, false);
            var borderRect = border.GetComponent<RectTransform>();
            borderRect.anchorMin = new Vector2(0f, 0f);
            borderRect.anchorMax = new Vector2(1f, 0f);
            borderRect.pivot = new Vector2(0.5f, 0f);
            borderRect.sizeDelta = new Vector2(0f, 2f);
            borderRect.anchoredPosition = new Vector2(0f, 0f);
            var borderImage = border.GetComponent<Image>();
            borderImage.color = ACCENT;

            // Input field
            var inputGO = new GameObject("PortInput", typeof(RectTransform));
            inputGO.transform.SetParent(inputBG.transform, false);
            var inputRect = inputGO.GetComponent<RectTransform>();
            inputRect.anchorMin = new Vector2(0f, 0f);
            inputRect.anchorMax = new Vector2(1f, 1f);
            inputRect.pivot = new Vector2(0.5f, 0.5f);
            inputRect.sizeDelta = new Vector2(-12f, -12f);
            inputRect.anchoredPosition = new Vector2(0f, 0f);
            portInput = inputGO.AddComponent<TMP_InputField>();
            portInput.contentType = TMP_InputField.ContentType.IntegerNumber;
            var inputText = inputGO.AddComponent<TextMeshProUGUI>();
            inputText.fontSize = 16;
            inputText.color = Color.white;
            inputText.enableWordWrapping = false;
            inputText.alignment = TextAlignmentOptions.MidlineLeft;
            portInput.textComponent = inputText;

            // Placeholder
            var placeholder = new GameObject("Placeholder", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
            placeholder.transform.SetParent(inputGO.transform, false);
            var phRect = placeholder.GetComponent<RectTransform>();
            phRect.anchorMin = new Vector2(0f, 0f);
            phRect.anchorMax = new Vector2(1f, 1f);
            phRect.offsetMin = new Vector2(0f, 0f);
            phRect.offsetMax = new Vector2(0f, 0f);
            placeholder.text = "38465";
            placeholder.fontSize = 16;
            placeholder.color = new Color(1f, 1f, 1f, 0.35f);
            placeholder.alignment = TextAlignmentOptions.MidlineLeft;
            portInput.placeholder = placeholder;
        }

        private void CreateConnectButton()
        {
            var buttonGO = new GameObject("ConnectButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            buttonGO.transform.SetParent(dialogPanel.transform, false);
            var rect = buttonGO.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.sizeDelta = new Vector2(200f, 40f);
            rect.anchoredPosition = new Vector2(0f, -165f);

            var image = buttonGO.GetComponent<Image>();
            image.color = BTN_BG;

            var colors = new ColorBlock
            {
                colorMultiplier = 1f,
                disabledColor = new Color(1f, 1f, 1f, 0.3f),
                highlightedColor = BTN_BG_HOVER,
                normalColor = BTN_BG,
                pressedColor = BTN_BG_PRESSED,
                selectedColor = BTN_BG
            };
            connectButton = buttonGO.GetComponent<Button>();
            connectButton.colors = colors;
            connectButton.onClick.AddListener(OnConnectClicked);

            var textGO = new GameObject("Text", typeof(RectTransform));
            textGO.transform.SetParent(buttonGO.transform, false);
            var textRect = textGO.GetComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0f, 0f);
            textRect.anchorMax = new Vector2(1f, 1f);
            textRect.pivot = new Vector2(0.5f, 0.5f);
            textRect.sizeDelta = new Vector2(-10f, -10f);
            var text = textGO.AddComponent<TextMeshProUGUI>();
            text.text = "Connect";
            text.fontSize = 20;
            text.alignment = TextAlignmentOptions.Center;
            text.color = Color.white;
        }

        private void CreateRecentServersList()
        {
            // Section title
            var titleGO = new GameObject("RecentServersTitle", typeof(RectTransform));
            titleGO.transform.SetParent(dialogPanel.transform, false);
            var titleRect = titleGO.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0f, 1f);
            titleRect.anchorMax = new Vector2(0f, 1f);
            titleRect.pivot = new Vector2(0f, 1f);
            titleRect.sizeDelta = new Vector2(440f, 24f);
            titleRect.anchoredPosition = new Vector2(30f, -220f);
            var titleText = titleGO.AddComponent<TextMeshProUGUI>();
            titleText.text = "Recent Servers";
            titleText.alignment = TextAlignmentOptions.Left;
            titleText.fontSize = 18;
            titleText.color = new Color(1f, 1f, 1f, 0.9f);

            // Container for recent server buttons
            var containerGO = new GameObject("RecentServersContainer", typeof(RectTransform));
            containerGO.transform.SetParent(dialogPanel.transform, false);
            var containerRect = containerGO.GetComponent<RectTransform>();
            containerRect.anchorMin = new Vector2(0f, 1f);
            containerRect.anchorMax = new Vector2(0f, 1f);
            containerRect.pivot = new Vector2(0f, 1f);
            containerRect.sizeDelta = new Vector2(440f, 150f);
            containerRect.anchoredPosition = new Vector2(30f, -250f);
            recentServersContainer = containerGO.transform;
        }

        private void CreateStatusText()
        {
            var statusGO = new GameObject("StatusText", typeof(RectTransform));
            statusGO.transform.SetParent(dialogPanel.transform, false);
            var statusRect = statusGO.GetComponent<RectTransform>();
            statusRect.anchorMin = new Vector2(0f, 0f);
            statusRect.anchorMax = new Vector2(1f, 0f);
            statusRect.pivot = new Vector2(0.5f, 0f);
            statusRect.sizeDelta = new Vector2(-40f, 30f);
            statusRect.anchoredPosition = new Vector2(0f, 10f);
            statusText = statusGO.AddComponent<TextMeshProUGUI>();
            statusText.text = "";
            statusText.alignment = TextAlignmentOptions.Center;
            statusText.fontSize = 14;
            statusText.color = new Color(1f, 1f, 1f, 0.85f);
        }

        private void CreateCloseButton()
        {
            var buttonGO = new GameObject("CloseButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            buttonGO.transform.SetParent(dialogPanel.transform, false);
            var rect = buttonGO.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(1f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.sizeDelta = new Vector2(28f, 28f);
            rect.anchoredPosition = new Vector2(-20f, -20f);

            var image = buttonGO.GetComponent<Image>();
            image.color = BTN_BG;

            var colors = new ColorBlock
            {
                colorMultiplier = 1f,
                disabledColor = new Color(1f, 1f, 1f, 0.3f),
                highlightedColor = BTN_BG_HOVER,
                normalColor = BTN_BG,
                pressedColor = BTN_BG_PRESSED,
                selectedColor = BTN_BG
            };
            closeButton = buttonGO.GetComponent<Button>();
            closeButton.colors = colors;
            closeButton.onClick.AddListener(() => Show(false));

            var textGO = new GameObject("Text", typeof(RectTransform));
            textGO.transform.SetParent(buttonGO.transform, false);
            var textRect = textGO.GetComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0f, 0f);
            textRect.anchorMax = new Vector2(1f, 1f);
            textRect.pivot = new Vector2(0.5f, 0.5f);
            textRect.sizeDelta = Vector2.zero;
            var text = textGO.AddComponent<TextMeshProUGUI>();
            text.text = "X";
            text.fontSize = 16;
            text.alignment = TextAlignmentOptions.Center;
            text.color = new Color(1f, 1f, 1f, 0.85f);
        }

        private void PrefillFields()
        {
            try
            {
                var target = ClientConnectionManager.GetTargetServer();
                
                if (ipInput != null)
                {
                    ipInput.text = string.Empty;
                    ipInput.text = target.ip ?? "localhost";
                }
                
                if (portInput != null)
                {
                    portInput.text = string.Empty;
                    portInput.text = target.port.ToString();
                }
            }
            catch (Exception ex)
            {
                logger.Error($"Error pre-filling fields: {ex}");
            }
        }

        private void RefreshRecentServers()
        {
            try
            {
                // Clear existing buttons
                foreach (var btn in recentServerButtons)
                {
                    GameObject.Destroy(btn);
                }
                recentServerButtons.Clear();

                // Get recent servers
                var recentServers = recentServersStore.GetRecentServers();
                
                // Create buttons for each recent server
                float yOffset = 0f;
                foreach (var server in recentServers)
                {
                    var buttonGO = CreateRecentServerButton(server, yOffset);
                    recentServerButtons.Add(buttonGO);
                    yOffset += 32f; // Height + spacing
                }
            }
            catch (Exception ex)
            {
                logger.Error($"Error refreshing recent servers: {ex}");
            }
        }

        private GameObject CreateRecentServerButton(RecentServersStore.ServerEntry server, float yOffset)
        {
            var buttonGO = new GameObject($"RecentServer_{server.DisplayText}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            buttonGO.transform.SetParent(recentServersContainer, false);
            var rect = buttonGO.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.sizeDelta = new Vector2(440f, 28f);
            rect.anchoredPosition = new Vector2(0f, -yOffset);

            var image = buttonGO.GetComponent<Image>();
            image.color = new Color(0.15f, 0.16f, 0.20f, 0.8f);

            var colors = new ColorBlock
            {
                colorMultiplier = 1f,
                disabledColor = new Color(1f, 1f, 1f, 0.3f),
                highlightedColor = new Color(0.20f, 0.22f, 0.26f, 0.9f),
                normalColor = new Color(0.15f, 0.16f, 0.20f, 0.8f),
                pressedColor = new Color(0.12f, 0.13f, 0.17f, 0.9f),
                selectedColor = new Color(0.15f, 0.16f, 0.20f, 0.8f)
            };
            var button = buttonGO.GetComponent<Button>();
            button.colors = colors;
            button.onClick.AddListener(() => OnRecentServerClicked(server));

            var textGO = new GameObject("Text", typeof(RectTransform));
            textGO.transform.SetParent(buttonGO.transform, false);
            var textRect = textGO.GetComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0f, 0f);
            textRect.anchorMax = new Vector2(1f, 1f);
            textRect.pivot = new Vector2(0f, 0.5f);
            textRect.anchoredPosition = new Vector2(10f, 0f);
            textRect.sizeDelta = new Vector2(-20f, -4f);
            var text = textGO.AddComponent<TextMeshProUGUI>();
            text.text = server.DisplayText;
            text.fontSize = 16;
            text.alignment = TextAlignmentOptions.MidlineLeft;
            text.color = Color.white;

            return buttonGO;
        }

        private void OnConnectClicked()
        {
            try
            {
                string ip = ipInput != null ? ipInput.text : string.Empty;
                string portText = portInput != null ? portInput.text : string.Empty;
                
                if (string.IsNullOrWhiteSpace(ip))
                {
                    SetStatus("IP address is required.");
                    return;
                }
                
                if (!int.TryParse(portText, out int port) || port <= 0 || port > 65535)
                {
                    SetStatus("Invalid port. Enter a number between 1 and 65535.");
                    return;
                }

                // Add to recent servers
                recentServersStore.AddServer(ip.Trim(), port);

                // Connect
                connectionManager.SetTargetServer(ip.Trim(), port);
                SetStatus($"Connecting to {ip.Trim()}:{port}...");
                connectionManager.StartDedicatedConnection();
            }
            catch (Exception ex)
            {
                logger.Error($"Error handling connect click: {ex}");
                SetStatus("Error initiating connection.");
            }
        }

        private void OnRecentServerClicked(RecentServersStore.ServerEntry server)
        {
            try
            {
                // Add to recent servers (updates timestamp)
                recentServersStore.AddServer(server.Ip, server.Port);

                // Connect
                connectionManager.SetTargetServer(server.Ip, server.Port);
                SetStatus($"Connecting to {server.DisplayText}...");
                connectionManager.StartDedicatedConnection();
            }
            catch (Exception ex)
            {
                logger.Error($"Error handling recent server click: {ex}");
                SetStatus("Error initiating connection.");
            }
        }

        private void SetStatus(string message)
        {
            if (statusText != null)
            {
                statusText.text = message ?? string.Empty;
            }
        }
    }
}
