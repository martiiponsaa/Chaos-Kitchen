using UnityEngine;

public class DoubleScreenCameraManager : MonoBehaviour
{

    public GameObject plane;
    public GameObject blend;
    public Color backgroundColor;

    [Range(0, 100)]
    public float percentageOfCameraOverlap;

    void Awake()
    {
        // Activar displays (proyectores)
        var cameras = Object.FindObjectsByType<Camera>(FindObjectsSortMode.None);

        for (int i = 0; i < cameras.Length; i++)
        {
            if (i < Display.displays.Length)
            {
                Display.displays[i].Activate();
            }
        }

        // Colocar cámara y blend
        correctCameraAndBlendPos();

        Camera cam1 = transform.GetChild(0).GetComponent<Camera>();
        Camera cam2 = transform.GetChild(1).GetComponent<Camera>();

        // Ajustar tamaño del solape
        correctBlendScale();

        // Dirección de mirada
        Vector3 lookDirection = cameraLookDirectionVector(cam1);

        // Calcular desplazamiento vertical
        float yourShiftY = GetViewMatrixShiftY(cam1, lookDirection);

        // Ajustar aspect ratio
        float originalAspect = 9f / 16f;
        float targetAspect = getTargetAspect();
        float scaleFactor = targetAspect / originalAspect;
        yourShiftY /= scaleFactor;

        // Modificar la matriz de proyección 
        Matrix4x4 projectionMatrix = cam1.projectionMatrix;
        projectionMatrix.m11 /= scaleFactor;
        projectionMatrix.m12 += yourShiftY;

        // Aplicar a ambas cámaras
        cam1.projectionMatrix = projectionMatrix;
        cam2.projectionMatrix = projectionMatrix;

        // Color de fondo
        transform.GetChild(0).GetComponent<Camera>().backgroundColor = backgroundColor;
        transform.GetChild(1).GetComponent<Camera>().backgroundColor = backgroundColor;
  
    }

    // Modifica el scale z del blend para ajustarlo según el overlap de los proyectores
    private void correctBlendScale()
    {
        Vector3 camPos = this.transform.position;
        Vector3 blendPos = blend.transform.position;
        Vector3 planePos = plane.transform.position;

        float cameraBlendRatio = Vector3.Distance(camPos, blendPos) / Vector3.Distance(camPos, planePos);

        float blendScale = (percentageOfCameraOverlap / 10f) * cameraBlendRatio;

        Vector3 newScale = blend.transform.localScale;
        newScale.z = blendScale;
        blend.transform.localScale = newScale;
    }

    // Cámara: Se coloca a una distancia proporcional al tamaño del plano
    // Blend: Se coloca exactamente en medio entre cámara y plano
    private void correctCameraAndBlendPos()
    {
        Vector3 planePos = plane.transform.position;
        Vector3 planeScale = plane.transform.lossyScale;

        this.transform.position = new Vector3(0f, planeScale.x * 13.9f, 0f) + planePos;
        blend.transform.position = new Vector3(0f, ((this.transform.position.y - planePos.y) / 2.0f), 0f) + planePos;
    }

    // Calcula qué proporción de imagen necesita la cámara para encajar correctamente la zona visible + el solape
    private float getTargetAspect()
    {
        Vector3 planeScale = plane.transform.lossyScale * 10;
        Vector3 bottomCameraVision = new Vector3(0f, 0f, -(planeScale.z / 2));
        Vector3 topCameraVision = new Vector3(0f, 0f, (planeScale.z / 2) * (percentageOfCameraOverlap / 100));
        Vector3 leftCameraVision = new Vector3(-(planeScale.x / 2), 0f, 0f);
        Vector3 rightCameraVision = new Vector3(planeScale.x / 2, 0f, 0f);

        float scale = Vector3.Distance(topCameraVision, bottomCameraVision) / Vector3.Distance(leftCameraVision, rightCameraVision);

        return scale;
    }

    // Quiero que la cámara mire exactamente al centro de la zona útil del plano, teniendo en cuenta el solape entre proyectores
    private Vector3 cameraLookDirectionVector(Camera cam)
    {
        Vector3 planePos = plane.transform.position;
        Vector3 planeScale = plane.transform.lossyScale * 10;
        Vector3 bottomCameraVision = new Vector3(0f, 0f, -(planeScale.z / 2)) + planePos;
        Vector3 topCameraVision = new Vector3(0f, 0f, (planeScale.z / 2) * (percentageOfCameraOverlap / 100)) + planePos;

        Vector3 middleCameraVision = (topCameraVision + bottomCameraVision) / 2;

        Vector3 lookDirection = cameraToPointDirection(cam, middleCameraVision);

        return lookDirection;
    }

    private Vector3 cameraToPointDirection(Camera cam, Vector3 posToLook)
    {
        Vector3 direction = posToLook - cam.transform.position;
        direction.Normalize();

        return direction;
    }

    // ¿Cuánto tengo que subir o bajar la imagen de la cámara para que apunte exactamente donde quiero?
    private float GetViewMatrixShiftY(Camera cam, Vector3 lookDirection)
    {
        Vector3 pointInWorldSpace = cam.transform.position + lookDirection;

        Vector3 viewportPoint = cam.WorldToViewportPoint(pointInWorldSpace);

        float normalizedY = (viewportPoint.y * 2f) - 1f;

        return normalizedY;
    }
}
