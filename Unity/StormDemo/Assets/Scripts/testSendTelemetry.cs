using UnityEngine;
using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

public class sendTelemetry : MonoBehaviour
{
    // Telemetry packets to be sent via UDP.
    byte[] motionPacket = new byte[60];
    byte[] gaugePacket = new byte[96];
    long lastTime;

    // Server settings (set these in the Inspector as needed)
    public string serverIP = "localhost";
    public int GaugePort = 4444;
    public int MotionPort = 4445;
    public int targetSendRateHz = 333;
    public float currentUpdateRate;

    // Telemetry values (using Unity's standard units)
    public float surgeAcc; // forward/backward acceleration (m/s²)
    public float swayAcc;  // lateral acceleration (m/s²)
    public float heaveAcc; // vertical acceleration (m/s²)
    public float roll;     // roll angle in radians
    public float pitch;    // pitch angle in radians
    public float speed;    // vehicle speed (m/s)
    public float rpm;      // simulated engine RPM

    // References – using the suspensionLogic script on your truck.
    public suspensionLogic truckSuspension;
    private Rigidbody truckRigidbody;
    private Vector3 lastVelocity;

    UdpClient udpClient = new UdpClient();
    CancellationTokenSource tokenSource2 = new CancellationTokenSource();
    CancellationToken ct;

    void Start()
    {
        ct = tokenSource2.Token;

        // Auto-assign the suspensionLogic component if not assigned.
        if (truckSuspension == null)
            truckSuspension = GetComponent<suspensionLogic>();

        // Since the Rigidbody is on the same GameObject as the suspensionLogic,
        // get it from that component.
        if (truckSuspension != null)
            truckRigidbody = truckSuspension.GetComponent<Rigidbody>();

        if (truckRigidbody != null)
            lastVelocity = truckRigidbody.velocity;

        // Initialize lastTime so that our delta timing is valid.
        lastTime = DateTime.Now.Ticks;

        // Begin sending telemetry on a background thread.
        Task.Run(() =>
        {
            while (true)
                SendTelemetry();
        }, tokenSource2.Token);
    }

    void FixedUpdate()
    {
        // Perform physics sampling on the main thread.
        makePackage();
    }

    void OnDestroy()
    {
        Debug.Log("Closing telemetry thread");
        tokenSource2.Cancel();
    }

    /// <summary>
    /// Computes telemetry values from physics data and packages them into UDP packets.
    /// This method should run on the main thread.
    /// </summary>
    void makePackage()
    {
        // Compute acceleration using the truck's Rigidbody.
        if (truckRigidbody != null)
        {
            Vector3 currentVelocity = truckRigidbody.velocity;
            float dt = Time.deltaTime;
            Vector3 worldAcceleration = (currentVelocity - lastVelocity) / dt;
            lastVelocity = currentVelocity;

            // Convert acceleration to the truck's local coordinate system.
            Vector3 localAcc = transform.InverseTransformDirection(worldAcceleration);
            swayAcc = localAcc.x;
            surgeAcc = localAcc.z;
            heaveAcc = localAcc.y;

            // Update speed.
            speed = currentVelocity.magnitude;
        }
        else
        {
            speed = 0;
            surgeAcc = swayAcc = heaveAcc = 0;
        }

        // Calculate pitch and roll from the truck's rotation.
        pitch = Mathf.Deg2Rad * Mathf.DeltaAngle(transform.eulerAngles.x, 0);
        roll = Mathf.Deg2Rad * Mathf.DeltaAngle(transform.eulerAngles.z, 0);

        // Simulate engine RPM based on speed.
        rpm = Mathf.Clamp(speed * 1000f, 0, 6000f);

        byte[] byteData;
        int index;

        // Write swayAcc into motionPacket at offset 28.
        byteData = BitConverter.GetBytes(swayAcc);
        index = 28;
        Array.Copy(byteData, 0, motionPacket, index, 4);

        // Write surgeAcc into motionPacket at offset 32.
        byteData = BitConverter.GetBytes(surgeAcc);
        index = 32;
        Array.Copy(byteData, 0, motionPacket, index, 4);

        // Write heaveAcc into motionPacket at offset 36.
        byteData = BitConverter.GetBytes(heaveAcc);
        index = 36;
        Array.Copy(byteData, 0, motionPacket, index, 4);

        // Write roll into motionPacket at offset 52.
        byteData = BitConverter.GetBytes(roll);
        index = 52;
        Array.Copy(byteData, 0, motionPacket, index, 4);

        // Write pitch into motionPacket at offset 56.
        byteData = BitConverter.GetBytes(pitch);
        index = 56;
        Array.Copy(byteData, 0, motionPacket, index, 4);

        // Write speed into gaugePacket at offset 12.
        byteData = BitConverter.GetBytes(speed);
        index = 12;
        Array.Copy(byteData, 0, gaugePacket, index, 4);

        // Write rpm into gaugePacket at offset 16.
        byteData = BitConverter.GetBytes(rpm);
        index = 16;
        Array.Copy(byteData, 0, gaugePacket, index, 4);
    }

    /// <summary>
    /// Sends the telemetry packets via UDP. This method runs on a background thread.
    /// </summary>
    public async void SendTelemetry()
    {
        long dtime = (DateTime.Now.Ticks - lastTime) / TimeSpan.TicksPerMillisecond;
        ct.ThrowIfCancellationRequested();

        if (dtime >= 950f / targetSendRateHz)
        {
            currentUpdateRate = (float)(1.0f / dtime * 1000);

            try
            {
                udpClient.Send(gaugePacket, gaugePacket.Length, serverIP, GaugePort);
            }
            catch (Exception e)
            {
                Debug.LogError(e.ToString());
            }

            try
            {
                udpClient.Send(motionPacket, motionPacket.Length, serverIP, MotionPort);
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
