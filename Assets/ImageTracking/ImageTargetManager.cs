using UnityEngine;
using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using Unity.Jobs;
using UnityEngine.UI;

using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

[RequireComponent(typeof(ARTrackedImageManager))]
public class ImageTargetManager : MonoBehaviour
{

    ARSession m_Session;

    [SerializeField]
    [Tooltip("The camera to set on the world space UI canvas for each instantiated image info.")]
    Camera m_WorldSpaceCanvasCamera;

    /// <summary>
    /// The prefab has a world space UI canvas,
    /// which requires a camera to function properly.
    /// </summary>
    public Camera worldSpaceCanvasCamera
    {
        get { return m_WorldSpaceCanvasCamera; }
        set { m_WorldSpaceCanvasCamera = value; }
    }

    [Serializable]
    public class ImageData
    {
        [SerializeField, Tooltip("The source texture for the image. Must be marked as readable.")]
        Texture2D m_Texture;

        public Texture2D texture
        {
            get => m_Texture;
            set => m_Texture = value;
        }

        [SerializeField, Tooltip("The name for this image.")]
        string m_Name;

        public string name
        {
            get => m_Name;
            set => m_Name = value;
        }

        [SerializeField, Tooltip("The width, in meters, of the image in the real world.")]
        float m_Width;

        public float width
        {
            get => m_Width;
            set => m_Width = value;
        }

        public AddReferenceImageJobState jobState { get; set; }
    }

    [SerializeField, Tooltip("The set of images to add to the image library at runtime")]
    ImageData[] m_Images;

    /// <summary>
    /// The set of images to add to the image library at runtime
    /// </summary>
    public ImageData[] images
    {
        get => m_Images;
        set => m_Images = value;
    }

    ARTrackedImageManager m_TrackedImageManager;

    void Awake()
    {
    }

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

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        StartCoroutine(CheckSupport());   
    }

    void AddImageTargets()
    {
        if (m_TrackedImageManager == null)
        {
            Debug.LogError($"No {nameof(ARTrackedImageManager)} available.");
        }

        // You can either add raw image bytes or use the extension method (used below) which accepts
        // a texture. To use a texture, however, its import settings must have enabled read/write
        // access to the texture.
        foreach (var image in m_Images)
        {
            if (!image.texture.isReadable)
            {
                Debug.LogError($"Image {image.name} must be readable to be added to the image library.");
            }
        }

        if (m_TrackedImageManager.referenceLibrary is MutableRuntimeReferenceImageLibrary mutableLibrary)
        {
            try
            {

                m_TrackedImageManager.referenceLibrary ??= m_TrackedImageManager.CreateRuntimeLibrary();
                m_TrackedImageManager.requestedMaxNumberOfMovingImages = 10;

                foreach (var image in m_Images)
                {
                    // Note: You do not need to do anything with the returned JobHandle, but it can be
                    // useful if you want to know when the image has been added to the library since it may
                    // take several frames.
                    image.jobState = mutableLibrary.ScheduleAddImageWithValidationJob(image.texture, image.name, image.width);
                    m_TrackedImageManager.requestedMaxNumberOfMovingImages += 1;
                }

            }
            catch (InvalidOperationException e)
            {
                Debug.LogError($"ScheduleAddImageJob threw exception: {e.Message}");
            }
        }
        else
        {
            Debug.LogError($"The reference image library is not mutable.");
        }

        m_TrackedImageManager.enabled = true;
        Debug.Log("Finished adding image targets.");
    }

    void OnTrackedImagesChanged(ARTrackablesChangedEventArgs<ARTrackedImage> eventArgs)
    {

        Debug.Log("---- OnTrackedImageChanged");
        
        foreach (var trackedImage in eventArgs.added)
        {
            Debug.Log("tracked image found");
        }

        foreach (var trackedImage in eventArgs.updated)
        {
            Debug.Log("tracked image update received");
        }

        foreach (var removed in eventArgs.removed)
        {
            // Handle removed event
            TrackableId removedImageTrackableId = removed.Key;
            ARTrackedImage removedImage = removed.Value;
            Debug.Log("tracking lost for " + removed.Value);
        }

    }

    void UpdateInfo(ARTrackedImage trackedImage)
    {
        // Set canvas camera
        var canvas = trackedImage.GetComponentInChildren<Canvas>();
        canvas.worldCamera = worldSpaceCanvasCamera;

        // Update information about the tracked image
        var text = canvas.GetComponentInChildren<Text>();
        text.text = string.Format(
            "{0}\ntrackingState: {1}\nGUID: {2}\nReference size: {3} cm\nDetected size: {4} cm",
            trackedImage.referenceImage.name,
            trackedImage.trackingState,
            trackedImage.referenceImage.guid,
            trackedImage.referenceImage.size * 100f,
            trackedImage.size * 100f);

        var planeParentGo = trackedImage.transform.GetChild(0).gameObject;
        var planeGo = planeParentGo.transform.GetChild(0).gameObject;

        // Disable the visual plane if it is not being tracked
        if (trackedImage.trackingState != TrackingState.None)
        {
            planeGo.SetActive(true);

            // The image extents is only valid when the image is being tracked
            trackedImage.transform.localScale = new Vector3(trackedImage.size.x, 1f, trackedImage.size.y);

            // Set the texture
            var material = planeGo.GetComponentInChildren<MeshRenderer>().material;
            material.mainTexture = trackedImage.referenceImage.texture;
        }
        else
        {
            planeGo.SetActive(false);
        }
    }

    // Update is called once per frame
    void Update()
    {

    }

    IEnumerator CheckSupport()
    {
        Debug.Log("Checking if AR session is available...");
        yield return ARSession.CheckAvailability();

        if (ARSession.state == ARSessionState.Ready || ARSession.state == ARSessionState.SessionTracking)
        {
            Debug.Log("AR session is tracking.");
            AddImageTargets();
        } else
        {
            Debug.LogError("Error: AR subsystem not ready:" + ARSession.state);
        }
    }
}