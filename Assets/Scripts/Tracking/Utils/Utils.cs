using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.IO;
using TMPro;
using UnityEngine;
using Newtonsoft.Json;

public static class Utils
{
    // ==== Start: To string utilities Block ====

    // Returns a string from a given vector3 (used for display in the interface)
    public static string Vector3ToString(Vector3 vector)
    {
        // Show only 2 decimal points
        float x = Mathf.Round(vector.x * 100f) / 100f;
        float y = Mathf.Round(vector.y * 100f) / 100f;
        float z = Mathf.Round(vector.z * 100f) / 100f;
        return $"({x} , {y} , {z})";
    }

    public static string QuaternionToString(Quaternion q)
    {
        //// Show only 2 decimal points
        float x = Mathf.Round(q.x * 100f) / 100f;
        float y = Mathf.Round(q.y * 100f) / 100f;
        float z = Mathf.Round(q.z * 100f) / 100f;
        float w = Mathf.Round(q.w * 100f) / 100f;
        return $"({x} , {y} , {z}, {w})";

        // Convert quaternion to Euler angles
        //Vector3 eulerAngles = q.eulerAngles;

        //// Show only 2 decimal points
        //float x = Mathf.Round(eulerAngles.x * 100f) / 100f;
        //float y = Mathf.Round(eulerAngles.y * 100f) / 100f;
        //float z = Mathf.Round(eulerAngles.z * 100f) / 100f;

        // Format as a string
        //return $"({x}, {y}, {z})";
    }

    // ==== Ends: To string utilities Block ====
}
