
using System.Collections.Generic;
using CustomVision;
using UnityEngine;

public class ObjectRecognitionResultMessage : ObjectRecognitionMessageBase
{
    public IList<PredictionModel> Predictions { get; protected set; }
    public IList<Color[]> Colors { get; internal set; }

    public ObjectRecognitionResultMessage(IList<PredictionModel> predictions, 
        Resolution cameraResolution, Transform cameraTransform, IList<Color[]> colors ) : 
        base( cameraResolution, cameraTransform)
    {
        Predictions = predictions;
        Colors = colors;
       
    }
}
