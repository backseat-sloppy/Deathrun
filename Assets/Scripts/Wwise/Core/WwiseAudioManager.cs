using UnityEngine;
using System.Collections.Generic;

namespace DeathrunGame.Audio
{
    /// <summary>
    /// Singleton manager for all Wwise audio operations in the game.
    /// Provides centralized control over audio events, parameters, and state management.
    /// Handles initialization, cleanup, and provides convenient methods for common audio operations.
    /// </summary>
    public class WwiseAudioManager : MonoBehaviour
    {
        #region Singleton
        private static WwiseAudioManager _instance;
        public static WwiseAudioManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<WwiseAudioManager>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("WwiseAudioManager");
                        _instance = go.AddComponent<WwiseAudioManager>();
                        DontDestroyOnLoad(go);
                    }
                }
                return _instance;
            }
        }
        #endregion

        #region Events
        public static event System.Action<string, GameObject> OnAudioEventPosted;
        public static event System.Action<string, float, GameObject> OnParameterSet;
        public static event System.Action<string, string, GameObject> OnSwitchSet;
        #endregion

        #region Private Fields
        private readonly Dictionary<uint, GameObject> _playingEvents = new Dictionary<uint, GameObject>();
        private readonly Dictionary<GameObject, uint> _gameObjectToAudioId = new Dictionary<GameObject, uint>();
        private bool _isInitialized = false;
        #endregion

        #region Unity Methods
        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeAudioManager();
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
            }
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                CleanupAudioManager();
            }
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
            {
                PauseAllAudio();
            }
            else
            {
                ResumeAllAudio();
            }
        }
        #endregion

        #region Initialization
        private void InitializeAudioManager()
        {
            if (_isInitialized) return;

            // Ensure Wwise is initialized
            if (!AkUnitySoundEngine.IsInitialized())
            {
                Debug.LogError("WwiseAudioManager: Wwise is not initialized! Audio will not work.");
                return;
            }

            _isInitialized = true;
            Debug.Log("WwiseAudioManager initialized successfully.");
        }

        private void CleanupAudioManager()
        {
            StopAllAudio();
            _playingEvents.Clear();
            _gameObjectToAudioId.Clear();
            _isInitialized = false;
        }
        #endregion

        #region Public API - Event Posting
        /// <summary>
        /// Post a Wwise event on a specific GameObject.
        /// </summary>
        /// <param name="eventName">Name of the Wwise event</param>
        /// <param name="gameObject">GameObject to post the event on</param>
        /// <returns>Playing ID of the posted event</returns>
        public uint PostEvent(string eventName, GameObject gameObject)
        {
            if (!_isInitialized || string.IsNullOrEmpty(eventName) || gameObject == null)
            {
                Debug.LogWarning($"WwiseAudioManager: Cannot post event '{eventName}' - invalid parameters or not initialized.");
                return AkUnitySoundEngine.AK_INVALID_PLAYING_ID;
            }

            uint playingId = AkUnitySoundEngine.PostEvent(eventName, gameObject);
            
            if (playingId != AkUnitySoundEngine.AK_INVALID_PLAYING_ID)
            {
                _playingEvents[playingId] = gameObject;
                OnAudioEventPosted?.Invoke(eventName, gameObject);
                
                Debug.Log($"Posted Wwise event '{eventName}' on {gameObject.name} (PlayingID: {playingId})");
            }
            else
            {
                Debug.LogWarning($"Failed to post Wwise event '{eventName}' on {gameObject.name}");
            }

            return playingId;
        }

        /// <summary>
        /// Post a Wwise event with a callback.
        /// </summary>
        /// <param name="eventName">Name of the Wwise event</param>
        /// <param name="gameObject">GameObject to post the event on</param>
        /// <param name="callback">Callback function</param>
        /// <param name="cookie">Optional data to pass to callback</param>
        /// <returns>Playing ID of the posted event</returns>
        public uint PostEvent(string eventName, GameObject gameObject, AkCallbackManager.EventCallback callback, object cookie = null)
        {
            if (!_isInitialized || string.IsNullOrEmpty(eventName) || gameObject == null)
            {
                Debug.LogWarning($"WwiseAudioManager: Cannot post event '{eventName}' with callback - invalid parameters or not initialized.");
                return AkUnitySoundEngine.AK_INVALID_PLAYING_ID;
            }

            uint callbackFlags = (uint)(AkCallbackType.AK_EndOfEvent | AkCallbackType.AK_Duration);
            uint playingId = AkUnitySoundEngine.PostEvent(eventName, gameObject, callbackFlags, callback, cookie);
            
            if (playingId != AkUnitySoundEngine.AK_INVALID_PLAYING_ID)
            {
                _playingEvents[playingId] = gameObject;
                OnAudioEventPosted?.Invoke(eventName, gameObject);
            }

            return playingId;
        }

        /// <summary>
        /// Stop a specific playing event.
        /// </summary>
        /// <param name="playingId">Playing ID to stop</param>
        /// <param name="fadeOutDuration">Fade out duration in milliseconds</param>
        public void StopEvent(uint playingId, int fadeOutDuration = 0)
        {
            if (playingId == AkUnitySoundEngine.AK_INVALID_PLAYING_ID) return;

            AkUnitySoundEngine.StopPlayingID(playingId, fadeOutDuration);
            _playingEvents.Remove(playingId);
        }

        /// <summary>
        /// Stop all events on a specific GameObject.
        /// </summary>
        /// <param name="gameObject">GameObject to stop events on</param>
        public void StopAllEvents(GameObject gameObject)
        {
            if (gameObject == null) return;

            AkUnitySoundEngine.StopAll(gameObject);
            
            // Clean up tracking dictionaries
            var keysToRemove = new List<uint>();
            foreach (var kvp in _playingEvents)
            {
                if (kvp.Value == gameObject)
                {
                    keysToRemove.Add(kvp.Key);
                }
            }
            
            foreach (uint key in keysToRemove)
            {
                _playingEvents.Remove(key);
            }
        }
        #endregion

        #region Public API - Parameters (RTPCs)
        /// <summary>
        /// Set a game parameter (RTPC) value on a specific GameObject.
        /// </summary>
        /// <param name="parameterName">Name of the parameter</param>
        /// <param name="value">Value to set</param>
        /// <param name="gameObject">GameObject to set parameter on (null for global)</param>
        public void SetParameter(string parameterName, float value, GameObject gameObject = null)
        {
            if (!_isInitialized || string.IsNullOrEmpty(parameterName))
            {
                Debug.LogWarning($"WwiseAudioManager: Cannot set parameter '{parameterName}' - invalid parameters or not initialized.");
                return;
            }

            AKRESULT result;
            if (gameObject == null)
            {
                // Global parameter
                result = AkUnitySoundEngine.SetRTPCValue(parameterName, value);
            }
            else
            {
                // GameObject-specific parameter
                result = AkUnitySoundEngine.SetRTPCValue(parameterName, value, gameObject);
            }

            if (result == AKRESULT.AK_Success)
            {
                OnParameterSet?.Invoke(parameterName, value, gameObject);
                Debug.Log($"Set parameter '{parameterName}' to {value} on {(gameObject ? gameObject.name : "Global")}");
            }
            else
            {
                Debug.LogWarning($"Failed to set parameter '{parameterName}' to {value}: {result}");
            }
        }
        #endregion

        #region Public API - Switches
        /// <summary>
        /// Set a switch value on a specific GameObject.
        /// </summary>
        /// <param name="switchGroup">Name of the switch group</param>
        /// <param name="switchValue">Name of the switch value</param>
        /// <param name="gameObject">GameObject to set switch on</param>
        public void SetSwitch(string switchGroup, string switchValue, GameObject gameObject)
        {
            if (!_isInitialized || string.IsNullOrEmpty(switchGroup) || string.IsNullOrEmpty(switchValue) || gameObject == null)
            {
                Debug.LogWarning($"WwiseAudioManager: Cannot set switch '{switchGroup}:{switchValue}' - invalid parameters or not initialized.");
                return;
            }

            AKRESULT result = AkUnitySoundEngine.SetSwitch(switchGroup, switchValue, gameObject);
            
            if (result == AKRESULT.AK_Success)
            {
                OnSwitchSet?.Invoke(switchGroup, switchValue, gameObject);
                Debug.Log($"Set switch '{switchGroup}' to '{switchValue}' on {gameObject.name}");
            }
            else
            {
                Debug.LogWarning($"Failed to set switch '{switchGroup}' to '{switchValue}': {result}");
            }
        }

        /// <summary>
        /// Set surface type switch for footstep audio.
        /// </summary>
        /// <param name="surfaceType">Surface type to set</param>
        /// <param name="gameObject">GameObject to set switch on</param>
        public void SetSurfaceType(SurfaceType surfaceType, GameObject gameObject)
        {
            string switchValue = WwiseSwitches.GetSurfaceSwitch(surfaceType);
            SetSwitch(WwiseSwitches.SURFACE_TYPE_GROUP, switchValue, gameObject);
        }
        #endregion

        #region Public API - Utility
        /// <summary>
        /// Stop all audio in the game.
        /// </summary>
        public void StopAllAudio()
        {
            AkUnitySoundEngine.StopAll();
            _playingEvents.Clear();
        }

        /// <summary>
        /// Pause all audio in the game.
        /// </summary>
        public void PauseAllAudio()
        {
            AkUnitySoundEngine.ExecuteActionOnEvent("", AkActionOnEventType.AkActionOnEventType_Pause);
        }

        /// <summary>
        /// Resume all audio in the game.
        /// </summary>
        public void ResumeAllAudio()
        {
            AkUnitySoundEngine.ExecuteActionOnEvent("", AkActionOnEventType.AkActionOnEventType_Resume);
        }

        /// <summary>
        /// Check if Wwise is properly initialized.
        /// </summary>
        /// <returns>True if initialized</returns>
        public bool IsInitialized()
        {
            return _isInitialized && AkUnitySoundEngine.IsInitialized();
        }

        /// <summary>
        /// Get the GameObject associated with a playing ID.
        /// </summary>
        /// <param name="playingId">Playing ID to look up</param>
        /// <returns>Associated GameObject or null</returns>
        public GameObject GetGameObjectForPlayingId(uint playingId)
        {
            _playingEvents.TryGetValue(playingId, out GameObject go);
            return go;
        }
        #endregion
    }
}