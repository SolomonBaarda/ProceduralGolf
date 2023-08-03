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

    [Header("Main Menu Camera")]
    public Transform RotatingPosition;
    public Transform RotatingLookAt;
    public float RotationSpeed = 0.5f;

    [Header("Course Preview Camera")]
    public CinemachineSmoothPath CoursePreviewDollyPath;
    public CinemachineDollyCart CoursePreviewDollyCart;
    public AnimationCurve CoursePreviewSpeedCurve = new AnimationCurve();
    public CinemachineVirtualCamera CoursePreviewCamera;

    public const float CoursePreviewDurationSeconds = 10.0f;
    public const float PercentLookingAtHoleCoursePreview = 0.1f;



    //public Camera MapCamera;

    private const string CameraSqrMagToTargetFloat = "SqrMagToTarget";
    private const string CameraAiming = "IsAiming", CameraRolling = "IsRolling", CameraFlying = "IsFlying";
    private const string OnMainMenuTrigger = "OnMainMenu", OnCoursePreviewTrigger = "OnCoursePreviewStart", OnGameStartTrigger = "OnGameStart";


    public const float DefaultMapCameraZoom = 500;


    public void Reset()
    {
        
    }

    public void SetVisible(bool visible)
    {
        CamerasParent.SetActive(visible);
    }

    private enum State
    {
        SpinningCamera = 0,
        CoursePreviewCamera = 1,
        GameCameras = 2
    }

    public void SetGolfBallSquareMagnitudeToTarget(float value)
    {
        CameraStates.SetFloat(CameraSqrMagToTargetFloat, value);
    }

    private void Update()
    {
        // Always update all cameras so that they are in position to cut to

        RotatingPosition.Rotate(Vector3.up, RotationSpeed * Time.deltaTime);

        CameraStates.SetBool(CameraAiming, TerrainManager.GolfBall.State == GolfBall.PlayState.Aiming);
        CameraStates.SetBool(CameraFlying, TerrainManager.GolfBall.State == GolfBall.PlayState.Flying);
        CameraStates.SetBool(CameraRolling, TerrainManager.GolfBall.State == GolfBall.PlayState.Rolling);
    }

    public void StartMainMenu()
    {
        CameraStates.SetTrigger(OnMainMenuTrigger);
    }

    public void StartGameCameras()
    {
        CameraStates.SetTrigger(OnGameStartTrigger);
    }

    public delegate void OnCoursePreviewCompleted();

    public void StartCoursePreview(CinemachineSmoothPath.Waypoint[] path, OnCoursePreviewCompleted callback)
    {
        // Update map camera
        /*
        Vector3 pos = (data.Start + data.Hole) / 2;
        pos.y = MapCamera.transform.position.y;
        MapCamera.transform.position = pos;
        MapCamera.orthographicSize = DefaultMapCameraZoom;
        MapCamera.enabled = false;
        */

        StartCoroutine(DoCoursePreview(path, callback));
    }

    private IEnumerator DoCoursePreview(CinemachineSmoothPath.Waypoint[] path, OnCoursePreviewCompleted callback)
    {
        CoursePreviewDollyPath.m_Waypoints = path;
        CoursePreviewDollyCart.m_PositionUnits = CinemachinePathBase.PositionUnits.Normalized;

        CameraStates.SetTrigger(OnCoursePreviewTrigger);

        // TODO fix when cinemachine gets updated
        // DUMB FIX FOR BROKEN PATHS
        CoursePreviewDollyPath.m_Resolution++;
        yield return null;
        CoursePreviewDollyPath.m_Resolution--;


        for (float totalTime = 0; totalTime < CoursePreviewDurationSeconds; totalTime += Time.deltaTime)
        {
            float t = CoursePreviewSpeedCurve.Evaluate(totalTime / CoursePreviewDurationSeconds);
            CoursePreviewDollyCart.m_Position = t;

            CoursePreviewCamera.LookAt = t < PercentLookingAtHoleCoursePreview ? TerrainManager.NextHoleFlag.transform : TerrainManager.GolfBall.transform;

            yield return null;
        }

        callback();
    }





}
