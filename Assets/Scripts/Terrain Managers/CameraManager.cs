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

    [Header("States")]
    public Animator MainStates;
    public Animator InGameStates;

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

    private const string OnMainMenuTrigger = "OnMainMenu", OnCoursePreviewTrigger = "OnCoursePreviewStart", OnGameStartTrigger = "OnGameStart";


    public void Reset()
    {

    }

    public void SetVisible(bool visible)
    {
        CamerasParent.SetActive(visible);
    }

    public void SetGolfBallSquareMagnitudeToAimingPosition(float value)
    {
        InGameStates.SetFloat("BallSqrMagToAimingPosition", value);
    }

    public void SetGolfBallYVelocity(float value)
    {
        InGameStates.SetFloat("BallYVelocity", value);
    }

    public void SetShotPeakHeightFromGround(float value)
    {
        InGameStates.SetFloat("ShotPeakHeightFromGround", value);
    }

    private void Update()
    {
        // Always update all cameras so that they are in position to cut to

        // Main menu
        RotatingPosition.Rotate(Vector3.up, RotationSpeed * Time.deltaTime);

        // Golf Ball
        InGameStates.SetBool("IsAiming", TerrainManager.GolfBall.State == GolfBall.PlayState.Aiming);
        InGameStates.SetBool("IsOnGround", TerrainManager.GolfBall.IsOnGround);
    }

    public void StartMainMenu()
    {
        MainStates.SetTrigger(OnMainMenuTrigger);
    }

    public void StartGameCameras()
    {
        MainStates.SetTrigger(OnGameStartTrigger);
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

        MainStates.SetTrigger(OnCoursePreviewTrigger);

        // TODO fix when cinemachine gets updated
        // DUMB FIX FOR BROKEN PATHS
        CoursePreviewDollyPath.m_Resolution++;
        yield return null;
        CoursePreviewDollyPath.m_Resolution--;


        for (float totalTime = 0; totalTime < CoursePreviewDurationSeconds; totalTime += Time.deltaTime)
        {
            float t = CoursePreviewSpeedCurve.Evaluate(totalTime / CoursePreviewDurationSeconds);
            CoursePreviewDollyCart.m_Position = t;

            CoursePreviewCamera.LookAt = t < PercentLookingAtHoleCoursePreview ? TerrainManager.NextHolePosition.transform : TerrainManager.GolfBall.transform;

            yield return null;
        }

        callback();
    }





}
