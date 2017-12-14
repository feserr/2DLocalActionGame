using UnityEngine;
using UnityEngine.Networking;

public class GamePlayer : NetworkBehaviour
{
    private SmoothFollow _script;

    void Start()
    {
        // Set player position
        transform.position = new Vector2(3.0f, 3.0f);

        _script = (SmoothFollow)GameObject.Find("Camera")
            .GetComponent<SmoothFollow>();
        _script.enabled = true;
    }

    void OnDestroy()
    {
        _script.enabled = false;
    }

    void Update()
    {
        if (NetworkServer.active)
        {
            //UpdateServer();
        }
        if (NetworkClient.active)
        {
            UpdateClient();
        }
    }

    void UpdateClient()
    {
        if (!isLocalPlayer)
        {
            return;
        }

        _script.target = this.transform;
        _script.enabled = true;
    }

}
