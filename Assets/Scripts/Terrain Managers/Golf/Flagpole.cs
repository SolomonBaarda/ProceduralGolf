using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Flagpole : MonoBehaviour
{
    public Transform GolfBall;
    public float DistanceToStartRaisingPole = 5.0f;
    public float DistanceToRaisePole = 1.0f;

    public Vector3 Position = Vector3.zero;

    // Update is called once per frame
    void Update()
    {
        float distanceToGolfBallSqrMag = (GolfBall.position - Position).sqrMagnitude;
        float t = distanceToGolfBallSqrMag / (DistanceToStartRaisingPole * DistanceToStartRaisingPole);
        float offset = Mathf.Lerp(DistanceToRaisePole, 0, Mathf.Clamp01(t));

        transform.position = Position + Vector3.up * offset;
    }
}
