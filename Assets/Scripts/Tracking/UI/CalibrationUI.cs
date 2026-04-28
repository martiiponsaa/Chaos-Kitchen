using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System;

public class CalibrationUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI playerNumber;
    [SerializeField] private TextMeshProUGUI baseStationNumber;

    [SerializeField] private TextMeshProUGUI calibrationFileStatus;

    [SerializeField] private TextMeshProUGUI center;
    [SerializeField] private TextMeshProUGUI physicalWorldSize;
    [SerializeField] private TextMeshProUGUI rotationOffset;

    [SerializeField] private List<TextMeshProUGUI> playersPositions;
    [SerializeField] private List<TextMeshProUGUI> playersRotations;

    private void Awake()
    {
        center.text = "Uncalibrated";
        physicalWorldSize.text = "Uncalibrated";
        rotationOffset.text = "Uncalibrated";
    }

    public void SetNumberOfPlayers(string numPlayers) { playerNumber.text = numPlayers; }
    public void SetNumberOfPlayers(int numPlayers) { playerNumber.text = numPlayers.ToString(); }

    public void SetNumberOfBaseStations(string numBaseStations) { baseStationNumber.text = numBaseStations; }
    public void SetNumberOfBaseStations(int numBaseStations) { baseStationNumber.text = numBaseStations.ToString(); }

    public void SetPlayerXPos(int x, Vector3 pos)
    {
        if (playersPositions.Count > 0)
            SetPlayerPos(playersPositions[x], pos);
    }
    public void SetPlayerPos(TextMeshProUGUI text, Vector3 pos) { text.text = Utils.Vector3ToString(pos); }

    public void SetPlayerXRot(int x, Quaternion rot)
    {
        if (playersRotations.Count > 0)
            SetPlayerRot(playersRotations[x], rot);
    }
    public void SetPlayerRot(TextMeshProUGUI text, Quaternion rot) { text.text = Utils.QuaternionToString(rot); }

    public void SetCalibrationFileStatus(string fileStatus) { calibrationFileStatus.text = fileStatus; }

    public void SetCenter(Vector3 c) { center.text = Utils.Vector3ToString(c); }
    public void SetPhysicalWorldSize(Vector3 size) { physicalWorldSize.text = Utils.Vector3ToString(size); }
    public void SetRotationOffset(Quaternion RotOff) { rotationOffset.text = Utils.QuaternionToString(RotOff); }

    internal void SetNumberOfBaseStations(object p)
    {
        throw new NotImplementedException();
    }
}
