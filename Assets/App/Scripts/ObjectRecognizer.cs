using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CustomVision;
using System.Runtime.InteropServices.WindowsRuntime;
using HoloToolkitExtensions.Messaging;
using UnityEngine;
using Random = System.Random;
#if UNITY_WSA && !UNITY_EDITOR
using System.Threading.Tasks;
using Windows.Graphics.Imaging;
using Windows.Media;
#endif

public class ObjectRecognizer : MonoBehaviour
{
#if UNITY_WSA && !UNITY_EDITOR
    private ObjectDetection _objectDetection;
#endif

    private bool _isInitialized;

    private void Start()
    {
        Messenger.Instance.AddListener<PhotoCaptureMessage>(p=> RecognizeObjects(p.Image, p.CameraResolution, p.CameraTransform));
#if UNITY_WSA && !UNITY_EDITOR

        _objectDetection = new ObjectDetection(new[]{"Person"}, 20, 0.5f,0.3f );
        Debug.Log("Initializing...");
        _objectDetection.Init("ms-appx:///Data/StreamingAssets/model.onnx").ContinueWith(p =>
        {
            Debug.Log("Intializing ready");
            _isInitialized = true;
        });
#endif
    }

    public virtual void RecognizeObjects(IList<byte> image, 
                                         Resolution cameraResolution, 
                                         Transform cameraTransform)
    {
        if (_isInitialized)
        {
#if UNITY_WSA && !UNITY_EDITOR
            RecognizeObjectsAsync(image, cameraResolution, cameraTransform);
#endif

        }
    }

#if UNITY_WSA && !UNITY_EDITOR


