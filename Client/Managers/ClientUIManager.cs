using MelonLoader;
using System;
using System.Collections;
using DedicatedServerMod.Client.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

namespace DedicatedServerMod.Client.Managers
{
    /// <summary>
    /// Manages UI additions for the dedicated server client mod.
    /// Handles adding the server connection button and dialog.
    /// </summary>
    public class ClientUIManager
    {
        private readonly MelonLogger.Instance logger;
        private readonly ClientConnectionManager connectionManager;
        
        // UI state
        private GameObject serversButton;
        private bool menuUISetup = false;

        // UI Components
        private ConnectDialog connectDialog;
        private RecentServersStore recentServersStore;

        // Menu animation controller
        private MenuAnimationController menuAnimationController;

        /// <summary>
        /// Initializes a new instance of the ClientUIManager class.
        /// </summary>
        /// <param name="logger">Logger instance.</param>
        /// <param name="connectionManager">Connection manager instance.</param>
        public ClientUIManager(MelonLogger.Instance logger, ClientConnectionManager connectionManager)
        {
            this.logger = logger;
            this.connectionManager = connectionManager;
        }

        /// <summary>
        /// Initializes the UI manager.
        /// </summary>
        public void Initialize()
        {
            try
            {
                logger.Msg("Initializing ClientUIManager");
                
                // Initialize menu animation controller
                menuAnimationController = new MenuAnimationController(logger);
                
                // Initialize recent servers store
                recentServersStore = new RecentServersStore(logger);
                
                // UI will be setup when menu scene loads
                
                logger.Msg("ClientUIManager initialized");
            }
            catch (Exception ex)
            {
                logger.Error($"Failed to initialize ClientUIManager: {ex}");
            }
        }

        /// <summary>
        /// Handle scene loading events
        /// </summary>
        /// <param name="sceneName">Name of the loaded scene.</param>
        public void OnSceneLoaded(string sceneName)
        {
            try
            {
                if (sceneName == "Menu" && !menuUISetup)
                {
                    logger.Msg("Menu scene loaded - setting up UI");
                    MelonCoroutines.Start(SetupMenuUI());
                }
                else if (sceneName == "Menu")
                {
                    // Reset animation controller when returning to menu
                    menuAnimationController?.Reset();
                }
            }
            catch (Exception ex)
            {
                logger.Error($"Error handling scene load ({sceneName}): {ex}");
            }
        }

        /// <summary>
        /// Setup UI elements in the main menu
        /// </summary>
        private IEnumerator SetupMenuUI()
        {
            // Wait for main menu to be fully loaded
            yield return new WaitForSeconds(0.5f);
            
            try
            {
                if (AddServersButton())
                {
                    menuUISetup = true;
                    logger.Msg("Menu UI setup completed");
                }
                else
                {
                    logger.Warning("Failed to setup menu UI - will retry next time menu loads");
                    menuUISetup = false;
                }
            }
            catch (Exception ex)
            {
                logger.Error($"Error setting up menu UI: {ex}");
                menuUISetup = false;
            }
        }

        /// <summary>
        /// Add the server connection button to the main menu
        /// </summary>
        private bool AddServersButton()
        {
            try
            {
                // Find main menu structure
                var mainMenu = GameObject.Find("MainMenu");
                if (mainMenu == null)
                {
                    logger.Warning("MainMenu not found");
                    return false;
                }

                var home = mainMenu.transform.Find("Home");
                if (home == null)
                {
                    logger.Warning("Home not found in MainMenu");
                    return false;
                }

                var bank = home.Find("Bank");
                if (bank == null)
                {
                    logger.Warning("Bank not found in Home");
                    return false;
                }

                var continueButton = bank.Find("Continue");
                if (continueButton == null)
                {
                    logger.Warning("Continue button not found");
                    return false;
                }

                // Create servers button by cloning continue button
                serversButton = GameObject.Instantiate(continueButton.gameObject, bank);
                serversButton.name = "ServersButton";
                
                // Position it to the right of the continue button
                PositionServersButton(serversButton, continueButton);
                
                // Update button appearance
                UpdateServersButtonText(serversButton);
                
                // Remove persistent onClick listeners from the cloned button
                StripPersistentOnClick(serversButton);

                // Setup button functionality
                SetupServersButtonClick(serversButton);

                // Create the connect dialog
                CreateConnectDialog(mainMenu.transform);

                logger.Msg("Servers button added to main menu successfully");
                return true;
            }
            catch (Exception ex)
            {
                logger.Error($"Error adding servers button: {ex}");
                return false;
            }
        }

        /// <summary>
        /// Position the servers button relative to the continue button
        /// </summary>
        private void PositionServersButton(GameObject newButton, Transform continueButton)
        {
            var rectTransform = newButton.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                var pos = rectTransform.anchoredPosition;
                rectTransform.anchoredPosition = new Vector2(pos.x + 100f, pos.y);
            }
        }

        /// <summary>
        /// Update the button text to reflect its purpose
        /// </summary>
        private void UpdateServersButtonText(GameObject button)
        {
            var tmp = button.GetComponentInChildren<TMP_Text>();
            if (tmp != null)
            {
                tmp.text = "Servers";
                return;
            }
            var legacy = button.GetComponentInChildren<Text>();
            if (legacy != null)
            {
                legacy.text = "Servers";
                return;
            }
            logger.Warning("Could not find text component on servers button");
        }

