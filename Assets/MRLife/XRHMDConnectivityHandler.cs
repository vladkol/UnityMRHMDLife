using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace XR
{
    /// <summary>
    /// This script checks an XR HMD presense and ajust XRSettings.enabled property accordingly. 
    /// It may also show a game object assigned to objectToShowWhenNoHMD based on the HMD connectivity. If an HMD is not connected, the script will make this object active. 
    /// If an HMD is connected, sceneTransitionProgressObject becomes active, and sceneToLoadInXR scene will be loaded. 
    /// If there is no HMD is initially connected, the moment when the script detects an HMD, it will restart the app. 
    /// </summary>
    public class XRHMDConnectivityHandler : MonoBehaviour
    {
        /// <summary>
        ///  This object becomes active when there is no HMD connected, and inactive when there is one or XR/VR support is not enabled. 
        /// </summary>
        public Transform objectToActivateWhenNoHMD;

        /// <summary>
        ///  This object becomes active if an HMD is connected. 
        /// </summary>
        public Transform sceneTransitionProgressObject;

        /// <summary>
        ///  This scene will be loaded if an HMD is connected on startup. 
        /// </summary>
        public string sceneToLoadInXR = string.Empty;

        /// <summary>
        ///  An event for handling when HMD is connected on startup. 
        /// </summary>
        public event System.Action<XRHMDConnectivityHandler> headsetConnected;

        public bool hasHMD
        {
            get
            {
                return _hasHMD;
            }
        }

        private bool _hasHMD = false;


        private void Awake()
        {
#if UNITY_WSA_10_0 && !UNITY_EDITOR && ENABLE_WINMD_SUPPORT
        Windows.Graphics.Holographic.HolographicSpace.IsAvailableChanged += HolographicSpace_IsAvailableChanged;
#endif
            if (objectToActivateWhenNoHMD != null)
            {
                objectToActivateWhenNoHMD.gameObject.SetActive(false);
            }
            if (sceneTransitionProgressObject != null)
            {
                sceneTransitionProgressObject.gameObject.SetActive(false);
            }


            if (IsXREnabled())
            {
                _hasHMD = IsXRDeviceAvailable();
                SetXREnabled(_hasHMD);
            }
        }

#if UNITY_WSA_10_0 && !UNITY_EDITOR && ENABLE_WINMD_SUPPORT
        private void HolographicSpace_IsAvailableChanged(object sender, object e)
        {
            if (Windows.Graphics.Holographic.HolographicSpace.IsAvailable)
            {
                _hasHMD = true;
                UnityEngine.WSA.Application.InvokeOnUIThread(() =>
                {
#pragma warning disable 4014
                    Windows.ApplicationModel.Core.CoreApplication.RequestRestartAsync(string.Empty);
#pragma warning restore 4014
                }, false);
            }
        }
#endif


        private void Start()
        {
            if (IsXRDeviceAvailable() && !string.IsNullOrEmpty(sceneToLoadInXR))
            {
                if (objectToActivateWhenNoHMD != null)
                {
                    objectToActivateWhenNoHMD.gameObject.SetActive(false);
                }

                if(sceneTransitionProgressObject != null)
                {
                    sceneTransitionProgressObject.gameObject.SetActive(true);
                }

                if (headsetConnected != null)
                {
                    try
                    {
                        headsetConnected(this);
                    }
                    catch { }
                }

                UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(sceneToLoadInXR);
            }
            else
            {
                UpdateNoHMDObject();
            }
        }


#if UNITY_EDITOR
        private void Update()
        {
            if (IsXRDeviceAvailable() && !string.IsNullOrEmpty(sceneToLoadInXR))
            {
                if (objectToActivateWhenNoHMD != null)
                {
                    objectToActivateWhenNoHMD.gameObject.SetActive(false);
                }

                if (sceneTransitionProgressObject != null)
                {
                    sceneTransitionProgressObject.gameObject.SetActive(true);
                }
                UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(sceneToLoadInXR);
                sceneToLoadInXR = string.Empty;
            }
        }
#endif


        private void UpdateNoHMDObject()
        {
            if (objectToActivateWhenNoHMD != null)
            {
                objectToActivateWhenNoHMD.gameObject.SetActive(!_hasHMD);
            }
        }


        private static bool IsXREnabled()
        {
#if UNITY_2017_2_OR_NEWER
        return UnityEngine.XR.XRSettings.enabled;
#else
            return UnityEngine.VR.VRSettings.enabled;
#endif
        }


        private static bool IsXRDeviceAvailable()
        {
#if UNITY_WSA_10_0 && !UNITY_EDITOR && ENABLE_WINMD_SUPPORT
        return Windows.Graphics.Holographic.HolographicSpace.IsAvailable;
#else
#if UNITY_2017_2_OR_NEWER
        return UnityEngine.XR.XRDevice.isPresent;
#else
        return UnityEngine.VR.VRDevice.isPresent;
#endif
#endif
        }


        private static void SetXREnabled(bool enabled)
        {
#if UNITY_2017_2_OR_NEWER
        UnityEngine.XR.XRSettings.enabled = enabled;
#else
            UnityEngine.VR.VRSettings.enabled = enabled;
#endif
        }
    }
}