    private async Task RecognizeObjectsAsync(IList<byte> image, Resolution cameraResolution, Transform cameraTransform)
    {
        //https://stackoverflow.com/questions/35070622/photo-capture-stream-to-softwarebitmap
        //https://blogs.msdn.microsoft.com/appconsult/2018/05/23/add-a-bit-of-machine-learning-to-your-windows-application-thanks-to-winml/
        using (var stream = new MemoryStream(image.ToArray()))
        {
            var decoder = await BitmapDecoder.CreateAsync(stream.AsRandomAccessStream());  

            var sfbmp = await decoder.GetSoftwareBitmapAsync();
            sfbmp = SoftwareBitmap.Convert(sfbmp, BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied);
    
            byte [] imageBytes = new byte[4*decoder.PixelWidth*decoder.PixelHeight];
            sfbmp.CopyToBuffer(imageBytes.AsBuffer());

            Debug.Log(imageBytes.Length + "imageBytes");
            var picture = VideoFrame.CreateWithSoftwareBitmap(sfbmp);
            var prediction = await _objectDetection.PredictImageAsync(picture);
            Debug.Log("acceptablePredications");
            var acceptablePredications = ((IList<PredictionModel>)prediction).Where(p => p.Probability >= 0.7).ToList();
            Debug.Log(" IList<Color> colors");

            //  ProcessPredictions(prediction, cameraResolution, cameraTransform);
            IList<Color[]> colors = new List<Color[]>();
            foreach (var predict in prediction)
            {
                Debug.Log("center");

                var center = new Vector2((float)(predict.BoundingBox.Left + (0.5 * predict.BoundingBox.Width)),
                    (float)(predict.BoundingBox.Top + (0.5 * predict.BoundingBox.Height)));
                var lefttop = new Vector2((float)(predict.BoundingBox.Left),
                    (float)(predict.BoundingBox.Top));
                var leftbot = new Vector2((float)(predict.BoundingBox.Left),
                    (float)(predict.BoundingBox.Top + (predict.BoundingBox.Height)));
                var righttop = new Vector2((float)(predict.BoundingBox.Left + (predict.BoundingBox.Width)),
                    (float)(predict.BoundingBox.Top));
                var rightbot = new Vector2((float)(predict.BoundingBox.Left + (predict.BoundingBox.Width)),
                    (float)(predict.BoundingBox.Top + (predict.BoundingBox.Height)));

                var left_top_num = get4pixfrompercentage(lefttop, cameraResolution);
                var left_bot_num = get4pixfrompercentage(leftbot, cameraResolution);
                var right_bot_num = get4pixfrompercentage(rightbot, cameraResolution);
                var right_top_num = get4pixfrompercentage(righttop, cameraResolution);
                var center_num = get4pixfrompercentage(center, cameraResolution);
               


                Debug.Log(center);

                var byteArray = new byte[4];
                    Debug.Log(byteArray);
                 Debug.Log( 4 * (int)(center.x * cameraResolution.width + center.y * cameraResolution.height * cameraResolution.width));
                 Debug.Log( "w:"+ cameraResolution.width + "H:"+ cameraResolution.height );
                 Debug.Log( image.Count+"count");
                 Debug.Log(byteArray.Length+ "byteArray.Length");
                 Debug.Log(imageBytes[4 * (int)(center.x * cameraResolution.width + center.y * cameraResolution.height * cameraResolution.width)]);
                // int temp = 4 *(int)(center.x * cameraResolution.width + center.y * cameraResolution.height * cameraResolution.width);
                //int temp = 0;
                //            stream.Read(byteArray, 4 * (int)(center.x * cameraResolution.width + center.y * cameraResolution.height * cameraResolution.width), 4);
                PlayerPrefs.SetInt("mode", 2500);
                
                int mode = PlayerPrefs.GetInt("mode");
                switch (mode) {
                    case 0:
                        Color[] col0 = new Color[1];
                        col0[0] = Color.white;


                        colors.Add(col0);

                        break;

                    case 1:
                        Color[] col1 = new Color[1];
                        col1[0] = Color.white;


                        colors.Add(col1);

                        break;
    
                    case 4://four corners
                        Color[] col = new Color[4];
                        col[0] = getcolorfrompix(left_top_num, imageBytes);
                        col[1] = getcolorfrompix(right_top_num, imageBytes);
                        col[2] = getcolorfrompix(left_bot_num, imageBytes);
                        col[3] = getcolorfrompix(right_bot_num, imageBytes);
                        //col[0] = getcolorfrompix(0, imageBytes);
                        //col[1] = getcolorfrompix(4 * cameraResolution.width-4, imageBytes);
                        //col[2] = getcolorfrompix(4 * (cameraResolution.height * cameraResolution.width - cameraResolution.width), imageBytes);
                        //col[3] = getcolorfrompix(4 * (cameraResolution.height * cameraResolution.width-1), imageBytes);
                     Debug.Log("Col!!"+ col[0]+" "+col[1]+" "+col[2]+" "+col[3]);

                        colors.Add(col);

                        break;
                    case 16:
                        Color[] col16 = new Color[16];
                        col16[0] = getcolorfrompix(left_top_num, imageBytes);
                        col16[1] = getcolorfrompix(left_top_num + 4, imageBytes);
                        col16[2] = getcolorfrompix(left_top_num + 4 * 2, imageBytes);
                        col16[3] = getcolorfrompix(left_top_num + 4 * 3, imageBytes);
                        col16[4] = getcolorfrompix(left_top_num + 4 * cameraResolution.width , imageBytes);
                        col16[5] = getcolorfrompix(left_top_num + 4 * cameraResolution.width + 4, imageBytes);
                        col16[6] = getcolorfrompix(left_top_num + 4 * cameraResolution.width + 4*2, imageBytes);
                        col16[7] = getcolorfrompix(left_top_num + 4 * cameraResolution.width + 4*3, imageBytes);
                        col16[8] = getcolorfrompix(left_top_num + 4 * cameraResolution.width *2 , imageBytes);
                        col16[9] = getcolorfrompix(left_top_num + 4 * cameraResolution.width *2+ 4, imageBytes);
                        col16[10] = getcolorfrompix(left_top_num + 4 * cameraResolution.width *2+ 4*2, imageBytes);
                        col16[11] = getcolorfrompix(left_top_num + 4 * cameraResolution.width *2+ 4*3, imageBytes);
                        col16[12] = getcolorfrompix(left_top_num + 4 * cameraResolution.width *3 , imageBytes);
                        col16[13] = getcolorfrompix(left_top_num + 4 * cameraResolution.width *3+ 4, imageBytes);
                        col16[14] = getcolorfrompix(left_top_num + 4 * cameraResolution.width *3+ 4*2, imageBytes);
                        col16[15] = getcolorfrompix(left_top_num + 4 * cameraResolution.width *3+ 4*3, imageBytes);
                         Debug.Log("Col16!!"+ col16[0]+" "+col16[1]+" "+col16[2]+" "+col16[3]);
                         Debug.Log("Col16!!"+ col16[4]+" "+col16[5]+" "+col16[6]+" "+col16[7]);
                         Debug.Log("Col16!!"+ col16[8]+" "+col16[9]+" "+col16[10]+" "+col16[11]);

                        colors.Add(col16);


                        break;
                 case 400:
                        Color[] col400 = new Color[400];
                        for (var i = 0; i < 400; i++) {
                            col400[i] = getcolorfrompix(left_top_num+4 * cameraResolution.width * (int)(i / 20) + 4 * (i % 20), imageBytes);
                        }
                        
                        colors.Add(col400);


                        break;
                case 2500:
                        Color[] col2500 = new Color[2500];
                        for (var i = 0; i < 2500; i++) {
                            col2500[i] = getcolorfrompix(left_top_num+4 * cameraResolution.width * (int)(i / 50) + 4 * (i % 50), imageBytes);
                        }
                        
                        colors.Add(col2500);


                        break;
                case 50://divided bt 50
                        Random r = new Random();
                        
                        int pixs =(int) (predict.BoundingBox.Width * cameraResolution.width * predict.BoundingBox.Height * cameraResolution.height)/50;

                        int hpix = (int) (predict.BoundingBox.Width * cameraResolution.width);
                        int wpix = (int) (predict.BoundingBox.Height * cameraResolution.height);

                        Color[] col50 = new Color[pixs];
                        for (var i = 0; i < pixs; i++) {
                           
                            int h_diff = r.Next(0, (int)( Math.Min(predict.BoundingBox.Top * cameraResolution.height, (int)(hpix * 0.2))));
                            int w_diff = r.Next(0, 50);
                          //  Debug.Log("hdff"+h_diff);
                          //  Debug.Log("hdffww"+w_diff+"wpix"+wpix);
                            //Debug.Log("left_top_num" + left_top_num);
                            //Debug.Log("4 * cameraResolution.width *h_diff" + 4 * cameraResolution.width * h_diff);
                            //Debug.Log("4 * (i * 50)%wpix/50*w_diff" + 4 * (i * 50) % wpix / 50 * w_diff+"-----"+(left_top_num - 4 * cameraResolution.width * h_diff + 4 * (i * 50) % wpix / 50 * w_diff));

                            col50[i] = getcolorfrompix(left_top_num-4 * cameraResolution.width *h_diff + 4 * (i * 50)%wpix/50+w_diff, imageBytes);
                        }
                        Debug.Log("DONE");

                        
                        colors.Add(col50);


                        break;



                }

                //Debug.Log(imageBytes[temp]/255.0 + " " + imageBytes[temp+1]/255.0   + " " + imageBytes[temp+2]/255.0  +" "+imageBytes[temp+3]/255.0+ "!!!!!!!!!COLOR IS");
                //colors.Add(col);

            }
            
            Messenger.Instance.Broadcast(
         new ObjectRecognitionResultMessage(acceptablePredications, cameraResolution, cameraTransform, colors));
        }
    }

