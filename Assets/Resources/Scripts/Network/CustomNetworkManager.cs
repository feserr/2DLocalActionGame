using System.Net;
using System.Net.Sockets;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;

[RequireComponent(typeof(NetworkManager))]
public class CustomNetworkManager : MonoBehaviour
{
    public string onlineStatus;
    static public CustomNetworkManager s_Singleton;
    public bool isAtStartup = true;
    NetworkClient myClient;

    private string _serverAddress;
    private string _serverPort;
    private bool _isServer = false;
    private NetworkManager _networkManager;

    private Rect _auxRect;

    void Start()
    {
        _networkManager = NetworkManager.singleton;

        s_Singleton = this;

        _serverPort = "8080";
        _serverAddress = LocalIPAddress().ToString();

        _auxRect = new Rect();
    }

    void Update()
    {
        if (isAtStartup)
        {
            if (Input.GetKeyDown(KeyCode.S))
            {
                SetupServer();
            }

            if (Input.GetKeyDown(KeyCode.C))
            {
                SetupClient();
            }

            if (Input.GetKeyDown(KeyCode.B))
            {
                SetupLocalClient();
            }
        }
    }

    void OnGUI()
    {
        //set up scaling
        float rx = Screen.width / 480;
        float ry = Screen.height / 320;
        GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity,
            new Vector3(rx, ry, 1));

        if (isAtStartup)
        {
            _auxRect.Set(2, 10, 150, 30);
            if (GUI.Button(_auxRect, "Press S for server"))
            {
                SetupServer();
            }

            _auxRect.Set(2, 40, 150, 30);
            if (GUI.Button(_auxRect, "Press B for both"))
            {
                SetupLocalClient();
            }

            _auxRect.Set(2, 70, 150, 30);
            if (GUI.Button(_auxRect, "Press C for client"))
            {
                SetupClient();
            }

            _auxRect.Set(160, 10, 150, 30);
            _serverPort =
                GUI.TextField(_auxRect, _serverPort);

            _auxRect.Set(160, 70, 150, 30);
            _serverAddress =
                GUI.TextField(_auxRect, _serverAddress);
        }
        else
        {
            _auxRect.Set(2, 10, 150, 30);
            if (GUI.Button(_auxRect, "Stop"))
            {
                StopNetwork();
            }
        }
    }

    // Create a server and listen on a port
    public void SetupServer()
    {
        int port = int.Parse(_serverPort);
        NetworkServer.Listen(port);

        _networkManager.StartServer();
        _networkManager.networkPort = port;
        _networkManager.networkAddress = _serverAddress;

        _isServer = true;
        isAtStartup = false;
    }

    // Create a client and connect to the server port
    public void SetupClient()
    {
        _networkManager.networkAddress = _serverAddress;
        _networkManager.networkPort = 7777;
        _networkManager.StartClient();

        _isServer = false;
        isAtStartup = false;
    }

    // Create a local client and connect to the local server
    public void SetupLocalClient()
    {
        int port = int.Parse(_serverPort);

        // Listen to the port
        NetworkServer.Listen(port);

        // Start the sever and join in it
        _networkManager.StartHost();
        _networkManager.networkAddress = _serverAddress;
        _networkManager.networkPort = port;

        _isServer = true;
        isAtStartup = false;
    }

    private void StopNetwork()
    {
        if (_isServer)
        {
            _networkManager.StopHost();
        }
        else
        {
            _networkManager.StopClient();
        }

        _networkManager.networkAddress = "localhost";
        _networkManager.networkPort = 7777;

        isAtStartup = true;
    }

    private IPAddress LocalIPAddress()
    {
        if (!System.Net.NetworkInformation.NetworkInterface
            .GetIsNetworkAvailable())
        {
            return null;
        }

        IPHostEntry host = Dns.GetHostEntry(Dns.GetHostName());

        return host.AddressList.FirstOrDefault(
            ip => ip.AddressFamily == AddressFamily.InterNetwork);
    }
}
