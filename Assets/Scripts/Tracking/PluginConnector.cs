using System.Runtime.InteropServices;

//This script acts as a bridge between Unity and the external tracking DLL, encapsulating all native function calls.
public static class PluginConnector
{
    private const string dllName = "tracking";

    [DllImport(dllName)] private static extern void startTracking(int numberOfPlayers, int numberOfBaseStations);
    [DllImport(dllName)] private static extern void stopTracking();
    [DllImport(dllName)] private static extern void updatePositions(int size, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] float[] array, bool invertX, bool invertZ, bool flipXZ);
    [DllImport(dllName)] private static extern int getNumberOfTrackers();
    [DllImport(dllName)] private static extern int getNumberOfBaseStations();

    public static void StartTracking(int numberOfPlayers, int numberOfBaseStations) => startTracking(numberOfPlayers, numberOfBaseStations);
    public static void StopTracking() => stopTracking();
    public static int GetNumberOfTrackers() => getNumberOfTrackers();
    public static int GetNumberOfBaseStations() => getNumberOfBaseStations();
    public static void UpdatePositions(float[] outputArray, bool invertX, bool invertZ, bool swapXZ) => updatePositions(outputArray.Length * sizeof(float), outputArray, invertX, invertZ, swapXZ);
}
