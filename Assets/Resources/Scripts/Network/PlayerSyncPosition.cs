using UnityEngine;
using System.Collections;
using UnityEngine.Networking;
using UnityEngine.UI;
using System.Collections.Generic;

[NetworkSettings(channel = 0, sendInterval = 0.1f)]
public class PlayerSyncPosition : NetworkBehaviour
{
    [SyncVar(hook = "SyncPositionValues")]
    private Vector3 _syncPos;

    [SerializeField] Transform _transform;
    private float _lerpRate;
    private float _normalLerpRate = 16;
    private float _fasterLerpRate = 27;

    private Vector3 _lastPos;
    private float _threshold = 0.5f;

    private NetworkClient _client;
    private int _latency;
    private Text _latencyText;

    private List<Vector3> _syncPosList = new List<Vector3>();
    [SerializeField] private bool _useHistoricalLerping = false;
    private float _closeEnough = 0.11f;

    void Start()
    {
        _transform = transform;
        _client = GameObject.Find("NetworkManager")
            .GetComponent<NetworkManager>().client;
        _latencyText = GameObject.Find("Latency Text").GetComponent<Text>();
        _lerpRate = _normalLerpRate;
    }

    void Update()
    {
        LerpPosition();
        ShowLatency();
    }

    void FixedUpdate()
    {
        TransmitPosition();

    }

    void LerpPosition()
    {
        if (!isLocalPlayer)
        {
            if (_useHistoricalLerping)
            {
                HistoricalLerping();
            }
            else
            {
                OrdinaryLerping();
            }
        }
    }

    [Command]
    void CmdProvidePositionToServer(Vector3 pos)
    {
        _syncPos = pos;
    }

    [ClientCallback]
    void TransmitPosition()
    {
        if (isLocalPlayer && Vector3.Distance(_transform.position, _lastPos) >
            _threshold)
        {
            CmdProvidePositionToServer(_transform.position);
            _lastPos = _transform.position;
        }
    }

    [Client]
    void SyncPositionValues(Vector3 latestPos)
    {
        _syncPos = latestPos;
        _syncPosList.Add(_syncPos);
    }

    void ShowLatency()
    {
        if (isLocalPlayer)
        {
            _latency = _client.GetRTT();
            _latencyText.text = _latency.ToString();
        }
    }

    void OrdinaryLerping()
    {
        _transform.position = Vector3.Lerp(_transform.position, _syncPos,
            Time.deltaTime * _lerpRate);
    }

    void HistoricalLerping()
    {
        if (_syncPosList.Count > 0)
        {
            _transform.position = Vector3.Lerp(_transform.position,
                _syncPosList[0], Time.deltaTime * _lerpRate);

            if (Vector3.Distance(_transform.position, _syncPosList[0]) <
                _closeEnough)
            {
                _syncPosList.RemoveAt(0);
            }

            if (_syncPosList.Count > 10)
            {
                _lerpRate = _fasterLerpRate;
            }
            else
            {
                _lerpRate = _normalLerpRate;
            }
        }
    }
}
