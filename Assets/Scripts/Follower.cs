using UnityEngine;

public class Follower : MonoBehaviour
{
    public static readonly Vector3 Up = Vector3.up;

    public Transform Following;
    private ICanBeFollowed target;

    public View CurrentView;
    private ViewPreset view;

    public const float SecondsToReachTarget = 0.5f;

    // Views
    public static readonly ViewPreset ViewPresetBehind = new ViewPreset(true, true, true, false, false, 0, 1f, 0.25f, 0f, Vector3.zero, Vector3.zero);
    public static readonly ViewPreset ViewPresetAbove = new ViewPreset(true, true, true, true, true, 5f, 1.5f, 1f, 0f, Vector3.zero, Vector3.zero);
    /// <summary>
    /// View for setting the shot rotation.
    /// </summary>
    public static readonly ViewPreset ViewPresetShootingAbove = new ViewPreset(true, false, true, true, true, 10, 3f, 2f, 0f, new Vector3(0, 1, 0), Vector3.zero);
    /// <summary>
    /// View for setting the shot angle.
    /// </summary>
    public static readonly ViewPreset ViewPresetShootingLeft = new ViewPreset(true, false, true, true, true, 7.5f, 0f, 0.5f, 3f, new Vector3(0, 0.5f, 0), Vector3.zero);
    /// <summary>
    /// View for setting the shot power.
    /// </summary>
    public static readonly ViewPreset ViewPresetShootingBehind = new ViewPreset(true, false, true, true, true, 7.5f, 2.5f, 2f, 1f, new Vector3(0, 2, 0), Vector3.zero);



    private void Update()
    {
        // Validate if the target object is null
        if (target == null)
        {
            OnValidate();
        }

        if (Following != null && target != null)
        {
            // Update the current view preset values
            switch (CurrentView)
            {
                case View.Behind:
                    view = ViewPresetBehind;
                    break;
                case View.Above:
                    view = ViewPresetAbove;
                    break;
                case View.ShootingAbove:
                    view = ViewPresetShootingAbove;
                    break;
                case View.ShootingBehind:
                    view = ViewPresetShootingBehind;
                    break;
                case View.ShootingLeft:
                    view = ViewPresetShootingLeft;
                    break;
            }


            // Remove any axis from the normal if the view doesn't want to use them
            Vector3 forward = target.Forward;
            forward.x = view.UseX ? forward.x : 0;
            forward.y = view.UseY ? forward.y : 0;
            forward.z = view.UseZ ? forward.z : 0;
            forward.Normalize();

            Vector3 left = Quaternion.AngleAxis(-90, Up) * forward;

            // Offset is backwards + upwards + sideways
            Vector3 relativeOffset = (forward * view.Backwards) - (Up * view.Upwards) - (left * view.Sideways);
            // Set the position
            Vector3 newPos = target.Position - relativeOffset;

            // Smooth the transition a little
            if (view.SmoothMovement)
            {
                // Distance between the two points
                float distance = Vector3.Distance(transform.position, newPos);
                // Move by a fraction of the total distance
                float distanceToMove = distance / SecondsToReachTarget;

                float extra = 0;
                // Add the extra distance to force the view to stay within the bounds
                if (distance > view.SmoothMaxDistanceFromTarget && view.SmoothMaxDistanceFromTarget > 0)
                {
                    extra = distance - view.SmoothMaxDistanceFromTarget;
                }

                newPos = Vector3.MoveTowards(transform.position, newPos, distanceToMove * Time.deltaTime + extra);

            }
            // Assign the value
            transform.position = newPos;


            // Set the camera rotation
            if (view.LookAtBall)
            {
                transform.LookAt(target.Position + view.LookingAtPositionOffset);
            }
            else
            {
                // Ensure it is not zero to stop the editor spam message
                if(target.Forward != Vector3.zero)
                {
                    transform.forward = target.Forward;
                }
            }

            // Now add the rotation offset
            transform.eulerAngles = transform.eulerAngles + view.ExtraRotation;
        }
    }




    private void OnValidate()
    {
        if (Following != null)
        {
            if (Utils.GameObjectExtendsClass(Following.gameObject, out ICanBeFollowed f))
            {
                target = f;
            }
        }


    }


    [System.Serializable]
    public struct ViewPreset
    {
        public bool UseX, UseY, UseZ;
        public bool LookAtBall;
        public bool SmoothMovement;
        public float SmoothMaxDistanceFromTarget;

        public float Backwards;
        public float Upwards;
        public float Sideways;

        public Vector3 LookingAtPositionOffset;
        public Vector3 ExtraRotation;


        public ViewPreset(bool useX, bool useY, bool useZ, bool lookAtBall, bool smoothMovement, float smoothMaxDistanceFromTarget,
            float backwards, float upwards, float sideways, Vector3 lookingAtPositionOffset, Vector3 extraRotation)
        {
            UseX = useX;
            UseY = useY;
            UseZ = useZ;

            LookAtBall = lookAtBall;
            SmoothMovement = smoothMovement;

            SmoothMaxDistanceFromTarget = smoothMaxDistanceFromTarget;

            Backwards = backwards;
            Upwards = upwards;
            Sideways = sideways;

            LookingAtPositionOffset = lookingAtPositionOffset;
            ExtraRotation = extraRotation;
        }
    }

    public enum View
    {
        Behind,
        Above,
        ShootingAbove,
        ShootingLeft,
        ShootingBehind,
    }

}
