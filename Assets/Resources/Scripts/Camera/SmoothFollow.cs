using UnityEngine;

public class SmoothFollow : MonoBehaviour
{
    public Transform target;
    public float smoothDampTime = 0.2f;

    public Vector3 cameraOffset;

    public bool useFixedUpdate = false;

    private Transform _transform;
    private Rigidbody2D _playerRigidbody2D;
    private Vector3 _smoothDampVelocity;

    void Start()
    {
        _transform = gameObject.transform;
        _playerRigidbody2D = target.GetComponent<Rigidbody2D>();
        cameraOffset = new Vector3(0.0f, 0.0f, 10.0f);
    }

    void LateUpdate()
    {
        if (!useFixedUpdate)
        {
            UpdateCameraPosition();
        }
    }

    void FixedUpdate()
    {
        if (useFixedUpdate)
        {
            UpdateCameraPosition();
        }
    }

    void UpdateCameraPosition()
    {
        if (_playerRigidbody2D == null)
        {
            _transform.position = Vector3.SmoothDamp(_transform.position,
                target.position - cameraOffset,
                ref _smoothDampVelocity, smoothDampTime);
            return;
        }

        if (_playerRigidbody2D.velocity.x > 0)
        {
            _transform.position = Vector3.SmoothDamp(_transform.position,
                target.position - cameraOffset,
                ref _smoothDampVelocity, smoothDampTime);
        }
        else
        {
            var leftOffset = cameraOffset;
            leftOffset.x *= -1;
            _transform.position = Vector3.SmoothDamp(_transform.position,
                target.position - leftOffset,
                ref _smoothDampVelocity, smoothDampTime);
        }
    }
}
