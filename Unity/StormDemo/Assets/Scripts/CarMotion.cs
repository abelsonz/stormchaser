using UnityEngine;



public class CarMotion : MonoBehaviour

{

    public float speed = 4f;

    public float bumpiness = 0.3f;

    public float tiltIntensity = 10f;

    public float tiltFrequency = 1.5f;



    public Transform[] wheels;



    private Rigidbody rb;



    void Start()

    {

        rb = GetComponent<Rigidbody>();

        if (rb == null)

        {

            rb = gameObject.AddComponent<Rigidbody>();

        }



        rb.mass = 1200f;

        rb.drag = 0.1f;

        rb.angularDrag = 0.05f;

    }



    void Update()

    {



        float move = speed * Time.deltaTime;

        transform.Translate(Vector3.forward * move);





        float bump = Mathf.Sin(Time.time * 2f) * bumpiness;

        Vector3 bumpEffect = new Vector3(0, bump, 0);

        transform.position += bumpEffect * Time.deltaTime;





        float sideTilt = Mathf.Sin(Time.time * tiltFrequency) * tiltIntensity;

        transform.rotation = Quaternion.Euler(0f, transform.rotation.eulerAngles.y, sideTilt);





        UpdateWheelRotation(move);



    }



    void UpdateWheelRotation(float move)

    {

        foreach (Transform wheel in wheels)

        {



            wheel.Rotate(Vector3.right, (move * 360f) / (2f * Mathf.PI * 0.35f));





            wheel.rotation = Quaternion.Euler(0, wheel.rotation.eulerAngles.y, 0);

        }

    }

}