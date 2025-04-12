using UnityEngine;
using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

public class testSendTelemetry : MonoBehaviour
{
    byte[] motionPacket = new byte[60];
    byte[] gaugePacket = new byte[96];

    long lastTime;
    bool running;

    [Header("Server Settings")]
    public string serverIP = "localhost";
    public int GaugePort = 4444;
    public int MotionPort = 4445;
    public int targetSendRateHz = 333;
    public float currentUpdateRate;

    [Header("Truck References")]
    public Rigidbody truckRb;
    public suspensionLogic suspension;

    // Real-time stream values
    public float surgeAcc; // forward/back
    public float swayAcc;  // left/right
    public float heaveAcc; // up/down
    public float roll;     // tilt left/right (radians)
    public float pitch;    // tilt front/back (radians)
    public float speed;    // m/s
    public float rpm;      // fake rpm estimate

    private Vector3 lastVelocity;

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

    public void makePackage()
    {
        if (truckRb == null)
            return;

        // Get local space values
        Vector3 localVelocity = truckRb.transform.InverseTransformDirection(truckRb.velocity);
        Vector3 localAngularVelocity = truckRb.transform.InverseTransformDirection(truckRb.angularVelocity);

        Vector3 localAccel = truckRb.transform.InverseTransformDirection(truckRb.velocity - lastVelocity) / Time.fixedDeltaTime;
        lastVelocity = truckRb.velocity;

        swayAcc = localAccel.x;    // Lateral
        surgeAcc = localAccel.z;   // Longitudinal
        heaveAcc = localAccel.y;   // Vertical

        // Orientation
        roll = truckRb.rotation.eulerAngles.z * Mathf.Deg2Rad;
        pitch = truckRb.rotation.eulerAngles.x * Mathf.Deg2Rad;

        if (roll > Mathf.PI) roll -= 2 * Mathf.PI;
        if (pitch > Mathf.PI) pitch -= 2 * Mathf.PI;

        speed = truckRb.velocity.magnitude;
        rpm = speed * 60f; // Fake but useful

        // Fill motion packet
        int index;

        var byteData = BitConverter.GetBytes(swayAcc);
        index = 28; motionPacket[index++] = byteData[0]; motionPacket[index++] = byteData[1]; motionPacket[index++] = byteData[2]; motionPacket[index++] = byteData[3];

        byteData = BitConverter.GetBytes(surgeAcc);
        index = 32; motionPacket[index++] = byteData[0]; motionPacket[index++] = byteData[1]; motionPacket[index++] = byteData[2]; motionPacket[index++] = byteData[3];

        byteData = BitConverter.GetBytes(heaveAcc);
        index = 36; motionPacket[index++] = byteData[0]; motionPacket[index++] = byteData[1]; motionPacket[index++] = byteData[2]; motionPacket[index++] = byteData[3];

        byteData = BitConverter.GetBytes(roll);
        index = 52; motionPacket[index++] = byteData[0]; motionPacket[index++] = byteData[1]; motionPacket[index++] = byteData[2]; motionPacket[index++] = byteData[3];

        byteData = BitConverter.GetBytes(pitch);
        index = 56; motionPacket[index++] = byteData[0]; motionPacket[index++] = byteData[1]; motionPacket[index++] = byteData[2]; motionPacket[index++] = byteData[3];

        // Fill gauge packet
        byteData = BitConverter.GetBytes(speed);
        index = 12; gaugePacket[index++] = byteData[0]; gaugePacket[index++] = byteData[1]; gaugePacket[index++] = byteData[2]; gaugePacket[index++] = byteData[3];

        byteData = BitConverter.GetBytes(rpm);
        index = 16; gaugePacket[index++] = byteData[0]; gaugePacket[index++] = byteData[1]; gaugePacket[index++] = byteData[2]; gaugePacket[index++] = byteData[3];
    }

    public async void SendTelemetry()
    {
        long dtime = (DateTime.Now.Ticks - lastTime) / TimeSpan.TicksPerMillisecond;

        ct.ThrowIfCancellationRequested();
        if (ct.IsCancellationRequested)
            ct.ThrowIfCancellationRequested();

        if (dtime >= 950f / targetSendRateHz)
        {
            currentUpdateRate = (float)(1.0f / dtime * 1000);
            makePackage();

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
