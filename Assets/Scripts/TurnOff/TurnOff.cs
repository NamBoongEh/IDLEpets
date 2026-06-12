using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;
using UnityEngine.Events;


public class TurnOff : MonoBehaviour
{
    UdpClient ReceivePort, SendPort;
    IPEndPoint remoteEndPoint;
    string[] protocol = { "tht_00", "tht_10", "tht_01", "tht_02", "tht_", "pow_01", "pow_02" };

    public class MyUDPEvent : UnityEvent<string> { }
    public MyUDPEvent OnReceiveMessage = new MyUDPEvent();

    string ip = "192.168.10.10";
    int port = 11116;

    [Serializable]
    class Data
    {
        public int port;
    }

    private UdpClient m_Receiver;
    string m_ReceiveMessage;

    private void Awake()
    {
        Load();
    }

    private void Start()
    {
        InitReceiver();
    }

    //메세지 보내기
    public void Send(string msg, int msgPort)
    {
        SendPort = new UdpClient();

        string remoteIP = ip;
        int remotePort = msgPort;
        remoteEndPoint = new IPEndPoint(IPAddress.Parse(remoteIP), remotePort);

        byte[] data = System.Text.Encoding.UTF8.GetBytes(msg);
        SendPort.Send(data, data.Length, remoteEndPoint);
    }

    public void InitReceiver()
    {
        try
        {
            if (m_Receiver == null)
            {
                m_Receiver = new UdpClient(port);
                m_Receiver.BeginReceive(new AsyncCallback(ReceiveCallback), null);
            }
        }
        catch (SocketException e)
        {
            Debug.Log("UDP exception : " + e.Message);
        }
    }

    void ReceiveCallback(IAsyncResult ar)
    {
        //IPEndPoint ipEndPoint = new IPEndPoint(IPAddress.Parse("192.168.0.22"), port);
        IPEndPoint ipEndPoint = new IPEndPoint(IPAddress.Any, port);

        byte[] received;
        if (m_Receiver != null)
        {
            received = m_Receiver.EndReceive(ar, ref ipEndPoint);
        }
        else
        {
            return;
        }

        m_Receiver.BeginReceive(new AsyncCallback(ReceiveCallback), null);

        m_ReceiveMessage = Encoding.Default.GetString(received);
        m_ReceiveMessage = m_ReceiveMessage.Trim();

            if (m_ReceiveMessage.Equals(protocol[5])) //pow_01 재부팅
                System.Diagnostics.Process.Start("shutdown.exe", "-r -t 1");
            if (m_ReceiveMessage.Equals(protocol[6])) //pow_02 아예 끄기 
                System.Diagnostics.Process.Start("shutdown.exe", "-s -t 1");
    }

    public void CloseReceiver()
    {
        if (m_Receiver != null)
        {
            m_Receiver.Close();
            m_Receiver = null;
        }
    }

    void OnApplicationQuit()
    {
        CloseReceiver();
    }

    void Load()
    {
        string FromJsonData = File.ReadAllText(Application.streamingAssetsPath + "/SaveFile/save.json");

        Data myData = JsonUtility.FromJson<Data>(FromJsonData);

        port = myData.port;
    }
}