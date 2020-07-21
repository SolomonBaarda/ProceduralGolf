using UnityEngine;

public class GolfBall : MonoBehaviour, ICanBeFollowed
{
    public enum PlayState
    {
        Shooting,
        Flying,
        Rolling,
    }

    public PlayState State;
    public bool IsOnGround;

    public const float DragInAir = 0f, DragOnGround = 0.2f;
    public const float AngularDragInAir = 0f, AngularDragOnGround = 0.5f;



    public Vector3 Forward
    {
        get
        {
            if (rigid != null)
            {
                return rigid.velocity.normalized;
            }
            else
            {
                return Vector3.zero;
            }
        }
    }





    public const int Max_Power = 100;
    public float VelocityMagnitudeCutoffThreshold = 0.25f;
    [Header("Control settings")]
    [Range(0, Max_Power)] public float Power;
    public float Rotation;
    public float Angle;



    [Header("References")]
    public Rigidbody rigid;
    public SphereCollider sphereCollider;




    private void Awake()
    {

    }



    public void Shoot()
    {
        // Apply the force in direction
        Vector3 force = transform.forward * Power;
        rigid.AddForce(force, ForceMode.Impulse);
    }





    private void FixedUpdate()
    {
        State = UpdateState(ref IsOnGround);

        // On the ground
        if (IsOnGround)
        {
            rigid.angularDrag = AngularDragOnGround;
            rigid.drag = DragOnGround;
        }
        // In the air
        else
        {
            rigid.angularDrag = AngularDragInAir;
            rigid.drag = DragInAir;
        }

    }





    private void OnValidate()
    {
        // Clamp the power
        Power = Mathf.Clamp(Power, 0, Max_Power);

        // Ensure rotation is between 0 and 360
        while (Rotation < 0)
        {
            Rotation += 360;
        }
        while (Rotation > 360)
        {
            Rotation -= 360;
        }

        // Clamp the angle
        Angle = Mathf.Clamp(Angle, -80, 80);

        // Update the angle
        transform.rotation = Quaternion.Euler(Angle, Rotation, 0);
    }



    private PlayState UpdateState(ref bool onGround)
    {
        onGround = GroundCheck.IsOnGround(transform.position, sphereCollider.radius + GroundCheck.DEFAULT_RADIUS);
        float velocityMagnitude = rigid.velocity.magnitude;
        PlayState newState;

        if (!onGround)
        {
            newState = PlayState.Flying;
        }
        else
        {
            if (velocityMagnitude < VelocityMagnitudeCutoffThreshold)
            {
                newState = PlayState.Shooting;
            }
            else
            {
                newState = PlayState.Rolling;
            }
        }

        return newState;
    }




    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawLine(transform.position, transform.position + 10 * transform.forward);
    }
}
