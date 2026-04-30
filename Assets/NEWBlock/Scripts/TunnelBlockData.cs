using UnityEngine;
public enum TunnelExit { Left, Right, Top, Bottom }

[System.Serializable]
public class TunnelBlockData
{
    public TunnelExit entryPoint;
    public TunnelExit exitPoint;
    public Transform[] waypoints; // alien follows these
}