using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HitboxScript : MonoBehaviour
{
    private bool active;
    public List<PlayerScript> hitList = new List<PlayerScript>();
    PlayerScript tempPlayerS;

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.tag.Contains("player"))
        {
            tempPlayerS = collision.GetComponent<PlayerScript>();
            hitList.Add(tempPlayerS);
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.tag.Contains("player"))
        {
            tempPlayerS = collision.GetComponent<PlayerScript>();
            hitList.Remove(tempPlayerS);
        }
    }

    public void SetActive(bool b)
    {
        active = b;
    }

    public bool GetFilled() // used for AI
    {
        if (hitList.Count >= 1)
            return true;
        else
            return false;
    }

}
