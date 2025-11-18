using System;
using System.Collections;
using System.Collections.Generic;
using RConsole.Runtime;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Sample : MonoBehaviour
{
    public TMP_InputField ifServerIp;
    public Button btnConnect;
    public Button btnDisconnect;
    public Button btnForward;
    public Button btnStopForward;

    // Start is called before the first frame update
    void Start()
    {
        // ifServerIp.text = "127.0.0.1";
        var cacheIp = PlayerPrefs.GetString("ServerIp", "127.0.0.1");
        ifServerIp.text = cacheIp;

    }

    // Update is called once per frame
    void Update()
    {
        btnConnect.interactable = !RCLogManager.Instance.IsConnected;
        btnDisconnect.interactable = RCLogManager.Instance.IsConnected;
        btnForward.interactable = !RCLogManager.Instance.IsCapturingLogs;
        btnStopForward.interactable = RCLogManager.Instance.IsCapturingLogs;
    }

    public void OnConnectClicked()
    {
        PlayerPrefs.SetString("ServerIp", ifServerIp.text);
        RCLogManager.Instance.Connect(ifServerIp.text);
    }

    public void OnDisconnectClicked()
    {
        RCLogManager.Instance.Disconnect();
    }

    public void OnForwardClicked()
    {
        RCLogManager.Instance.ForwardingUnityLog();
    }

    public void OnStopForwardClicked()
    {
        RCLogManager.Instance.StopForwardingUnityLog();
    }

    public void OnLogClicked()
    {
        Debug.Log("Log. Hello, Remote Console!");
    }

    public void OnErrorClicked()
    {
        Debug.LogError("Error. Hello, Remote Console!");
    }

    public void OnWarningClicked()
    {
        Debug.LogWarning("Warning. Hello, Remote Console!");
    }

    private void OnDestroy()
    {
        OnStopForwardClicked();
    }
}
