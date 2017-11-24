
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Events;
using UnityStandardAssets.CrossPlatformInput;
using System.Collections.Generic;
using System.Collections;

/// <summary>
/// This class controls the platformerMotor2D in network games
/// </summary>
[RequireComponent(typeof(PlatformerMotor2D))]
[NetworkSettings(channel = 0, sendInterval = 0.1f)]
public class NetworkController2D : NetworkBehaviour
{
    [SyncVar(hook = "SyncPositionValues")]
    private Vector3 syncPos;

    private Queue pendingMoves;

    private Transform _playerTransform;
    private PlatformerMotor2D _motor;
    private Joystick _joystickRight;
    private float _dx = 0.0f;
    private GameObject _bulletPoll;

    private int _fireCooldown = 0;

    // Use this for initialization
    void Awake()
    {
        _playerTransform = GetComponent<Transform>();
        _motor = GetComponent<PlatformerMotor2D>();
        _joystickRight = GameObject.Find("RightMobileJoystick")
            .GetComponent<Joystick>();

        _bulletPoll = new GameObject("BulletPoll");
        _bulletPoll.AddComponent<PoolSystem>();
        _bulletPoll.GetComponent<PoolSystem>().Init(30,
            Resources.Load("Prefabs/Bullet") as GameObject);
    }

    void Update()
    {
        if (!isLocalPlayer)
        {
            UpdateServer();
        }
        else
        {
            UpdateClient();
        }
    }

    void UpdateServer()
    {
        transform.position = syncPos;
        // transform.position = Vector3.Lerp(transform.position, syncPos,
        //     Time.deltaTime * 15);
    }

    [ClientCallback]
    void UpdateClient()
    {
        if (!isLocalPlayer)
        {
            pendingMoves = new Queue();
            return;
        }

        if (Mathf.Abs(Input.GetAxis(PC2D.Input.HORIZONTAL)) >
            PC2D.Globals.INPUT_THRESHOLD)
        {
            //_motor.normalizedXMovement = Input.GetAxis(PC2D.Input.HORIZONTAL);
            _dx = Input.GetAxis(PC2D.Input.HORIZONTAL);
        }
        else if (Mathf.Abs(
            CrossPlatformInputManager.GetAxis(GameGlobals.LEFTJOYHORIZONTAL)) >
                GameGlobals.JOY_THRESHOLD)
        {
            //_motor.normalizedXMovement =
            //    CrossPlatformInputManager.GetAxis(GameGlobals.LEFTJOYHORIZONTAL);
            _dx = CrossPlatformInputManager.GetAxis(GameGlobals.LEFTJOYHORIZONTAL);
        }
        else
        {
            //_motor.normalizedXMovement = 0;
            _dx = 0;
        }

        if (isClient)
        {
            _motor.normalizedXMovement = _dx;
        } else if (isServer)
        {
            RpcHorizontalMovement(_dx);
        }
        CmdHorizontalMovement(_dx);

        // Jump?
        if (Input.GetButtonDown(PC2D.Input.JUMP) ||
            CrossPlatformInputManager.GetButtonDown("Jump"))
        {
            // _joystickRight.SetImage(false);

            if (isClient)
            {
                _motor.Jump();
            }
            CmdDoJump();
        }

        _motor.jumpingHeld = (Input.GetButton(PC2D.Input.JUMP) ||
            CrossPlatformInputManager.GetButton("Jump"));

        if (Input.GetAxis(PC2D.Input.VERTICAL) <
            -PC2D.Globals.FAST_FALL_THRESHOLD ||
           (CrossPlatformInputManager.GetAxis(GameGlobals.LEFTJOYVERTICAL) <
            -PC2D.Globals.FAST_FALL_THRESHOLD))
        {
            if (isClient)
            {
                _motor.fallFast = true;
            }
            CmdFallFast(true);
        }
        else
        {
            if (isClient)
            {
                _motor.fallFast = false;
            }
            CmdFallFast(false);
        }

        if ((Mathf.Abs(CrossPlatformInputManager
            .GetAxis(GameGlobals.RIGHTJOYHORIZONTAL)) > GameGlobals.JOY_THRESHOLD) ||
           (Mathf.Abs(CrossPlatformInputManager
           .GetAxis(GameGlobals.RIGHTJOYVERTICAL)) > GameGlobals.JOY_THRESHOLD))
        {
            Vector2 joystick = new Vector2(
                CrossPlatformInputManager.GetAxis(GameGlobals.RIGHTJOYHORIZONTAL),
                CrossPlatformInputManager.GetAxis(GameGlobals.RIGHTJOYVERTICAL));

            CmdFire(joystick, false);
            if (isClient)
            {
                Fire(joystick, false);
            } else if (isServer) {
                RpcFire(joystick, false);
            }
        }

        if (Input.GetButtonDown(PC2D.Input.FIRE))
        {
            CmdFire(new Vector2(1.0f, 0.0f), true);
            if (isClient)
            {
                Fire(new Vector2(1.0f, 0.0f), true);
            } else if (isServer) {
                RpcFire(new Vector2(1.0f, 0.0f), true);
            }
        }

        if (Input.GetButtonDown(PC2D.Input.DASH))
        {
            if (isClient)
            {
                _motor.Dash();
            }
            CmdDoDash();
        }

        CmdProvidePositionToServer(transform.position);
    }

