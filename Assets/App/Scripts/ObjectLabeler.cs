using System;
using System.Collections.Generic;
using System.Linq;
using CustomVision;
using CustomVison;
using HoloToolkit.Unity.InputModule;
using HoloToolkit.Unity.SpatialMapping;
using HoloToolkit.UX.ToolTips;
using HoloToolkitExtensions.Messaging;
using UnityEngine;

public class ObjectLabeler : MonoBehaviour, IInputClickHandler
{
    private List<GameObject> _createdObjects = new List<GameObject>();

    [SerializeField]
    private GameObject _labelObject;

    [SerializeField]
    private GameObject _labelContainer;

    [SerializeField]
    private string _labelText = "Person";

    [SerializeField]
    private GameObject _debugObject;


    [SerializeField]
    private AudioClip _succesSoundClip;

    [SerializeField]
    private AudioClip _failureSoundClip;

    private AudioSource _audio;


    private void Start()
    {
        Messenger.Instance.AddListener<ObjectRecognitionResultMessage>(
            p => LabelObjects(p.Predictions, p.CameraResolution, p.CameraTransform, p.Colors));
        _audio = GetComponent<AudioSource>();
    }

    public virtual void LabelObjects(IList<PredictionModel> predictions,
        Resolution cameraResolution, Transform cameraTransform, IList<Color[]> colors)
    {
        //if (predictions.Any())
        //{
        //    ClearLabels();
        //}

        var heightFactor = cameraResolution.height / cameraResolution.width;
        var topCorner = cameraTransform.position + cameraTransform.forward -
                        cameraTransform.right / 2f +
                        cameraTransform.up * heightFactor / 2f;

        PlayerPrefs.SetInt("width", cameraResolution.width);
        PlayerPrefs.SetInt("height", cameraResolution.height);

        PlaySound(predictions.Any());
        var color_ct = 0;
        foreach (var prediction in predictions)
        {
            var center = prediction.GetCenter();
            var recognizedPos = topCorner + cameraTransform.right * center.x -
                                cameraTransform.up * center.y * heightFactor;

#if UNITY_EDITOR
            _createdObjects.Add(CreateLabel(_labelText, recognizedPos, prediction, colors[color_ct]));
#endif
            var labelPos = DoRaycastOnSpatialMap(cameraTransform, recognizedPos);
            if (labelPos != null)
            {
                _createdObjects.Add(CreateLabel(_labelText, labelPos.Value, prediction, colors[color_ct]));
            }
        }

        if (_debugObject != null)
        {
            _debugObject.SetActive(false);
        }

        Destroy(cameraTransform.gameObject);
    }

    private Vector3 getRBGColor(IList<byte> image, float x, float y, Resolution cameraResolution)
    {
        x = x * cameraResolution.width;
        y = y * cameraResolution.height;

        byte R = image[3 * (int)(x + y * cameraResolution.width)];
        byte G = image[1 + 3 * (int)(x + y * cameraResolution.width)];
        byte B = image[2 + 3 * (int)(x + y * cameraResolution.width)];
        Debug.Log(R + B + G+"COLOR IS");
        return new Vector3(R, B, G);
    }

    private Vector3? DoRaycastOnSpatialMap(Transform cameraTransform, Vector3 recognitionCenterPos)
    {
        RaycastHit hitInfo;

        if (SpatialMappingManager.Instance != null &&
            Physics.Raycast(cameraTransform.position, (recognitionCenterPos - cameraTransform.position),
                out hitInfo, 10, SpatialMappingManager.Instance.LayerMask))
        {
            return hitInfo.point;
        }
        return null;
    }
    public void OnInputClicked(InputClickedEventData eventData)
    {
        if (PlayerPrefs.GetInt("Capture") == 1)
        {
            PlayerPrefs.SetInt("Capture", 0);
        }
        else if (PlayerPrefs.GetInt("Capture") == 0) {
            PlayerPrefs.SetInt("Capture", 1);
            ClearLabels();
        }

        //PlaySound();
        //TakePicture();
    }
    private void ClearLabels()
    {
        foreach (var label in _createdObjects)
        {
            Destroy(label);
        }
        _createdObjects.Clear();
    }

