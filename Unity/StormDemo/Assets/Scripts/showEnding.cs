using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class showEnding : MonoBehaviour
{
    public GameObject happyPoster;
    public GameObject sadPoster;

    private void Awake()
    {
        happyPoster.SetActive(false);
        sadPoster.SetActive(false);
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    public void ShowPoster(bool happy)
    {
        if (happy)
        {
            happyPoster.SetActive(true);
            happyPoster.transform.position = this.transform.position + this.transform.forward;
            happyPoster.transform.LookAt(this.transform.position + 2*this.transform.forward);
        }
        else
        {
            sadPoster.SetActive(true);
            sadPoster.transform.position = this.transform.position + this.transform.forward;
            sadPoster.transform.LookAt(this.transform.position + 2*this.transform.forward);
        }

    }



    // Update is called once per frame
    void Update()
    {
        
    }
}
