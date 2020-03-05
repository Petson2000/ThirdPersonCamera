using UnityEngine;

[RequireComponent(typeof(Camera))]
public class ThirdPersonCamera : MonoBehaviour
{
    Vector3 focusPoint;
    Vector3 previousFocusPoint;
    Vector2 orbitAngles = new Vector2(45f, 0f);

    Camera regularCamera;

    float lastManualRotationTime; //Keeps track of last manual rotation

    [SerializeField]
    [Tooltip("Object the camera will focus on")]
    Transform focus = default;

    [SerializeField, Range(1f, 20f)]
    [Tooltip("Distance from focus point")]
    float distance = 5f;

    [SerializeField, Min(0f)]
    [Tooltip("Moves the camera when its focus points differs this much from the ideal focus (player)")]
    float focusRadius = 1f;

    [SerializeField, Range(0f, 1f)]
    [Tooltip("Keeps camera moving until the focus is back in the center of view")]
    float focusCentering = 0.5f;

    [SerializeField, Range(1f, 360f)]
    float rotationSpeed = 90f;

    [SerializeField, Range(-89f, 89)]
    [Tooltip("Max and minimum camera angles for the focused object")]
    float minVerticalAngle = -30f, maxVerticalAngle = 60f;

    [SerializeField, Min(0f)]
    [Tooltip("Time until camera auto aligns with player, to turn this setting off, put 0 as the value.")]
    float alignDelay = 5f;

    [SerializeField, Range(0f, 90f)]
    float alignSmoothRange = 45f;

    [SerializeField]
    [Tooltip("The layers the camera will adjust position if focus point is lost due to an obstacle")]
    LayerMask obstructionMask = -1;

    [SerializeField, Range(1f, 20f)]
    float minDistance = 1f;

    [SerializeField, Range(1f, 20f)]
    float MaxDistance = 20f;


    private void Awake()
    {
        regularCamera = GetComponent<Camera>();
        focusPoint = focus.position;
        transform.localRotation = Quaternion.Euler(orbitAngles);
    }
    private void LateUpdate()
    {
        if(Input.GetAxisRaw("Mouse ScrollWheel") > 0 && distance > minDistance)
        {
            distance--;
        }
        
        else if(Input.GetAxisRaw("Mouse ScrollWheel") < 0 && distance < MaxDistance)
        {
            distance++;
        }

        UpdateFocusPoint();
        ManualRotation();

        Quaternion lookRotation;

        if (ManualRotation() || AutomaticRotation())
        {
            ConstrainAngles();
            lookRotation = Quaternion.Euler(orbitAngles);
        }

        else
        {
            lookRotation = transform.rotation;
        }

        Vector3 lookDirection = lookRotation * Vector3.forward;             //Handles camera to orbit the player 
        Vector3 lookPosition = focusPoint - lookDirection * distance;

        // Variables for box cast to prevent the environment from covering up the player
        Vector3 rectOffset = lookDirection * regularCamera.nearClipPlane;
        Vector3 rectPosition = lookPosition + rectOffset;
        Vector3 castFrom = focus.position;
        Vector3 castLine = rectPosition - castFrom;
        float castDistance = castLine.magnitude;
        Vector3 castDirection = castLine / castDistance;

        //Check if player can be seen, if not, change position.
        if (Physics.BoxCast(castFrom, CameraHalfExtends, castDirection, out RaycastHit hit, lookRotation, castDistance - regularCamera.nearClipPlane, obstructionMask))
        {
            rectPosition = castFrom + castDirection * hit.distance;
            lookPosition = rectPosition - rectOffset;
        }

        transform.SetPositionAndRotation(lookPosition, lookRotation);
    }

    void UpdateFocusPoint()
    {
        previousFocusPoint = focusPoint;

        Vector3 targetPoint = focus.position;

        if (focusRadius > 0f) //if using focusRadius, otherwise set focus point to target point all the time.
        {
            float distance = Vector3.Distance(targetPoint, focusPoint);

            if (distance > focusRadius) //Camera is too far from player, get closer
            {
                focusPoint = Vector3.Lerp(targetPoint, focusPoint, focusRadius / distance);
            }

            if (distance > 0.01f && focusCentering > 0f)
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

        if (input.x < -e || input.x > e || input.y < -e || input.y > e) //If input exceeds e, meaning that very small inputs wont be adjusted for
        {
            orbitAngles += rotationSpeed * Time.unscaledDeltaTime * input;
            lastManualRotationTime = Time.unscaledDeltaTime;
            return true;
        }

        return false;
    }

    private void OnValidate() //Make sure max doesn't drop below min
    {
        if (maxVerticalAngle < minVerticalAngle)
        {
            maxVerticalAngle = minVerticalAngle;
        }
    }

    void ConstrainAngles() //Locks camera rotation between different angles.
    {
        orbitAngles.x = Mathf.Clamp(orbitAngles.x, minVerticalAngle, maxVerticalAngle);

        if (orbitAngles.y < 0f) //Make sure the horizontal stays between 0 and 360 to avoid bugs
        {
            orbitAngles.y += 360f;
        }

        else if (orbitAngles.y >= 360f)
        {
            orbitAngles.y -= 360f;
        }
    }

    bool AutomaticRotation() //Checks if the camera should auto align with player and handles automatic camera rotation
    {
        if(alignDelay == 0)
        {
            return false;
        }

        if (Time.unscaledDeltaTime - lastManualRotationTime < alignDelay)
        {
            return false;
        }

        Vector2 movement = new Vector2(focusPoint.x - previousFocusPoint.x, focusPoint.z - previousFocusPoint.z); //Calculate the movement vector for the current frame

        float movementDeltaSqr = movement.sqrMagnitude;
        
        if (movementDeltaSqr < 0.000001f)
        {
            return false;
        }

        float headingAngle = GetAngle(movement / Mathf.Sqrt(movementDeltaSqr));

        float deltaAbs = Mathf.Abs(Mathf.DeltaAngle(orbitAngles.y, headingAngle));

        float rotationChange = rotationSpeed * Mathf.Min(Time.unscaledDeltaTime, movementDeltaSqr);

        if (deltaAbs < alignSmoothRange)
        {
            rotationChange *= deltaAbs / alignSmoothRange;
        }

        else if (180f - deltaAbs < alignSmoothRange)
        {
            rotationChange *= (180f - deltaAbs) / alignSmoothRange;
        }
        orbitAngles.y = Mathf.MoveTowardsAngle(orbitAngles.y, headingAngle, rotationChange);

        return true;
    }

    static float GetAngle(Vector2 direction) //Convert 2D direction to an angle
    {
        float angle = Mathf.Acos(direction.y) * Mathf.Rad2Deg;
        return direction.x < 0f ? 360f - angle : angle; //If x is negative its counter clockwise and then we have to subtract the angle from 360 deg
    }

    Vector3 CameraHalfExtends //Calculate half of cameras height, width and depth
    {
        get
        {
            Vector3 halfExtends;
            halfExtends.y = regularCamera.nearClipPlane * Mathf.Tan(0.5f * Mathf.Deg2Rad * regularCamera.fieldOfView);
            halfExtends.x = halfExtends.y * regularCamera.aspect;
            halfExtends.z = 0f;
            return halfExtends;
        }
    }
}