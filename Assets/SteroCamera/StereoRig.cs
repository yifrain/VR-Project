using UnityEngine;
using System;
using Apt.Unity.Projection;

public class StereoRig : MonoBehaviour
{
    public ProjectionPlane projectionPlane;
    public bool offAxisProjection = false;

    [Header("Stereo Settings")]
    [Range(0.04f, 2.0f)]
    public float ipd = 0.064f;          // 瞳距（单位：米）

    public Transform leftEye;
    public Transform rightEye;

    public Camera leftCamera;
    public Camera rightCamera;

    public bool useToeIn = true;        // 是否启用 toe-in

    public Transform convergencePoint;  // 会聚点（空物体）

    [Range(0.5f, 20f)]
    public float convergenceDistance = 5f;  // Main Camera 到会聚点的距离（局部 Z）
    public float convergenceAdjustSpeed = 3f; // 调整距离的速度（米/秒）

    [Header("Movement Settings")]
    public float moveSpeed = 5f;        // WASD 移动速度
    public float mouseSensitivity = 2f; // 鼠标灵敏度

    private float yaw;                  // 水平旋转角
    private float pitch;                // 垂直旋转角

    void Reset()
    {
        // 自动找子物体
        if (leftEye == null)
            leftEye = transform.Find("LeftEye");
        if (rightEye == null)
            rightEye = transform.Find("RightEye");

        if (convergencePoint == null)
        {
            Transform cp = transform.Find("ConvergencePoint");
            if (cp != null) convergencePoint = cp;
        }
    }

    void Start()
    {
        Debug.Log("StereoRig started.");

        // 初始化会聚点在本地 Z 轴上的位置
        convergencePoint.localPosition = new Vector3(0f, 0f, convergenceDistance);

    }

    void Update()
    {
        Debug.Log("Update: Handling off-axis projection.");
        ApplyOffAxisProjection(leftCamera);
        ApplyOffAxisProjection(rightCamera);
    }


    void LateUpdate()
    {
        Debug.Log("LateUpdate: Updating eye positions and orientations.");
        if (leftEye == null || rightEye == null) return;

        float halfIPD = ipd / 2f;

        // 1⃣ 左右眼在局部 x 方向平移
        leftEye.localPosition  = new Vector3(-halfIPD, 0f, 0f);
        rightEye.localPosition = new Vector3( halfIPD, 0f, 0f);
        Debug.Log($"LeftEye Position: {leftEye.localPosition}, RightEye Position: {rightEye.localPosition}");

        // 2⃣ 决定是否 toe-in
        if (useToeIn && convergencePoint != null)
        {
            leftEye.LookAt(convergencePoint.position);
            rightEye.LookAt(convergencePoint.position);
        }
        else
        {
            // 不 toe-in 时：左右眼方向与 Main Camera 保持平行
            leftEye.rotation = transform.rotation;
            rightEye.rotation = transform.rotation;
        }
    }

    private void ApplyOffAxisProjection(Camera camera)
{
    if (!offAxisProjection)
    {
        Debug.Log("Not offAxisProjection");
        camera.ResetProjectionMatrix();
        camera.ResetWorldToCameraMatrix();
        return;
    }

    if (!projectionPlane)
        throw new Exception("No projection plane set!");
        
    camera.projectionMatrix = GetProjectionMatrix(projectionPlane, camera);
        
   // Translation to eye position
    var planeWorldMatrix = projectionPlane.M;
    var relativePlaneRotationMatrix =Matrix4x4.Rotate(
       Quaternion.Inverse(transform.rotation) * projectionPlane.transform.rotation);
    var cameraTranslationMatrix = Matrix4x4.Translate(-camera.transform.position);
    camera.worldToCameraMatrix = planeWorldMatrix * relativePlaneRotationMatrix * cameraTranslationMatrix;
    Debug.Log("Applied off-axis projection matrix to camera.");
}

    private static Matrix4x4 GetProjectionMatrix(ProjectionPlane projectionPlane, Camera camera)
    {
        // Extract positions of the projection plane corners
        Vector3 pa = projectionPlane.BottomLeft;
        Vector3 pb = projectionPlane.BottomRight;
        Vector3 pc = projectionPlane.TopLeft;

        // Extract projection plane axes
        Vector3 vr = projectionPlane.DirRight;   // right direction on the projection plane
        Vector3 vu = projectionPlane.DirUp;      // up direction on the projection plane
        Vector3 vn = projectionPlane.DirNormal;  // normal direction of the projection plane

        float n = camera.nearClipPlane;
        float f = camera.farClipPlane;

        // Position of the eye / camera
        Vector3 pe = camera.transform.position;

        // Vectors from eye to three corners of the projection plane
        Vector3 va = pa - pe;
        Vector3 vb = pb - pe;
        Vector3 vc = pc - pe;

        // Distance from eye to plane along its normal
        float d = -Vector3.Dot(vn, va);

        // Compute the parameters for the off-axis frustum
        float l = Vector3.Dot(vr, va) * n / d;
        float r = Vector3.Dot(vr, vb) * n / d;

        float b = Vector3.Dot(vu, va) * n / d;
        float t = Vector3.Dot(vu, vc) * n / d;

        // Build the projection matrix
        return Matrix4x4.Frustum(l, r, b, t, n, f);
    }

}