    private int get4pixfrompercentage(Vector2 center, Resolution cameraResolution)
    {
        return 4 * (int)(center.x * cameraResolution.width + center.y * cameraResolution.height * cameraResolution.width);
    }

    private Color getcolorfrompix(int temp, byte[] imageBytes) {
        return new Color(imageBytes[temp + 2] / 255.0f, imageBytes[temp + 0] / 255.0f, imageBytes[temp + 1] / 255.0f, imageBytes[temp + 3] / 255.0f);
    }
#endif

#if UNITY_WSA && !UNITY_EDITOR

    //private void ProcessPredictions(IList<PredictionModel>predictions, Resolution cameraResolution, Transform cameraTransform, IList<byte> image)
    //{
    //    var acceptablePredications = predictions.Where(p => p.Probability >= 0.7).ToList();
    //    var colors = new IList<Color>();
    //    foreach (var prediction in predictions)
    //    {
    //        var center = new Vector2((float)(prediction.BoundingBox.Left + (0.5 * prediction.BoundingBox.Width)),
    //            (float)(prediction.BoundingBox.Top + (0.5 * prediction.BoundingBox.Height)));
    //        colors.Add((getRBGColor(image, center.x, center.y, cameraResolution)));
    //    }
    //        Messenger.Instance.Broadcast(
    //     new ObjectRecognitionResultMessage(acceptablePredications, cameraResolution, cameraTransform, colors));
    //}
    
    //private Color getRBGColor(IList<byte> image, float x, float y, Resolution cameraResolution)
    //{
    //    x = x * cameraResolution.width;
    //    y = y * cameraResolution.height;

    //    float R = image[4 * (int)(x + y * cameraResolution.width)];
    //    float G = image[1 + 4 * (int)(x + y * cameraResolution.width)];
    //    float B = image[2 + 4 * (int)(x + y * cameraResolution.width)];
    //    float A = image[3 + 4 * (int)(x + y * cameraResolution.width)];


    //    Debug.Log(R+ " " + B + " " + G + " " + A +"!!!!!!!!!COLOR IS");
    //    return new Color(R, B, G, A);
    //}

#endif

}

