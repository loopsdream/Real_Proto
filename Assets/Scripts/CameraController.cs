using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("Camera Settings")]
    public float cameraMarginPercent = 0.1f;
    public float minCameraSize = 3f;
    public float maxCameraSize = 15f;
    
    [Header("Portrait Mode Settings")]
    public bool portraitMode = true;
    public float topUISpacePixels = 200f;
    public float bottomUISpacePixels = 150f;
    public float sideMarginPixels = 100f;
    
    private Camera mainCamera;
    private float pixelsToWorldUnit = 1f;
    private float screenAspectRatio = 1f;
    
    void Awake()
    {
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            Debug.LogError("Main Camera not found!");
        }
    }
    
    public void AdjustCameraForGrid(int gridWidth, int gridHeight, float cellSize)
    {
        if (mainCamera == null) return;
        
        CalculateScreenMetrics();
        float optimalSize = CalculateOptimalCameraSize(gridWidth, gridHeight, cellSize);
        ApplyCameraSize(optimalSize);
        
        Debug.Log($"Camera adjusted for grid {gridWidth}x{gridHeight}, cell size {cellSize}");
    }
    
    public void CenterCameraOnGrid(Vector3 gridWorldCenter)
    {
        if (mainCamera == null) return;
        
        Vector3 targetPosition = gridWorldCenter;
        
        if (portraitMode)
        {
            float topUISpace = topUISpacePixels * pixelsToWorldUnit;
            float bottomUISpace = bottomUISpacePixels * pixelsToWorldUnit;
            float yOffset = -(topUISpace - bottomUISpace) * 0.5f;
            targetPosition.y += yOffset;
        }
        
        targetPosition.z = mainCamera.transform.position.z;
        mainCamera.transform.position = targetPosition;
        
        Debug.Log($"Camera centered at: {targetPosition}");
    }
    
    private void CalculateScreenMetrics()
    {
        if (mainCamera == null) return;
        
        screenAspectRatio = (float)Screen.width / Screen.height;
        float worldHeight = mainCamera.orthographicSize * 2f;
        pixelsToWorldUnit = worldHeight / Screen.height;
        
        Debug.Log($"Screen: {Screen.width}x{Screen.height}, Aspect: {screenAspectRatio:F2}");
    }
    
    private float CalculateOptimalCameraSize(int gridWidth, int gridHeight, float cellSize)
    {
        if (!portraitMode)
        {
            return Mathf.Max((gridHeight * cellSize) * 0.6f, (gridWidth * cellSize) / screenAspectRatio * 0.6f);
        }
        
        float gridWorldWidth = gridWidth * cellSize + sideMarginPixels * pixelsToWorldUnit * 2f;
        float gridWorldHeight = gridHeight * cellSize;
        
        float topUISpace = topUISpacePixels * pixelsToWorldUnit;
        float bottomUISpace = bottomUISpacePixels * pixelsToWorldUnit;
        
        float cameraHeightFromHeight = (gridWorldHeight + topUISpace + bottomUISpace) * 0.5f;
        float cameraHeightFromWidth = gridWorldWidth / screenAspectRatio * 0.5f;
        
        float requiredCameraSize = Mathf.Max(cameraHeightFromHeight, cameraHeightFromWidth);
        requiredCameraSize = Mathf.Clamp(requiredCameraSize, minCameraSize, maxCameraSize);
        requiredCameraSize *= (1f + cameraMarginPercent);
        
        return requiredCameraSize;
    }
    
    private void ApplyCameraSize(float cameraSize)
    {
        if (mainCamera != null)
        {
            mainCamera.orthographicSize = cameraSize;
            Debug.Log($"Camera size set to: {cameraSize}");
        }
    }
    
    public bool ValidateCameraPosition(Vector3 expectedCenter)
    {
        if (mainCamera == null) return false;
        
        Vector3 screenCenter = new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 10f);
        Vector3 worldCenter = mainCamera.ScreenToWorldPoint(screenCenter);
        
        float distance = Vector2.Distance(
            new Vector2(worldCenter.x, worldCenter.y), 
            new Vector2(expectedCenter.x, expectedCenter.y)
        );
        
        bool isValid = distance < 1f;
        
        if (!isValid)
        {
            Debug.LogWarning($"Camera position validation failed. Distance: {distance:F2}");
        }
        
        return isValid;
    }
    
    public void ForceCenterCamera()
    {
        if (mainCamera == null) return;
        
        Vector3 fixedPosition = Vector3.zero;
        
        if (portraitMode)
        {
            float topUISpace = topUISpacePixels * pixelsToWorldUnit;
            float bottomUISpace = bottomUISpacePixels * pixelsToWorldUnit;
            float yOffset = -(topUISpace - bottomUISpace) * 0.5f;
            fixedPosition.y = yOffset;
        }
        
        fixedPosition.z = mainCamera.transform.position.z;
        mainCamera.transform.position = fixedPosition;
        
        Debug.Log($"Camera force-centered to: {fixedPosition}");
    }
}