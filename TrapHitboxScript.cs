using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static PlayerScript;

public class TrapHitboxScript : MonoBehaviour
{

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
            if (tempPlayerS.GetState() != CharState.struck && tempPlayerS.GetTrapCooldown() <= 0) // makes sure not already hit
            {
                tempPlayerS.FirstStruck(tempPlayerS, 1);
                tempPlayerS.TrapCooldownReset(); // prevents multi-hits
        }
        }
    }
}
