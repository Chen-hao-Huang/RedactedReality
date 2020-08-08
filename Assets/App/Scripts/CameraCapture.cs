using System.Collections.Generic;
using System.Linq;
using HoloToolkit.Unity;
using HoloToolkit.Unity.InputModule;
using HoloToolkitExtensions.Messaging;
using UnityEngine;
using UnityEngine.XR.WSA.WebCam;

public class CameraCapture : MonoBehaviour
{
    PhotoCapture _photoCaptureObject = null;
    private Resolution _cameraResolution;


    [SerializeField]
    private GameObject _debugPane;

    [SerializeField]
    private AudioClip _clickSoundClip;

    private AudioSource _audio;

    // Use this for initialization
    void Start()
    {
        PlayerPrefs.SetInt("Capture", 1); //Capture 1:working    0:pause 
        _cameraResolution =
            PhotoCapture.SupportedResolutions.OrderByDescending(res => res.width * res.height).First();
        _audio = GetComponent<AudioSource>();
        PhotoCapture.CreateAsync(false, delegate (PhotoCapture captureObject)
        {
            _photoCaptureObject = captureObject;
            CameraParameters cameraParameters = new CameraParameters
            {
                hologramOpacity = 0.0f,
                cameraResolutionWidth = _cameraResolution.width,
                cameraResolutionHeight = _cameraResolution.height,
                pixelFormat = _debugPane != null ? CapturePixelFormat.BGRA32 : CapturePixelFormat.JPEG
            };

            // Activate the camera
            _photoCaptureObject.StartPhotoModeAsync(cameraParameters, p =>
            {
                _photoCaptureObject.TakePhotoAsync(OnCapturedPhotoToMemory);
            });
        });

    }

    public void TakePicture()
    {
        // Create a PhotoCapture object
        _photoCaptureObject.TakePhotoAsync(OnCapturedPhotoToMemory);
    }

    void OnCapturedPhotoToMemory(PhotoCapture.PhotoCaptureResult result, PhotoCaptureFrame photoCaptureFrame)
    {
        var photoBuffer = new List<byte>();
        
        if (photoCaptureFrame.pixelFormat == CapturePixelFormat.JPEG)
        {
            photoCaptureFrame.CopyRawImageDataIntoBuffer(photoBuffer);
        }
        else
        {
            photoBuffer = ConvertAndShowOnDebugPane(photoCaptureFrame);
        }

        Messenger.Instance.Broadcast(
            new PhotoCaptureMessage(photoBuffer, _cameraResolution, CopyCameraTransForm()));
   
        // Deactivate our camera
       // _photoCaptureObject.StopPhotoModeAsync(OnStoppedPhotoMode);
    }

    void OnStoppedPhotoMode(PhotoCapture.PhotoCaptureResult result)
    {
        // Shutdown our photo capture resource
        _photoCaptureObject.Dispose();
        _photoCaptureObject = null;
    }

    private List<byte> ConvertAndShowOnDebugPane(PhotoCaptureFrame photoCaptureFrame)
    {
        var targetTexture = new Texture2D(_cameraResolution.width, _cameraResolution.height);
        photoCaptureFrame.UploadImageDataToTexture(targetTexture);
        Destroy(_debugPane.GetComponent<Renderer>().material.mainTexture);

        _debugPane.GetComponent<Renderer>().material.mainTexture = targetTexture;
        _debugPane.transform.parent.gameObject.SetActive(true);
        return new List<byte>(targetTexture.EncodeToJPG());
    }

    private float nextActionTime = 0.0f;
    private float period = 5f;

    void Update()
    {
        if (PlayerPrefs.GetInt("Capture") == 1)
        {
            if (Time.time > nextActionTime)
            {

                nextActionTime = Time.time + period;
                PlaySound();
                TakePicture();
            }
        }
    }


    private void OnApplicationQuit()
    {
        _photoCaptureObject.StopPhotoModeAsync(OnStoppedPhotoMode);
    }

    //public void OnInputClicked(InputClickedEventData eventData)
    //{
         
    //    //PlaySound();
    //    //TakePicture();
    //}

    private Transform CopyCameraTransForm()
    {
        var g = new GameObject();
        g.transform.position = CameraCache.Main.transform.position;
        g.transform.rotation = CameraCache.Main.transform.rotation;
        g.transform.localScale = CameraCache.Main.transform.localScale;
        return g.transform;
    }

    private void PlaySound()
    {
        if (_audio != null && _clickSoundClip != null)
        {
            _audio.clip = _clickSoundClip;
            _audio.Play();
        }
    }
}
