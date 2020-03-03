using UnityEngine;

[RequireComponent(typeof(Camera))]
public class ThirdPersonCamera : MonoBehaviour
{
    RaycastHit Hit;

    public GameObject Player;

    Vector3 focusPoint;
    Vector2 orbitAngles = new Vector2(45f, 0f);

    [SerializeField]
    Transform focus = default;

    [SerializeField, Range(1f, 20f)]
    float distance = 5f;

    [SerializeField, Min(0f)]
    float focusRadius = 1f;

    [SerializeField, Range(0f, 1f)] //Keeps camera moving until the focus is back in the center of view
    float focusCentering = 0.5f;

    [SerializeField, Range(1f, 360f)]
    float rotationSpeed = 90f;

    [SerializeField, Range(-89f, 89)]
    float minVerticalAngle = -30f, maxVerticalAngle = 60f;

    private void Awake()
    {
        focusPoint = focus.position;
        transform.localRotation = Quaternion.Euler(orbitAngles);
    }


    void Start()
    {
    }

    void Update()
    {

        // if(Physics.Linecast(transform.position, Player.transform.position, out Hit))
        // {
        //     if(Hit.transform.gameObject == Player)
        //     {
        //         Debug.Log("Player Found!");
        //     }
        //
        //     else
        //     {
        //         Debug.Log(Hit.transform.name);
        //     }

        // if(rotateAroundPlayer)
        // {
        //     Quaternion camTurnAngle = Quaternion.AngleAxis(Input.GetAxis("Mouse X") * rotationSpeed, Vector3.up);
        // }
    }

    private void LateUpdate()
    {
        UpdateFocusPoint();
        ManualRotation();

        Quaternion lookRotation;
        
        if(ManualRotation())
        {
            ConstrainAngles();
            lookRotation = Quaternion.Euler(orbitAngles);
        }

        else
        {
            lookRotation = transform.rotation;
        }

        Vector3 lookDirection = lookRotation * Vector3.forward;             //Handles camera to orbit the player 
        Vector3 lookPosition  = focusPoint - lookDirection * distance;

        transform.SetPositionAndRotation(lookPosition, lookRotation);
    }

    void UpdateFocusPoint()
    {
        Vector3 targetPoint = focus.position;

        if(focusRadius > 0f) 
        {
            float distance = Vector3.Distance(targetPoint, focusPoint);
            if(distance > focusRadius) //Camera is too far from player, get closer
            {
                focusPoint = Vector3.Lerp(targetPoint, focusPoint, focusRadius / distance);
            }

            if(distance > 0.01f && focusCentering > 0f)
            {
                focusPoint = Vector3.Lerp(targetPoint, focusPoint, Mathf.Pow(1f - focusCentering, Time.unscaledDeltaTime));
            }
        }

        else
        {
            focusPoint = targetPoint;
        }
    }

    bool ManualRotation()
    {
        Vector2 input = new Vector2(Input.GetAxis("Vertical Camera"), Input.GetAxis("Horizontal Camera"));

        const float e = 0.001f;

        if(input.x < -e || input.x > e || input.y < -e || input.y > e) //If input exceeds e, meaning that very small inputs wont be adjusted for
        {
            orbitAngles += rotationSpeed * Time.unscaledDeltaTime * input;
            return true;
        }

        return false;
    }

    private void OnValidate() //Make sure max doesn't drop below min
    {
        if(maxVerticalAngle < minVerticalAngle)
        {
            maxVerticalAngle = minVerticalAngle;
        }
    }

    void ConstrainAngles() //Locks camera rotation between different angles.
    {
        orbitAngles.x = Mathf.Clamp(orbitAngles.x, minVerticalAngle, maxVerticalAngle);

        if(orbitAngles.y < 0f) //Make sure the horizontal stays between 0 and 360 to avoid bugs
        {
            orbitAngles.y += 360f;
        }

        else if(orbitAngles.y >= 360f)
        {
            orbitAngles.y -= 360f;
        }
    }

}

