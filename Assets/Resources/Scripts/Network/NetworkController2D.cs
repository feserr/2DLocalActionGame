
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

    private Transform _playerTransform;
    private PlatformerMotor2D _motor;
    private JoystickRight _joystickRight;
    private float _dx = 0.0f;
    private GameObject _bulletPoll;

    private int _fireCooldown = 0;

    // Use this for initialization
    void Start()
    {
        _playerTransform = GetComponent<Transform>();
        _motor = GetComponent<PlatformerMotor2D>();
        _joystickRight = GameObject.Find("RightMobileJoystick")
            .GetComponent<JoystickRight>();

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
        transform.position = Vector3.Lerp(transform.position, syncPos,
            Time.deltaTime * 15);
    }

    [ClientCallback]
    void UpdateClient()
    {
        if (!isLocalPlayer)
            return;

        if (Mathf.Abs(Input.GetAxis(PC2D.Input.HORIZONTAL)) >
            PC2D.Globals.INPUT_THRESHOLD)
        {
            _motor.normalizedXMovement = Input.GetAxis(PC2D.Input.HORIZONTAL);
            _dx = Input.GetAxis(PC2D.Input.HORIZONTAL);
        }
        else if (Mathf.Abs(
            CrossPlatformInputManager.GetAxis(GameGlobals.LEFTJOYHORIZONTAL)) >
                GameGlobals.JOY_THRESHOLD)
        {
            _motor.normalizedXMovement =
                CrossPlatformInputManager.GetAxis(GameGlobals.LEFTJOYHORIZONTAL);
            _dx = CrossPlatformInputManager.GetAxis(GameGlobals.LEFTJOYHORIZONTAL);
        }
        else
        {
            _motor.normalizedXMovement = 0;
            _dx = 0;
        }

        CmdHorizontalMovement(_dx);

        // Jump?
        if (Input.GetButtonDown(PC2D.Input.JUMP) ||
            CrossPlatformInputManager.GetButtonDown("Jump"))
        {
            _motor.Jump();
            _joystickRight.SetImage(false);
            CmdDoJump();
        }

        _motor.jumpingHeld = (Input.GetButton(PC2D.Input.JUMP) ||
            CrossPlatformInputManager.GetButton("Jump"));

        if (Input.GetAxis(PC2D.Input.VERTICAL) <
            -PC2D.Globals.FAST_FALL_THRESHOLD ||
           (CrossPlatformInputManager.GetAxis(GameGlobals.LEFTJOYVERTICAL) <
            -PC2D.Globals.FAST_FALL_THRESHOLD))
        {
            _motor.fallFast = true;
            CmdFallFast(true);
        }
        else
        {
            _motor.fallFast = false;
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
            Fire(joystick, false);
        }

        if (Input.GetButtonDown(PC2D.Input.FIRE))
        {
            CmdFire(new Vector2(1.0f, 0.0f), true);
            //Fire(new Vector2(1.0f, 0.0f), true);
        }

        if (Input.GetButtonDown(PC2D.Input.DASH))
        {
            _motor.Dash();
            CmdDoDash();
        }

        CmdProvidePositionToServer(transform.position);
    }

    [Command]
    void CmdProvidePositionToServer(Vector3 pos)
    {
        syncPos = pos;
    }

    [Command]
    void CmdHorizontalMovement(float dx)
    {
        _motor.normalizedXMovement = dx;
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
            //GameObject bullet = new GameObject("Bullet");
            // GameObject bulletClone = (GameObject)Instantiate(_bullet);
            // bulletClone.AddComponent<NetworkIdentity>();

            // ClientScene.RegisterPrefab(bulletClone);

            // bulletClone.transform.position = _playerTransform.position;
            // Bullet bulletScript = bulletClone.AddComponent<Bullet>();
            // bulletScript.direction = direction;

            // // Rotate the x axis
            // bulletScript.Rotate =
            //     direction.x * (direction.x > 0.0f ? 0.0f : -180.0f);
            // // Rotate the y axis
            // bulletScript.Rotate =
            //     direction.y * (direction.x > 0.0f ? 90.0f : -90.0f);

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

            StartCoroutine(ResetBullet(5.0f, obj, bulletScript));
        }
    }

    void Fire(Vector2 direction, bool keyboard)
    {
        if (++_fireCooldown % 10 == 0 || keyboard)
        {
            //GameObject bullet = new GameObject("Bullet");
            // GameObject bulletClone = (GameObject)Instantiate(_bullet);
            // bulletClone.AddComponent<NetworkIdentity>();

            // //ClientScene.RegisterPrefab(bulletClone);

            // bulletClone.transform.position = _playerTransform.position;
            // Bullet bulletScript = bulletClone.AddComponent<Bullet>();
            // bulletScript.direction = direction;

            // // Rotate the x axis
            // bulletScript.Rotate =
            //     direction.x * (direction.x > 0.0f ? 0.0f : -180.0f);
            // // Rotate the y axis
            // bulletScript.Rotate =
            //     direction.y * (direction.x > 0.0f ? 90.0f : -90.0f);

            // //CmdAddBullet(bulletClone);
            // ClientScene.RegisterPrefab(bulletClone);
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

    private IEnumerator ResetBullet(float time, GameObject obj, Bullet script)
    {
        yield return new WaitForSeconds(time);

        script.ResetBullet();
        _bulletPoll.GetComponent<PoolSystem>().DestroyObjectPool(obj);
    }
}
