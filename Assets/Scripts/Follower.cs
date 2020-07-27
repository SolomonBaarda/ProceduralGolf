using UnityEngine;

public class Follower : MonoBehaviour
{
    public readonly Vector3 Up = Vector3.up;

    public GameObject Following;
    private ICanBeFollowed target;

    [Header("Offset Presets")]
    public static readonly ViewPreset ViewPresetBehind = new ViewPreset(1f, 0.25f, 0f, false, Vector3.zero, Vector3.zero);
    public static readonly ViewPreset ViewPresetAbove = new ViewPreset(1.5f, 1f, 0f, true, Vector3.zero, Vector3.zero);


    /// <summary>
    /// View for setting the shot rotation.
    /// </summary>
    public static readonly ViewPreset ViewPresetShootingAbove = new ViewPreset(3f, 2f, 0f, true, new Vector3(0, 1, 0), Vector3.zero);
    /// <summary>
    /// View for setting the shot angle.
    /// </summary>
    public static readonly ViewPreset ViewPresetShootingLeft = new ViewPreset(0f, 0.5f, 3f, true, new Vector3(0, 0.5f, 0), Vector3.zero);
    /// <summary>
    /// View for setting the shot power.
    /// </summary>
    public static readonly ViewPreset ViewPresetShootingBehind = new ViewPreset(5f, 2f, 2f, false, Vector3.zero, Vector3.zero);


    [Header("Current View")]
    public View CurrentView;
    private ViewPreset view;


    private void Update()
    {
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

            Vector3 left = Quaternion.AngleAxis(-90, Up) * target.Forward;

            // Offset is backwards + upwards + sideways
            Vector3 relativeOffset = (target.Forward * view.Backwards) - (Up * view.Upwards) - (left * view.Sideways);
            // Set the position
            transform.position = target.Position - relativeOffset;


            // Set the camera rotation
            if (view.LookAtBall)
            {
                transform.LookAt(target.Position + view.LookingAtPositionOffset);
            }
            else
            {
                transform.forward = target.Forward;
            }

            // Now add the rotation offset
            transform.eulerAngles = transform.eulerAngles + view.ExtraRotation;
        }
    }




    private void OnValidate()
    {
        if (Following != null)
        {
            if (Utils.GameObjectExtendsClass(Following, out ICanBeFollowed f))
            {
                target = f;
            }
        }


    }


    [System.Serializable]
    public struct ViewPreset
    {
        public float Backwards;
        public float Upwards;
        public float Sideways;

        public bool LookAtBall;
        public Vector3 LookingAtPositionOffset;
        public Vector3 ExtraRotation;

        public ViewPreset(float backwards, float upwards, float sideways, bool lookAtBall, Vector3 lookingAtPositionOffset, Vector3 extraRotation)
        {
            Backwards = backwards;
            Upwards = upwards;
            Sideways = sideways;

            LookAtBall = lookAtBall;
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
