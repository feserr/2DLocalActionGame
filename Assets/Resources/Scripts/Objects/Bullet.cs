using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class Bullet : NetworkBehaviour
{
    /// <summary>
    /// Bullet GameObject
    /// </summary>
    private GameObject _bullet;

    /// <summary>
    /// Network transform object component
    /// </summary>
    private NetworkTransform _networkTransform;

    /// <summary>
    /// 2D Toolkit sprite of the bullet
    /// </summary>
    private SpriteRenderer _sprite = null;

    /// <summary>
    /// Direction of the bullet
    /// </summary>
    private Vector2 _direction;

    private int _layerMask;

    private Rect _box = new Rect(0, 0, 0 , 0);

    private Transform _transform;
    private BoxCollider2D _boxCollider;
    private Rigidbody2D _rigidbody;

    private float _acceleration = 1f;
    private Vector2 _velocity;
    private int _horizontalRays = 2;
    private int _verticalRays = 4;
    private float _margin = 0;
    private int _angleLeeway = 5;

    private bool _begin = true;

    public Vector3 position {
        get { return transform.position; }
        set { transform.position = value; }
    }

    public Vector2 direction {
        get { return _direction; }
        set { _direction = value; }
    }

    public float Rotate {
        // set { CmdRotate(value); }
        set { transform.Rotate(new Vector3(0.0f, 0.0f, value)); }
    }

    void OnDestroy()
    {
    }

    [Command]
    void CmdSpawn()
    {
        GameObject bullet = (GameObject)Instantiate(gameObject,
            transform.position, transform.rotation);
        NetworkServer.Spawn(bullet);
    }

    // Use this for initialization
    private void Start()
    {
        // CmdSpawn();

        _transform = this.transform;
        _boxCollider = this.GetComponent<BoxCollider2D>();
        _rigidbody = this.GetComponent<Rigidbody2D>();

        // Set the platform layer
        _layerMask = 1 << 8;
    }

    private void LateUpdate()
    {
        _box.Set(
            _transform.position.x + _boxCollider.offset.x - (_boxCollider.size.x / 2),
            _transform.position.y + _boxCollider.offset.y - (_boxCollider.size.y / 2),
            _boxCollider.size.x,
            _boxCollider.size.y);

        // Collision dectection
        float newVelocityX = _velocity.x;

        newVelocityX += _acceleration * Mathf.Sign(Mathf.Abs(_direction.x));

        newVelocityX = Mathf.Clamp(newVelocityX, -5.0f, 5.0f);

        _velocity = new Vector2(newVelocityX, _velocity.y);

        if (_velocity.x != 0)
        {
            Vector2 startPoint = new Vector2(_box.center.x, _box.yMin);
            Vector2 endPoint = new Vector2(_box.center.x, _box.yMax);

            RaycastHit2D[] hitInfos = new RaycastHit2D[_horizontalRays];

            float sideRayLength = _box.width / 2 +
                Mathf.Abs(newVelocityX * Time.deltaTime);
            Vector2 direction =
                (_direction.x > 0 ? Vector2.right : -Vector2.right);

            for (int i = 0; i < _horizontalRays; i++)
            {
                float lerpAmount = (float)i / (float)(_horizontalRays - 1);
                Vector2 origin = Vector2.Lerp(startPoint, endPoint, lerpAmount);

                if (Physics2D.Raycast(origin, direction, sideRayLength,
                    _layerMask))
                {
                    _velocity = Vector2.zero;
                    break;
                }
            }
        }

        _transform.Translate(_velocity * Time.deltaTime);
    }

    public void ResetBullet()
    {
        _transform.position = Vector3.zero;
        _direction = Vector2.zero;
        Rotate = 0.0f;
    }

    [Command]
    public void CmdResetBullet()
    {
        _transform.position = Vector3.zero;
        _direction = Vector2.zero;
        Rotate = 0.0f;
    }

    [Command]
    private void CmdMove(Vector2 velocity, float deltaTime)
    {
        _transform.Translate(_velocity * Time.deltaTime);
    }

    [Command]
    private void CmdRotate(float angle)
    {
        transform.Rotate(new Vector3(0.0f, 0.0f, angle));
    }
}
