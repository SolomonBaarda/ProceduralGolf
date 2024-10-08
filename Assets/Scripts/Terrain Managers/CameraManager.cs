using Cinemachine;
using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;

public class CameraManager : MonoBehaviour, IManager
{
    public GameObject CamerasParent;
    public TerrainManager TerrainManager;

    [Space]
    public CinemachineBrain MainCameraBrain;
    public Volume MainCameraPostProcessing;
    public Camera MainCamera => MainCameraBrain.OutputCamera;

    [Header("States")]
    public Animator MainStates;
    public Animator InGameStates;
    public Animator CoursePreviewStates;

    [Header("Main Menu Camera")]
    public Transform RotatingPosition;
    public Transform RotatingLookAt;
    public float RotationSpeed = 0.5f;

    [Header("Course Preview Camera")]
    public CinemachineSmoothPath CoursePreviewDollyPath;
    public CinemachineDollyCart CoursePreviewDollyCart;
    public AnimationCurve CoursePreviewSpeedCurve = new AnimationCurve();
    public CinemachineTargetGroup CoursePreviewTargetGroup;

    public const float CoursePreviewStartSeconds = 1.0f;
    public const float CoursePreviewMainSeconds = 7.0f;
    public const float CoursePreviewEndSeconds = 2.0f;

    [Header("Course End Camera")]
    public CinemachineDollyCart CourseEndDollyCart;

    private const string OnMainMenuTrigger = "OnMainMenu", OnCoursePreviewTrigger = "OnCoursePreviewStart", OnGameStartTrigger = "OnGameStart", OnCourseEndTrigger = "OnCourseEndPreviewStart";


    public void Reset()
    {

    }

    public void SetVisible(bool visible)
    {
        CamerasParent.SetActive(visible);
    }

    public void SetPostProcessing(VolumeProfile profile)
    {
        MainCameraPostProcessing.sharedProfile = profile;
        MainCameraPostProcessing.enabled = profile != null;
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
        StartCoroutine(DoCoursePreview(path, callback));
    }

    private IEnumerator DoCoursePreview(CinemachineSmoothPath.Waypoint[] path, OnCoursePreviewCompleted callback)
    {
        CoursePreviewDollyPath.m_Waypoints = path;
        CoursePreviewDollyCart.m_PositionUnits = CinemachinePathBase.PositionUnits.Normalized;

        MainStates.SetTrigger(OnCoursePreviewTrigger);

        // Stationary looking at hole
        CoursePreviewTargetGroup.m_Targets = new CinemachineTargetGroup.Target[]
        {
            new CinemachineTargetGroup.Target() { target = TerrainManager.NextHolePosition, weight = 1, radius = 0 }
        };
        CoursePreviewDollyCart.m_Position = 0.0f;
        CoursePreviewStates.SetTrigger("OnStartWatchingHole");

        // TODO fix when cinemachine gets updated
        // DUMB FIX FOR BROKEN PATHS
        CoursePreviewDollyPath.m_Resolution++;
        yield return null;
        CoursePreviewDollyPath.m_Resolution--;

        yield return new WaitForSeconds(CoursePreviewStartSeconds);


        // Doing flyby
        CoursePreviewTargetGroup.m_Targets = new CinemachineTargetGroup.Target[]
        {
            new CinemachineTargetGroup.Target() { target = TerrainManager.GolfBall.transform, weight = 1, radius = 0 }
        };
        CoursePreviewStates.SetTrigger("OnStartMoving");

        for (float totalTime = 0; totalTime < CoursePreviewMainSeconds; totalTime += Time.deltaTime)
        {
            CoursePreviewDollyCart.m_Position = CoursePreviewSpeedCurve.Evaluate(totalTime / CoursePreviewMainSeconds);
            yield return null;
        }

        // Looking at ball
        CoursePreviewTargetGroup.m_Targets = new CinemachineTargetGroup.Target[]
        {
            new CinemachineTargetGroup.Target() { target = TerrainManager.GolfBall.transform, weight = 1, radius = 0 }
        };
        CoursePreviewDollyCart.m_Position = 1.0f;
        CoursePreviewStates.SetTrigger("OnStartWatchingBall");
        yield return new WaitForSeconds(CoursePreviewEndSeconds);


        callback();
    }

    public void DoCourseEndPreview(float startSeconds, float spinSeconds, float endSeconds)
    {
        StartCoroutine(DoCourseEnd(startSeconds, spinSeconds, endSeconds));
    }

    private IEnumerator DoCourseEnd(float start, float seconds, float end)
    {
        MainStates.SetTrigger(OnCourseEndTrigger);

        CourseEndDollyCart.m_Position = 0;
        yield return new WaitForSeconds(start);

        for (float totalTime = 0; totalTime < seconds; totalTime += Time.deltaTime)
        {
            CourseEndDollyCart.m_Position = totalTime / seconds;
            yield return null;
        }

        CourseEndDollyCart.m_Position = 1;
        yield return new WaitForSeconds(end);
    }



}
