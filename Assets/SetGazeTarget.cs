using i5.VirtualAgents;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using System.Collections;

public class SetGazeTarget : MonoBehaviour
{

    ARTrackedImageManager m_TrackedImageManager;

    void OnEnable()
    {
        m_TrackedImageManager = GetComponent<ARTrackedImageManager>();
        m_TrackedImageManager.trackablesChanged.AddListener(OnTrackedImagesChanged);
    }

    void OnDisable()
    {
        m_TrackedImageManager.trackablesChanged.RemoveListener(OnTrackedImagesChanged);
        m_TrackedImageManager.enabled = false;
    }

    void OnTrackedImagesChanged(ARTrackablesChangedEventArgs<ARTrackedImage> eventArgs)
    {

        foreach (var trackedImage in eventArgs.added)
        {
            Debug.Log("tracked image found");
            AdaptiveGaze ag = trackedImage.GetComponent<AdaptiveGaze>();
            ag.OverwriteGazeTarget = GameObject.Find("Cube").transform;
        }

    }

}