    [Command]
    void CmdProvidePositionToServer(Vector3 pos)
    {
        syncPos = pos;
    }

    void HorizontalMovement(float dx)
    {
        _motor.normalizedXMovement = dx;
    }

    [Command]
    void CmdHorizontalMovement(float dx)
    {
        HorizontalMovement(dx);
    }

    [ClientRpc]
    void RpcHorizontalMovement(float dx)
    {
        HorizontalMovement(dx);
    }

    [Command]
    void CmdStopMovement()
    {
        _dx = 0.0f;
    }

    [Command]
    void CmdDoJump()
    {
        _motor.Jump();
    }

    [Command]
    void CmdJumpHeld()
    {
        _motor.jumpingHeld = (Input.GetButton(PC2D.Input.JUMP) ||
            CrossPlatformInputManager.GetButton("Jump"));
    }

    [Command]
    void CmdFallFast(bool fall)
    {
        _motor.fallFast = fall;
    }

    [Command]
    void CmdDoDash()
    {
        _motor.Dash();
    }

    [Command]
    void CmdFire(Vector2 direction, bool keyboard)
    {
        if (++_fireCooldown % 10 == 0 || keyboard)
        {
            GameObject obj = _bulletPoll.GetComponent<PoolSystem>().GetObject();
            obj.SetActive(true);

            obj.transform.position = _playerTransform.transform.position;

            Bullet bulletScript = obj.GetComponent<Bullet>();
            bulletScript.direction = direction;

            // Rotate the x axis
            bulletScript.Rotate =
                direction.x * (direction.x > 0.0f ? 0.0f : -180.0f);
            // Rotate the y axis
            bulletScript.Rotate =
                direction.y * (direction.x > 0.0f ? 90.0f : -90.0f);

            StartCoroutine(ResetBullet(5.0f, obj));
        }
    }

    void Fire(Vector2 direction, bool keyboard)
    {
        if (++_fireCooldown % 10 == 0 || keyboard)
        {
            GameObject obj = _bulletPoll.GetComponent<PoolSystem>().GetObject();
            obj.SetActive(true);

            obj.transform.position = _playerTransform.transform.position;

            Bullet bulletScript = obj.GetComponent<Bullet>();
            bulletScript.direction = direction;

            // Rotate the x axis
            bulletScript.Rotate =
                direction.x * (direction.x > 0.0f ? 0.0f : -180.0f);
            // Rotate the y axis
            bulletScript.Rotate =
                direction.y * (direction.x > 0.0f ? 90.0f : -90.0f);

            StartCoroutine(ResetBullet(5.0f, obj));
        }
    }

    [ClientRpc]
    void RpcFire(Vector2 direction, bool keyboard)
    {
        if (++_fireCooldown % 10 == 0 || keyboard)
        {
            GameObject obj = _bulletPoll.GetComponent<PoolSystem>().GetObject();
            obj.SetActive(true);

            obj.transform.position = _playerTransform.transform.position;

            Bullet bulletScript = obj.GetComponent<Bullet>();
            bulletScript.direction = direction;

            // Rotate the x axis
            bulletScript.Rotate =
                direction.x * (direction.x > 0.0f ? 0.0f : -180.0f);
            // Rotate the y axis
            bulletScript.Rotate =
                direction.y * (direction.x > 0.0f ? 90.0f : -90.0f);

            StartCoroutine(ResetBullet(5.0f, obj));
        }
    }

    [Command]
    void CmdAddBullet(GameObject bullet)
    {
        Instantiate(bullet);
        ClientScene.RegisterPrefab(bullet);
    }

    [Client]
    void SyncPositionValues(Vector3 latestPos)
    {
        syncPos = latestPos;
    }

    private IEnumerator ResetBullet(float time, GameObject obj)
    {
        yield return new WaitForSeconds(time);

        // CmdReset(obj);
        Bullet script = obj.GetComponent<Bullet>();
        script.ResetBullet();
        _bulletPoll.GetComponent<PoolSystem>().DestroyObjectPool(obj);
    }

    [Command]
    void CmdReset(GameObject obj)
    {
        Bullet script = obj.GetComponent<Bullet>();
        script.CmdResetBullet();
        _bulletPoll.GetComponent<PoolSystem>().DestroyObjectPool(obj);
    }
}
