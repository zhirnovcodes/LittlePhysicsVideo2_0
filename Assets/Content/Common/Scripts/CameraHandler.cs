using UnityEngine;

public struct CameraHandlerData
{
    public Vector3 Position;
    public Quaternion Rotation;
    public float ZoomPosition;
    public float CameraOrthoSize;

    public CameraHandlerData Lerp(CameraHandlerData target, float t)
    {
        return new CameraHandlerData
        {
            Position = Vector3.Lerp(Position, target.Position, t),
            Rotation = Quaternion.Slerp(Rotation, target.Rotation, t),
            ZoomPosition = Mathf.Lerp(ZoomPosition, target.ZoomPosition, t),
            CameraOrthoSize = Mathf.Lerp(CameraOrthoSize, target.CameraOrthoSize, t),
        };
    }
}

public class CameraHandler : MonoBehaviour
{
    public Transform CameraPositionHandler;
    public Transform CameraRotationHandler;
    public Transform CameraZoomHandler;
    public Camera Camera;

    public CameraHandlerData GetData()
    {
        return new CameraHandlerData
        {
            Position = CameraPositionHandler.localPosition,
            Rotation = CameraRotationHandler.localRotation,
            ZoomPosition = CameraZoomHandler.localPosition.z,
            CameraOrthoSize = Camera.orthographicSize,
        };
    }

    public void SetData(CameraHandlerData data)
    {
        CameraPositionHandler.localPosition = data.Position;
        CameraRotationHandler.localRotation = data.Rotation;

        Vector3 zoomLocalPosition = CameraZoomHandler.localPosition;
        zoomLocalPosition.z = data.ZoomPosition;
        CameraZoomHandler.localPosition = zoomLocalPosition;

        Camera.orthographicSize = data.CameraOrthoSize;
    }
}
