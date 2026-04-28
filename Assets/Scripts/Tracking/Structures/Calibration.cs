using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.IO;
using TMPro;
using UnityEngine;

//Data can only be modified by setters
public struct Calibration
{
    public Vector3 CalibrationCenter;  //Center of the scene
    public Vector3 CalibrationRealWorldSize;  //Scale comparing real world and 1, so final scale will be application size / scale
    public Quaternion CalibrationRotation; // Rotation diference in a quaternion

    public Calibration(Vector3 center, Vector3 realWorldSize, Quaternion matTransform)
    {
        CalibrationCenter = center;
        CalibrationRealWorldSize = realWorldSize;
        CalibrationRotation = matTransform;
    }

    public void Initialize()
    {
        CalibrationCenter = Vector3.zero;
        CalibrationRealWorldSize = Vector3.zero;
        CalibrationRotation = Quaternion.identity;
    }

    public void Clear()
    {
        CalibrationCenter = Vector3.zero;
        CalibrationRealWorldSize = Vector3.zero;
        CalibrationRotation = Quaternion.identity;
    }

    public void SetCalibrationCenter(Vector3 vec)
    {
        CalibrationCenter = vec;
    }

    public void SetCalibrationCenter(Vector3 vec, float yOffset)
    {
        CalibrationCenter = new Vector3(vec.x, vec.y + yOffset, vec.z);
    }

    public void SetCalibrationRealWorldSize(Vector3 realWorldSize)
    {
        CalibrationRealWorldSize = realWorldSize;
    }

    public void SetCalibrationRotation(Quaternion mat)
    {
        CalibrationRotation = mat;
    }

    public Vector3 GetCalibrationCenter()
    {
        return CalibrationCenter;
    }

    public Vector3 GetCalibrationRealWorldSize()
    {
        return CalibrationRealWorldSize;
    }

    public Quaternion GetCalibrationRotation()
    {
        return CalibrationRotation;
    }
}