        /// <summary>
        /// Remove any persistent listeners copied from the original Continue button.
        /// </summary>
        private void StripPersistentOnClick(GameObject button)
        {
            try
            {
                var btn = button.GetComponent<Button>();
                if (btn == null)
                {
                    logger.Warning("StripPersistentOnClick: Button component not found");
                    return;
                }

                // Replace the entire UnityEvent to ensure no persistent listeners remain
                btn.onClick = new Button.ButtonClickedEvent();
            }
            catch (Exception ex)
            {
                logger.Error($"Error stripping onClick listeners: {ex}");
            }
        }

        /// <summary>
        /// Setup the button click handler
        /// </summary>
        private void SetupServersButtonClick(GameObject button)
        {
            var buttonComponent = button.GetComponent<Button>();
            if (buttonComponent != null)
            {
                buttonComponent.onClick.RemoveAllListeners();
                buttonComponent.onClick.AddListener(OnServersButtonClicked);
            }
            else
            {
                logger.Warning("Could not find Button component on servers button");
            }
        }

        /// <summary>
        /// Handle servers button click
        /// </summary>
        private void OnServersButtonClicked()
        {
            try
            {
                logger.Msg("Servers button clicked - opening connect dialog");
                ShowConnectDialog(true);
            }
            catch (Exception ex)
            {
                logger.Error($"Error handling servers button click: {ex}");
            }
        }

        /// <summary>
        /// Creates the connect dialog
        /// </summary>
        private void CreateConnectDialog(Transform parent)
        {
            try
            {
                connectDialog = new ConnectDialog(logger, connectionManager, recentServersStore);
                connectDialog.Create(parent);
                logger.Msg("Connect dialog created");
            }
            catch (Exception ex)
            {
                logger.Error($"Error creating connect dialog: {ex}");
            }
        }

        /// <summary>
        /// Shows or hides the connect dialog
        /// </summary>
        /// <param name="show">True to show, false to hide.</param>
        private void ShowConnectDialog(bool show)
        {
            try
            {
                // Hide/show main menu buttons using animation controller
                menuAnimationController?.ToggleMenuVisibility(!show);

                // Show/hide the dialog
                connectDialog?.Show(show);
            }
            catch (Exception ex)
            {
                logger.Error($"Error showing/hiding connect dialog: {ex}");
            }
        }

        /// <summary>
        /// Update button text based on connection state
        /// </summary>
        public void UpdateButtonState()
        {
            if (serversButton == null)
                return;

            try
            {
                var textComponent = serversButton.GetComponentInChildren<Text>();
                if (textComponent != null)
                {
                    textComponent.text = "Servers";
                }
            }
            catch (Exception ex)
            {
                logger.Error($"Error updating button state: {ex}");
            }
        }

        /// <summary>
        /// Show connection status in a temporary UI element
        /// </summary>
        public void ShowConnectionStatus()
        {
            try
            {
                var status = connectionManager.GetConnectionStatus();
                logger.Msg("=== Connection Status ===");
                logger.Msg(status);
            }
            catch (Exception ex)
            {
                logger.Error($"Error showing connection status: {ex}");
            }
        }

        /// <summary>
        /// Add debug UI elements (for development/testing)
        /// </summary>
        public void AddDebugUI()
        {
            try
            {
                logger.Msg("Debug UI available via console commands");
            }
            catch (Exception ex)
            {
                logger.Error($"Error adding debug UI: {ex}");
            }
        }

        /// <summary>
        /// Handle debug key inputs for UI testing
        /// </summary>
        public void HandleDebugInput()
        {
            try
            {
                // F8 - Show connection status
                if (Input.GetKeyDown(KeyCode.F8))
                {
                    ShowConnectionStatus();
                }

                // F10 - Update button state
                if (Input.GetKeyDown(KeyCode.F10))
                {
                    UpdateButtonState();
                }
            }
            catch (Exception ex)
            {
                logger.Error($"Error handling debug input: {ex}");
            }
        }

        /// <summary>
        /// Handle input for closing dialogs
        /// </summary>
        public void HandleInput()
        {
            try
            {
                // ESC - Close connect dialog
                if (Input.GetKeyDown(KeyCode.Escape))
                {
                    if (connectDialog != null)
                    {
                        ShowConnectDialog(false);
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error($"Error handling input: {ex}");
            }
        }

        /// <summary>
        /// Remove UI elements when cleaning up
        /// </summary>
        public void Cleanup()
        {
            try
            {
                if (serversButton != null)
                {
                    GameObject.Destroy(serversButton);
                    serversButton = null;
                }
                
                connectDialog?.Cleanup();
                connectDialog = null;

                menuUISetup = false;
                logger.Msg("UI elements cleaned up");
            }
            catch (Exception ex)
            {
                logger.Error($"Error during UI cleanup: {ex}");
            }
        }

        /// <summary>
        /// Reset UI state when returning to menu
        /// </summary>
        public void ResetUIState()
        {
            try
            {
                menuUISetup = false;
                menuAnimationController?.Reset();
                UpdateButtonState();
            }
            catch (Exception ex)
            {
                logger.Error($"Error resetting UI state: {ex}");
            }
        }
    }
}
