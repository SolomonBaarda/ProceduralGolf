using UnityEngine;

public class GolfBall : MonoBehaviour
{
    public const int Max_Power = 100;
    [Header("Control settings")] 
    [Range(0, Max_Power)] public float Power;
    public float Rotation;
    public float Angle;



    [Header("References")]
    public Rigidbody rigid;
    public SphereCollider sphereCollider;




    public void Shoot()
    {
        // Apply the force in direction
        Vector3 force = transform.forward * Power;
        rigid.AddForce(force, ForceMode.Impulse);
    }



    private void OnValidate()
    {
        // Clamp the power
        Power = Mathf.Clamp(Power, 0, Max_Power);

        // Ensure rotation is between 0 and 360
        while(Rotation < 0)
        {
            Rotation += 360;
        }
        while(Rotation > 360)
        {
            Rotation -= 360;
        }

        // Clamp the angle
        Angle = Mathf.Clamp(Angle, -80, 80);

        // Update the angle
        transform.rotation = Quaternion.Euler(Angle, Rotation, 0);
    }



    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawLine(transform.position, transform.position + 10 * transform.forward);
    }
}
