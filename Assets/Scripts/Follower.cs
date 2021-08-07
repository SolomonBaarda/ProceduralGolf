using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Follower : MonoBehaviour
{
    public Transform Target;
    public Vector3 OffsetFromTarget;

    public bool DoOffsetFromTargetFacingDirection = true;
    public float FacingDirectionDistance = 0;
    public bool SetRotation = false;

    private void Update()
    {
        Vector3 offset = OffsetFromTarget;

        if(DoOffsetFromTargetFacingDirection)
        {
            offset += -Target.transform.forward * FacingDirectionDistance;
        }

        transform.position = Target.transform.position + offset;

        if (SetRotation)
        {
            transform.forward = Target.transform.forward;

            Vector3 newRotation = transform.rotation.eulerAngles;
            newRotation.x = 0;
            newRotation.z = 0;
            transform.rotation = Quaternion.Euler(newRotation);
        }
    }
}
