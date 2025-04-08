using UnityEngine;
using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;


public class sendTelemetry : MonoBehaviour
{
    byte[] motionPacket = new byte[60];
    byte[] gaugePacket = new byte[96]; //20
    long lastTime;
    bool running; //used to stop thread

    //server settings
    public String serverIP = "localhost";
    public int GaugePort = 4444;
    public int MotionPort= 4445;
    public int targetSendRateHz = 333;
    public float currentUpdateRate;


    public float angle = 0; //for simulation

    //stream data 
    public float surgeAcc; // meters per second squared - positive sway when turning right
    public float swayAcc;  //meters per second squared - positive surge during forward acceleration
    public float heaveAcc; //meters per second squared - positive when hitting a bump and vehicle rises
    public float roll;    //radians positive roll occurs when turning left
    public float pitch; //radians positive pitch when climbing or forward acc
    public float speed;  //vehicle speed 0.89408 or higher meters per second
    public float rpm; //engine rpm

    // Start is called before the first frame update
    UdpClient udpClient = new UdpClient();
    CancellationTokenSource tokenSource2 = new CancellationTokenSource();
    CancellationToken ct;

    public void Start()
    {
        ct = tokenSource2.Token;

        Task.Run(() => { while (true) SendTelemetry(); }, tokenSource2.Token); 
    }

    public void OnDestroy()
    {
        Debug.Log("Closing thread");
        tokenSource2.Cancel();      

    }

    public void makePackage(float a)
    {

        surgeAcc = 3*5.62f*Mathf.Sin(a); 
        swayAcc = 3*1.34f * Mathf.Sin(a);  
        heaveAcc = 9.8f * Mathf.Sin(a); 
        roll = 0;   
        pitch = 0; 
        speed = (100.0f / 3.6f) * Mathf.Sin(a);  //+- 100 km/h
        rpm = 4.0f * Mathf.Sin(a) + 4.0f; 

        var byteData = BitConverter.GetBytes(swayAcc);
        var index = 28; //offset
        motionPacket[index++] = byteData[0];
        motionPacket[index++] = byteData[1];
        motionPacket[index++] = byteData[2];
        motionPacket[index++] = byteData[3];

        byteData = BitConverter.GetBytes(surgeAcc);
        index = 32; //offset
        motionPacket[index++] = byteData[0];
        motionPacket[index++] = byteData[1];
        motionPacket[index++] = byteData[2];
        motionPacket[index++] = byteData[3];


        byteData = BitConverter.GetBytes(heaveAcc);
        index = 36; //offset
        motionPacket[index++] = byteData[0];
        motionPacket[index++] = byteData[1];
        motionPacket[index++] = byteData[2];
        motionPacket[index++] = byteData[3];

        byteData = BitConverter.GetBytes(roll);
        index = 52; //offset
        motionPacket[index++] = byteData[0];
        motionPacket[index++] = byteData[1];
        motionPacket[index++] = byteData[2];
        motionPacket[index++] = byteData[3];

        byteData = BitConverter.GetBytes(pitch);
        index = 56; //offset
        motionPacket[index++] = byteData[0];
        motionPacket[index++] = byteData[1];
        motionPacket[index++] = byteData[2];
        motionPacket[index++] = byteData[3];


        byteData = BitConverter.GetBytes(speed);
        index = 12; //offset
        gaugePacket[index++] = byteData[0];
        gaugePacket[index++] = byteData[1];
        gaugePacket[index++] = byteData[2];
        gaugePacket[index++] = byteData[3];

        byteData = BitConverter.GetBytes(rpm);
        index = 16; //offset
        gaugePacket[index++] = byteData[0];
        gaugePacket[index++] = byteData[1];
        gaugePacket[index++] = byteData[2];
        gaugePacket[index++] = byteData[3];

    }

    // Update is called once per frame
    void Update()
    {

    }

    public async void SendTelemetry()
    {

        long dtime = (DateTime.Now.Ticks - lastTime)/ TimeSpan.TicksPerMillisecond;

        ct.ThrowIfCancellationRequested();
        if (ct.IsCancellationRequested)
        {
            // Clean up here, then...
            ct.ThrowIfCancellationRequested();
        }

        if (dtime >= 950f / targetSendRateHz)
        {
            angle += 0.01f;
            currentUpdateRate = (float)(1.0f / dtime*1000);
          

            makePackage(angle);
            try
            {
                udpClient.Send(gaugePacket, 96, serverIP, GaugePort);
            }
            catch (Exception e)
            {
                Debug.LogError(e.ToString());
            }

            try
            {
                udpClient.Send(motionPacket, 60, serverIP, MotionPort);
            }
            catch (Exception e)
            {
                Debug.LogError(e.ToString());
            }

            lastTime = DateTime.Now.Ticks;

        }

        await Task.Yield();
    }

    
}
