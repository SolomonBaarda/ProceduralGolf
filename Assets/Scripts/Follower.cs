using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Follower : MonoBehaviour
{
    public Transform Target;
    public Vector3 OffsetFromTarget;

    private void Update()
    {
        transform.position = Target.transform.position + OffsetFromTarget;
    }
}