    private GameObject CreateLabel(string text, Vector3 location, PredictionModel prediction, Color[] color)
    {
        var labelObject = Instantiate(_labelObject);
        var toolTip = labelObject;//.GetComponent<GameObject>();
                                  //   toolTip.ShowOutline = false;
                                  // toolTip.ShowBackground = true;
                                  //toolTip.ToolTipText = text;
        MeshRenderer meshRenderer = toolTip.GetComponent<MeshRenderer>();
        Debug.Log(meshRenderer.material.color + " MYCOLOR FOR meshRenderer");
        // meshRenderer.material.color = color;


        Debug.Log("0");
        var w = (int)(prediction.BoundingBox.Width * PlayerPrefs.GetInt("width"));
        var h = (int)(prediction.BoundingBox.Height * PlayerPrefs.GetInt("height"));
        Debug.Log(h+"HHHWWW" + w);
        var texture = new Texture2D(w, h);


        Debug.Log("1 " + w + "(int)prediction.BoundingBox.Height" + h);
        
        Color[] pixels = Enumerable.Repeat(color[0], w * h).ToArray();
        Debug.Log("done INTI COLORS");
        Boolean changeColor = true;
        switch (PlayerPrefs.GetInt("mode")) {
            case 0:
                changeColor = false;
                break;
            case 1:
          
                break;
            case 4:
                Debug.Log("ColorsatCASE4" + color[0] + " " + color[1] + " " + color[2] + " " + color[3]);

                for (var i = 0; i < pixels.Length; i++)
                {
                    var a = i % w < (w / 2);
                    var b = i < h * w / 2;
                    if (a && b)
                        pixels[i] = color[0];
                    if (!a && b)
                        pixels[i] = color[1];
                    if (a && !b)
                        pixels[i] = color[2];
                    if (!a && !b)
                        pixels[i] = color[3];
                }
                break;
            case 16://pixels.Length
                Debug.Log(pixels.Length+"PIX LEN");

                Debug.Log(color[0]);
                Debug.Log(color[1]);
                Debug.Log(color[2]);
                Debug.Log(color[3]);

                for (var i = 0; i < pixels.Length; i++)
                {
                    var x = i % w % 4;
                    var y = (int)(i / w) % 4;
                 //   Debug.Log(y * 4 + x);

                    pixels[i] =color[ y * 4 + x];
                }
                break;       
            case 400://pixels.Length
                Debug.Log(pixels.Length + "PIX LEN!");

                Debug.Log(color[0]);
                Debug.Log(color[1]);
                Debug.Log(color[2]);
                Debug.Log(color[3]);
                for (var i = 0; i < pixels.Length; i++)
                {
                    var x = i % w % 20;
                    var y = (int)(i / w) % 20;
                 //   Debug.Log(y * 4 + x);

                    pixels[i] =color[ y * 20 + x];
                }
                break;

            case 2500:

                for (var i = 0; i < pixels.Length; i++)
                {
                    var x = i % w % 50;
                    var y = (int)(i / w) % 50;
                    //   Debug.Log(y * 4 + x);
                  
                    pixels[i] = color[y * 50 + x];

                    if (i == 51)
                    {
                        Debug.Log(pixels[i]);
                        Debug.Log("AT51 " + x + "_x_y" + y + "HW" + h + w + " " + (y * 50 + x) + "  " + color[y * 50 + x]);
                    }
                
                }

                Debug.Log(color[0] + "COLOR0 VS> PIX" + pixels[0]);
                Debug.Log(color[1] + "COLOR VS> PIX" + pixels[1]);
                Debug.Log(color[2] + "COLOR VS> PIX" + pixels[2]);
                Debug.Log(color[3] + "COLOR VS> PIX" + pixels[3]);
                Debug.Log(color[1] + "51  VS> PIX 1" + pixels[51]);

                break;
            case 50:
                int xmax = w / 50;
                int ymax = h / 50;
                Debug.Log(color.Length + "colorlenth"+pixels.Length);
                Debug.Log(xmax + "xmax" );
                
                for (int i = 0; i < color.Length; i++) {
                    int x = i % xmax;
                    int y = i / xmax;

                    for (int a = 0; a < 50; a++) {
                        for (int b = 0; b < 50; b++) {
                            if(x * 50 + (y + b) * w + a<pixels.Length)
                            pixels[x * 50 + (y + b) * w + a] = color[i];
                        } }
                }

                break;

        }

        //Debug.Log(color[1]);
        //Debug.Log(color[2]);
        //Debug.Log(color[3]);
        //Debug.Log(color[4]);


        //Debug.Log("2"+"  MODE "+ PlayerPrefs.GetInt("mode"));

        //Debug.Log(pixels[0]);
        //Debug.Log(pixels[1]); 
        //Debug.Log(pixels[2]); 
        //Debug.Log(pixels[3]);
        //Debug.Log(pixels[4]); 
        //Debug.Log(pixels[5]); 
        //Debug.Log(pixels[1000]);


        if (changeColor) {


            texture.SetPixels(pixels);
            Debug.Log("3");

            texture.Apply();
            Debug.Log("4");

            Debug.Log(meshRenderer.materials[0].name + " MYCOLOR FOR meshRenderer AFT");
            meshRenderer.materials[0].SetTexture("_MainTex", texture);
            //        Debug.Log(meshRenderer.material.GetTexture("_MainTex") + " _MainTex FOR meshRenderer Before");

        
        }


        //toolTip.GetComponent<MeshRenderer>().material.SetTexture("_MainTex", texture);

        toolTip.transform.position = location;
        toolTip.transform.parent = _labelContainer.transform;
        toolTip.transform.localScale =  new Vector3((float)prediction.BoundingBox.Width/2, (float)prediction.BoundingBox.Height/2, (float)prediction.BoundingBox.Width/2);
        //toolTip.AttachPointPosition = location;
        //toolTip.ContentParentTransform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
        // var connector = toolTip.GetComponent<ToolTipConnector>();
        //connector.PivotDirectionOrient = ConnectorOrientType.OrientToCamera;
        //connector.Target = labelObject;
        return labelObject;
    }

    private void PlaySound(bool success)
    {
        PlaySound(success ? _succesSoundClip : _failureSoundClip);
    }

    private void PlaySound(AudioClip clip)
    {
        if (clip != null && _audio != null)
        {
            _audio.clip = clip;
            _audio.Play();
        }
    }
}

