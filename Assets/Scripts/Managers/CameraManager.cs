using Cinemachine;
using System.Collections;
using UnityEngine;

public class CameraManager : MonoBehaviour, IManager
{
    public GameObject CamerasParent;
    public TerrainManager TerrainManager;

    [Space]
    public CinemachineBrain MainCameraBrain;
    public Camera MainCamera => MainCameraBrain.OutputCamera;
    public Animator CameraStates;

    [Header("Course Preview Camera")]
    public CinemachineSmoothPath CoursePreviewDollyPath;
    public CinemachineDollyCart CoursePreviewDollyCart;
    public AnimationCurve CoursePreviewSpeedCurve = new AnimationCurve();
    public CinemachineVirtualCamera CoursePreviewCamera;


    //public Camera MapCamera;


    private const string CameraSqrMagToTargetFloat = "SqrMagToTarget";
    private const string CameraAiming = "IsAiming", CameraRolling = "IsRolling", CameraFlying = "IsFlying", CameraCoursePreview = "IsCoursePreview";


    public const float DefaultMapCameraZoom = 500;

    public const float CoursePreviewDurationSeconds = 10.0f;
    public const float PercentLookingAtHoleCoursePreview = 0.1f;

    public void Reset()
    {
        
    }

    public void SetVisible(bool visible)
    {
        CamerasParent.SetActive(visible);
    }

    public void SetGolfBallSquareMagnitudeToTarget(float value)
    {
        CameraStates.SetFloat(CameraSqrMagToTargetFloat, value);
    }

    private void Update()
    {
        CameraStates.SetBool(CameraAiming, TerrainManager.GolfBall.State == GolfBall.PlayState.Aiming);
        CameraStates.SetBool(CameraFlying, TerrainManager.GolfBall.State == GolfBall.PlayState.Flying);
        CameraStates.SetBool(CameraRolling, TerrainManager.GolfBall.State == GolfBall.PlayState.Rolling);
    }

    public void StartCoursePreview(CinemachineSmoothPath.Waypoint[] path)
    {
        CoursePreviewDollyPath.m_Waypoints = path;

        // Update map camera
        /*
        Vector3 pos = (data.Start + data.Hole) / 2;
        pos.y = MapCamera.transform.position.y;
        MapCamera.transform.position = pos;
        MapCamera.orthographicSize = DefaultMapCameraZoom;
        MapCamera.enabled = false;
        */

        StartCoroutine(DoCoursePreview(path));
    }

    private IEnumerator DoCoursePreview(CinemachineSmoothPath.Waypoint[] path)
    {
        CameraStates.SetBool(CameraCoursePreview, true);

        CoursePreviewDollyCart.m_PositionUnits = CinemachinePathBase.PositionUnits.Normalized;


        // TODO fix when cinemachine gets updated
        // DUMB FIX FOR BROKEN PATHS
        CoursePreviewDollyPath.m_Resolution--;
        yield return null;
        CoursePreviewDollyPath.m_Resolution++;


        for (float totalTime = 0; totalTime < CoursePreviewDurationSeconds; totalTime += Time.deltaTime)
        {
            float t = CoursePreviewSpeedCurve.Evaluate(totalTime / CoursePreviewDurationSeconds);
            CoursePreviewDollyCart.m_Position = t;

            CoursePreviewCamera.LookAt = t < PercentLookingAtHoleCoursePreview ? TerrainManager.NextHoleFlag.transform : TerrainManager.GolfBall.transform;

            yield return null;
        }

        CameraStates.SetBool(CameraCoursePreview, false);
    }




}
