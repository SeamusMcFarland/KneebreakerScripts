using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

public class PlayerScript : MonoBehaviour
{
    private bool jPress, lPress, rPress, sPress; // jump, left, right, special
    private bool rPressPriority;
    public enum CharState { grounded, jumping, tripping, airborne, missing, striking, struck, heavyHitstun, lightHitstun, bouncing, dead };
    public CharState ownState;

    private int inputScheme;
    private int charType;

    private float specialCharge;
    private float specialRequirement;

    private float acceleration_g, acceleration_a;
    private float maxSpeed_g, maxSpeed_a;

    private float minJump, maxJump;
    private float jumpStrength; // vertical velocity
    private float xJump; // horizontal velocity
    private float horizontalRate; // rate of charging xJump
    private float verticalRate; // rate of charging jumpStrength
    private CharState savedJumpState; // recovers transitioning state previous to jump state

    private bool attacking;
    private float attackSpin;
    private HitboxScript hitboxS;
    private bool extendHitbox;
    private const float SHAKE_TIME = 0.2f;
    private const float SHAKE_RANGE = 0.15f;
    private Vector2 originalPosition;
    private Vector2 savedVV; // velocity vector
    private int groundedFrames; // prevents problems from immediately attacking after jumping
    private bool usedAttack; // prevents 1-frame double hits by the AI by disabling the ability to attack twice in one jump
    Vector2 differenceVector;

    public float launchModifier;
    private const float HEAVY_BREAK_SPEED = 4f; // speed needed to drop from heavy to light hitstun state
    public PhysicsMaterial2D noBounce, yesBounce;
    int heavyFrames;

    private const float LIGHT_HITSTUN_MOVE_MODIFIER = 0.2f;

    private Rigidbody2D rb;
    private SpriteRenderer sr;

    public Sprite neutral_n, kick_n, pull_n, zap_n, zap_n2;
    public Sprite neutral_h, kick_h, pull_h, zap_h, zap_h2;
    public Sprite neutral_b, kick_b, pull_b, zap_b, zap_b2;


    private float tripTimer;

    private bool touchingGround;
    private bool ceiling;

    bool spinDirection;
    const float STRUCK_SPIN = 5f;

    List<SpriteRenderer> strikeSR = new List<SpriteRenderer>();

    GameManagerScript gms;

    public bool isAI;
    PlayerScript[] otherPS;
    PlayerScript selectedPS;
    float jumpAI; // time AI spends charging jump
    float tiltAI; // time AI spends tilting a direction while charging jump
    float groundedAI; // time passed since AI has been grounded 
    const float AI_AIM_DISTANCE = 1f;

    private float trapCooldown;

    float frameOffset;

    bool electrified;

    int characterType;

    // Start is called before the first frame update
    void Start()
    {
        characterType = 1;

        frameOffset = 250f * Time.deltaTime;

        switch (this.tag)
        {
            case "player1":
                inputScheme = 1;
                break;

            case "player2":
                inputScheme = 2;
                break;

            case "player3":
                inputScheme = 3;
                break;

            case "player4":
                inputScheme = 4;
                break;

            default:
                print("INVALID INPUT SCHEME AT START!");
                break;
        }

        otherPS = FindObjectsOfType(typeof(PlayerScript)) as PlayerScript[];

        StrikeEffectScript[] teAr = GetComponentsInChildren<StrikeEffectScript>();
        foreach (StrikeEffectScript ses in teAr)
            strikeSR.Add(ses.gameObject.GetComponent<SpriteRenderer>());
        foreach (SpriteRenderer sre in strikeSR)
            sre.enabled = false;

        gms = GameObject.FindGameObjectWithTag("GameManager").GetComponent<GameManagerScript>();

        charType = 1;

        touchingGround = false;
        hitboxS = GetComponentInChildren<HitboxScript>();
        hitboxS.SetActive(false);

        switch (charType)
        {
            case 1:
                specialRequirement = 10f;
                acceleration_g = 0.4f;
                acceleration_a = 0.05f;
                maxSpeed_g = 4f;
                maxSpeed_a = 2f;
                minJump = 7f;
                maxJump = 15f;
                horizontalRate = 0.7f;
                verticalRate = 10f;
                attackSpin = 2f;
                launchModifier = 40f;
                break;
        }

        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();

        ownState = CharState.airborne;
    }

    public void SetAI(bool b)
    {
        isAI = b;
    }

    // Update is called once per frame
    void Update()
    {
        frameOffset = 250f*Time.deltaTime;

        if (ownState != CharState.struck)
            trapCooldown -= Time.deltaTime;

        if ((ownState == CharState.airborne || ownState == CharState.missing) && touchingGround && !ceiling)
            ownState = CharState.grounded;

        if (ownState == CharState.heavyHitstun || ownState == CharState.lightHitstun || ownState == CharState.struck)
        {
            if(spinDirection)
                transform.eulerAngles = new Vector3(transform.eulerAngles.x, transform.eulerAngles.y, transform.eulerAngles.z + STRUCK_SPIN * frameOffset);
            else
                transform.eulerAngles = new Vector3(transform.eulerAngles.x, transform.eulerAngles.y, transform.eulerAngles.z - STRUCK_SPIN * frameOffset);
        }

        CheckInputs();

        if (ownState != CharState.airborne)
            attacking = false;

        switch (ownState)
        {
            case CharState.grounded:
                Grounded();
                break;
            case CharState.airborne:
                Airborne();
                break;
            case CharState.jumping:
                Jumping();
                break;
            case CharState.tripping:
                Tripping();
                break;
            case CharState.missing:
                Missing();
                break;
            case CharState.striking:
                Striking();
                break;
            case CharState.struck:
                Struck();
                break;
            case CharState.heavyHitstun:
                HeavyHitstun();
                break;
            case CharState.lightHitstun:
                LightHitstun();
                break;
            case CharState.bouncing:
                Bouncing();
                break;
            case CharState.dead:
                Dead();
                break;
        }
    }

    private void Grounded()
    {
        usedAttack = false;

        if (!touchingGround)
            ownState = CharState.airborne;

        if (groundedFrames <= 0)
            SwitchToNeutral();

        if (5f < transform.eulerAngles.z && transform.eulerAngles.z < 180f)
            transform.localScale = new Vector2(-3f, transform.localScale.y);
        else if (180 <= transform.eulerAngles.z && transform.eulerAngles.z < 355f)
            transform.localScale = new Vector2(3f, transform.localScale.y);

        if (rPressPriority)
        {
            if (rPress)
            {
                MoveRight();
            }
            else if (lPress)
            {
                MoveLeft();
            }
            else
                transform.eulerAngles = new Vector3(transform.eulerAngles.x, transform.eulerAngles.y, 0);
        }
        else
        {
            if (lPress)
            {
                MoveLeft();
            }
            else if (rPress)
            {
                MoveRight();
            }
            else
                transform.eulerAngles = new Vector3(transform.eulerAngles.x, transform.eulerAngles.y, 0);
        }

        if (jPress)
        {
            jumpStrength = minJump;
            xJump = 0;
            transform.eulerAngles = new Vector3(transform.eulerAngles.x, transform.eulerAngles.y, 0f);
            savedJumpState = ownState;
            ownState = CharState.jumping;
        }
        else if (sPress && specialCharge >= specialRequirement)
            Special();
    }

    private void Airborne()
    {
        if (groundedFrames <= -60 && sr.sprite == kick_n)
            SwitchToNeutral();

        if (rPressPriority)
        {
            if (rPress)
            {
                MoveRight();
            }
            else if (lPress)
            {
                MoveLeft();
            }
        }
        else
        {
            if (lPress)
            {
                MoveLeft();
            }
            else if (rPress)
            {
                MoveRight();
            }
        }


        if (!attacking && jPress && groundedFrames <= 0 && !usedAttack)
            StartAttack();

        groundedFrames--;

        if (attacking)
        {
            if (jPress)
            {
                CheckAttackSpin();
            }
            else
            {
                Attack();
            }

        }

        if (sPress && specialCharge >= specialRequirement)
            Special();
    }

    private void CheckAttackSpin()
    {
        if (rPressPriority)
        {
            if (rPress)
            {
                transform.eulerAngles = new Vector3(transform.eulerAngles.x, transform.eulerAngles.y, transform.eulerAngles.z - attackSpin * frameOffset);
            }
            else if (lPress)
            {
                transform.eulerAngles = new Vector3(transform.eulerAngles.x, transform.eulerAngles.y, transform.eulerAngles.z + attackSpin * frameOffset);
            }
        }
        else
        {
            if (lPress)
            {
                transform.eulerAngles = new Vector3(transform.eulerAngles.x, transform.eulerAngles.y, transform.eulerAngles.z + attackSpin * frameOffset);
            }
            else if (rPress)
            {
                transform.eulerAngles = new Vector3(transform.eulerAngles.x, transform.eulerAngles.y, transform.eulerAngles.z - attackSpin * frameOffset);
            }
        }
        /*if (lPress)
            transform.eulerAngles = new Vector3(transform.eulerAngles.x, transform.eulerAngles.y, transform.eulerAngles.z - attackSpin);
        else
            transform.eulerAngles = new Vector3(transform.eulerAngles.x, transform.eulerAngles.y, transform.eulerAngles.z + attackSpin);*/
    }

    private void StartAttack()
    {
        usedAttack = true;
        attacking = true;
        SwitchToPull();
    }

    private void Attack()
    {
        hitboxS.SetActive(true);
        SwitchToPull();
        ownState = CharState.missing;
        StartCoroutine("DisableAttack");
    }

    IEnumerator DisableAttack()
    {
        yield return new WaitForSeconds(0.1f);
        if (!extendHitbox) // play miss sound if missing
            gms.PlaySFX("swish");
        yield return new WaitForSeconds(0.2f);
        if (extendHitbox)
            yield return new WaitForSeconds(SHAKE_TIME);
        extendHitbox = false; // will reach this reguardless of changed states
        hitboxS.SetActive(false);
        if (ownState == CharState.missing || ownState == CharState.striking) // realistically it should be impossible to be striking at this time
            ownState = CharState.airborne;
    }

    private void MoveLeft()
    {
        if (ownState == CharState.grounded)
            transform.eulerAngles = new Vector3(transform.eulerAngles.x, transform.eulerAngles.y, 30f);
        switch (ownState)
        {
            case CharState.grounded:
                if (rb.velocity.x > -maxSpeed_g)
                    rb.velocity = new Vector2(rb.velocity.x - acceleration_g * frameOffset, rb.velocity.y);
                break;

            case CharState.airborne:
                if (rb.velocity.x > -maxSpeed_a)
                    rb.velocity = new Vector2(rb.velocity.x - acceleration_a * frameOffset, rb.velocity.y);
                break;

            case CharState.jumping:
                if (rb.velocity.x > -maxSpeed_g * 0.7f)
                    rb.velocity = new Vector2(rb.velocity.x - acceleration_g * frameOffset, rb.velocity.y);
                break;

            case CharState.lightHitstun:
                if (rb.velocity.x > -maxSpeed_a)
                    rb.velocity = new Vector2(rb.velocity.x - acceleration_a * LIGHT_HITSTUN_MOVE_MODIFIER * frameOffset, rb.velocity.y);
                break;
        }
    }

    private void MoveRight()
    {
        if(ownState == CharState.grounded)
            transform.eulerAngles = new Vector3(transform.eulerAngles.x, transform.eulerAngles.y, -30f);
        switch (ownState)
        {
            case CharState.grounded:
                if (rb.velocity.x < maxSpeed_g)
                    rb.velocity = new Vector2(rb.velocity.x + acceleration_g * frameOffset, rb.velocity.y);
                break;

            case CharState.airborne:
                if (rb.velocity.x < maxSpeed_a)
                    rb.velocity = new Vector2(rb.velocity.x + acceleration_a * frameOffset, rb.velocity.y);
                break;

            case CharState.jumping:
                if (rb.velocity.x < maxSpeed_g * 0.7f)
                    rb.velocity = new Vector2(rb.velocity.x + acceleration_g * frameOffset, rb.velocity.y);
                break;

            case CharState.lightHitstun:
                if (rb.velocity.x < maxSpeed_a)
                    rb.velocity = new Vector2(rb.velocity.x + acceleration_a * LIGHT_HITSTUN_MOVE_MODIFIER * frameOffset, rb.velocity.y);
                break;
        }
    }

    private void Special()
    {

    }


    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Environment"))
        {
            touchingGround = true;
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Environment"))
        {
            touchingGround = false;

            groundedFrames = 3;
        }
    }

    private void Jumping()
    {
        SwitchToPull();

        rb.velocity = new Vector2(0, 0);

        if (transform.eulerAngles.z < 180f)
            transform.localScale = new Vector2(-3f, transform.localScale.y);
        else
            transform.localScale = new Vector2(3f, transform.localScale.y);

        HorizontalJump();

        if (!jPress)
            Jump();
        else if (jumpStrength < maxJump)
            jumpStrength += ((maxJump - minJump) * verticalRate) * frameOffset / 1000f;

        if (40f < transform.eulerAngles.z && transform.eulerAngles.z < 320f)
        {
            ownState = CharState.tripping;
            Trip();
        }
    }

    private void HorizontalJump() // handles tilt angle and horizontal velocity after jump
    {
        if (rPressPriority)
        {
            if (rPress)
            {
                transform.eulerAngles = new Vector3(transform.eulerAngles.x, transform.eulerAngles.y, transform.eulerAngles.z - 0.2f * frameOffset);
                xJump += (horizontalRate * maxJump * frameOffset) / Mathf.Pow(jumpStrength, 2f);
            }
            else if (lPress)
            {
                transform.eulerAngles = new Vector3(transform.eulerAngles.x, transform.eulerAngles.y, transform.eulerAngles.z + 0.2f * frameOffset);
                xJump -= (horizontalRate * maxJump * frameOffset) / Mathf.Pow(jumpStrength, 2f);
            }
        }
        else
        {
            if (lPress)
            {
                transform.eulerAngles = new Vector3(transform.eulerAngles.x, transform.eulerAngles.y, transform.eulerAngles.z + 0.2f * frameOffset);
                xJump -= (horizontalRate * maxJump) / Mathf.Pow(jumpStrength, 2f);
            }
            else if (rPress)
            {
                transform.eulerAngles = new Vector3(transform.eulerAngles.x, transform.eulerAngles.y, transform.eulerAngles.z - 0.2f * frameOffset);
                xJump += (horizontalRate * maxJump) / Mathf.Pow(jumpStrength, 2f);
            }
        }
    }

    private void Jump()
    {
        groundedFrames = 3;
        SwitchToKick();
        ownState = CharState.airborne;
        rb.velocity = new Vector2(xJump, jumpStrength);
        //StartCoroutine("DelayJumpStateChange");
    }

    /*IEnumerator DelayJumpStateChange()
    {
        yield return new WaitForSeconds(0.01f);
        print("State: " + ownState + " at: " + Time.time);
        ownState = savedJumpState;
    }*/

    private void Tripping()
    {
        SwitchToNeutral();

        if (tripTimer < 0)
            ownState = CharState.grounded;
        else
            tripTimer -= Time.deltaTime;
    }

    private void Trip()
    {
        gms.PlaySFX("slap");
        SwitchToNeutral();
        tripTimer = 1f;
        if (xJump < 0) // tripping to the left
        {
            transform.localScale = new Vector2(-3f, transform.localScale.y);
            rb.velocity = new Vector2(-13f, 0f);
            transform.eulerAngles = new Vector3(transform.eulerAngles.x, transform.eulerAngles.y, 110f);
        }
        else
        {
            transform.localScale = new Vector2(3f, transform.localScale.y);
            rb.velocity = new Vector2(13f, 0f);
            transform.eulerAngles = new Vector3(transform.eulerAngles.x, transform.eulerAngles.y, 250f);
        }
    }

    private void Missing()
    {
        SwitchToKick();
        if (hitboxS.hitList.Count != 0)
        {
            bool succeed = false;
            foreach (PlayerScript hs in hitboxS.hitList)
            {
                if (hs.ownState != CharState.striking && hs.ownState != CharState.struck)
                {
                    succeed = true;
                    hs.FirstStruck(this, 0);
                }
            }
            if (succeed)
            {
                ownState = CharState.striking;
                FirstStriking();
            }
        }
    }

    private void FirstStriking()
    {
        foreach (SpriteRenderer sre in strikeSR)
            sre.enabled = true;
        originalPosition = transform.position;
        extendHitbox = true;
        savedVV = rb.velocity;
        StartCoroutine("EndStriking");
    }

    IEnumerator EndStriking()
    {
        yield return new WaitForSeconds(SHAKE_TIME);
        if (ownState == CharState.striking)
        {
            ownState = CharState.airborne;
            if (savedVV.x > 0 && savedVV.y > 0)
            {
                float xvv = savedVV.x / Vector2.Distance(savedVV, new Vector2(0, 0));
                float yvv = savedVV.y / Vector2.Distance(savedVV, new Vector2(0, 0));
                rb.velocity = new Vector2(-xvv * 2f, -yvv * 2f);
            }
            transform.position = originalPosition;
        }
        foreach (SpriteRenderer sre in strikeSR)
            sre.enabled = false;

    }

    private void Striking()
    {
        transform.position = new Vector2(originalPosition.x + Random.Range(-SHAKE_RANGE/5f, SHAKE_RANGE/5f), originalPosition.y + Random.Range(-SHAKE_RANGE/5f, SHAKE_RANGE/5f));
        SwitchToKick();
        rb.velocity = new Vector2(0,0);
    }

    public void FirstStruck(PlayerScript ps, int type) // type is for special strikes, 0 is normal, 1 is electrified
    {
        gms.PlaySFX("impact");

        switch (this.tag)
        {
            case "player1":
                gms.DecreaseLife(1);
                if (gms.GetLife(1) <= 0)
                    Death();
                break;

            case "player2":
                gms.DecreaseLife(2);
                if (gms.GetLife(2) <= 0)
                    Death();
                break;

            case "player3":
                gms.DecreaseLife(3);
                if (gms.GetLife(3) <= 0)
                    Death();
                break;

            case "player4":
                gms.DecreaseLife(4);
                if (gms.GetLife(4) <= 0)
                    Death();
                break;

            default:
                print("INVALID PLAYER TAG FOR FIRST STRUCK! tag is: " + this.tag);
                break;
        }            

        if (Random.value > 0.5f)
            spinDirection = !spinDirection;
        originalPosition = transform.position;
        hitboxS.SetActive(false);
        ownState = CharState.struck;

        switch (type)
        {
            case 0: // normal
                electrified = false;
                break;
            case 1: // electric
                electrified = true;
                break;
        }

        if(this.gameObject.activeSelf == true)
            StartCoroutine("EndStruck", ps);
    }

    private void Struck()
    {
        print("struck called: " + Time.time);
        transform.position = new Vector2(originalPosition.x + Random.Range(-SHAKE_RANGE, SHAKE_RANGE), originalPosition.y + Random.Range(-SHAKE_RANGE, SHAKE_RANGE));
        if (electrified)
        {
            if (Random.value < 0.8f)
                SwitchToZap();
            else
                SwitchToZap2();
        }
        else
            SwitchToNeutral();
        rb.velocity = new Vector2(0, 0);
    }

    IEnumerator EndStruck(PlayerScript ps)
    {
        yield return new WaitForSeconds(SHAKE_TIME);
        FirstHeavyHitstun(ps);
    }

    private void HeavyHitstun()
    {
        if (heavyFrames <= 0)
            FirstLightHitstun();
        else
            heavyFrames--;
    }

    private void FirstHeavyHitstun(PlayerScript ps)
    {
        SwitchToNeutral();
        GetComponent<CapsuleCollider2D>().sharedMaterial = yesBounce;
        heavyFrames = 240;
        ownState = CharState.heavyHitstun;
        if (ps == this) // PlayerScript is set to own playerscript if hit by a trap
            differenceVector = new Vector2(Random.Range(-1f,1f), Random.Range(-1f, 1f));
        else
            differenceVector = new Vector2(transform.position.x - ps.transform.position.x, transform.position.y - ps.transform.position.y);
        rb.velocity = ps.launchModifier * differenceVector / Vector2.Distance(Vector2.zero, differenceVector);
        StartCoroutine("RepeatPhysics", ps);
        StartCoroutine("RemoveBounce");
    }

    IEnumerator RepeatPhysics(PlayerScript ps)
    {
        yield return new WaitForSeconds(0.05f);
        rb.velocity = ps.launchModifier * differenceVector / Vector2.Distance(Vector2.zero, differenceVector);
    }

    IEnumerator RemoveBounce()
    {
        yield return new WaitForSeconds(0.05f);
        if(ownState == CharState.airborne || ownState == CharState.grounded || ownState == CharState.jumping)
            GetComponent<CapsuleCollider2D>().sharedMaterial = noBounce;
        else
            StartCoroutine("RemoveBounce");
    }

    private void LightHitstun()
    {
        if (touchingGround)
            ownState = CharState.grounded;
    }

    private void FirstLightHitstun()
    {
        ownState = CharState.lightHitstun;
    }

    private void Bouncing()
    {

    }

    private void Dead()
    {

    }

    //includes AI input
    private void CheckInputs()
    {
        if (isAI)
        {
            AIInputScheme();
        }
        else
        {
            switch (inputScheme)
            {
                case 1:
                    InputScheme1();
                    break;
                case 2:
                    InputScheme2();
                    break;
                case 3:
                    InputScheme3();
                    break;
                case 4:
                    InputScheme4();
                    break;
                default:
                    print("ERROR! INVALID INPUT SCHEME");
                    break;
            }
        }
    }

    private void AIInputScheme()
    {
        if (otherPS[0] != this) // exludes self from default
            selectedPS = otherPS[0];
        else
            selectedPS = otherPS[1];
        foreach (PlayerScript ps in otherPS) // selects closest player to target
        {
            if (ps != this) // excludes self
            {
                if (Vector2.Distance(this.transform.position, selectedPS.transform.position) > Vector2.Distance(this.transform.position, ps.transform.position))
                    selectedPS = ps;
            }
        }
        if (ownState == CharState.grounded || ownState == CharState.jumping)
        {
            if (groundedAI == 0) // if first frame to be grounded
            {
                jumpAI = Random.Range(0.1f, 1f);
                tiltAI = Random.Range(0.1f, 0.5f);
            }

            groundedAI += Time.deltaTime;

            AITilt();
            AIJump(); // handles both height and release

            sPress = false;

        }
        else if (ownState == CharState.airborne) // immediately readies an attack and begins aiming at the player
        {
            if (hitboxS.GetFilled())
                jPress = false; // triggers attack()
            else
                jPress = true;

            AIAim();

            groundedAI = 0;
            sPress = false;


        }
        else
        {
            groundedAI = 0;
            jPress = false;
            rPress = false;
            lPress = false;
            sPress = false;
        }
    }

    private void AITilt()
    {
        if (groundedAI <= tiltAI)
        {
            if (selectedPS.transform.position.x < this.transform.position.x) // if target player to left of self
            {
                rPress = false;
                lPress = true;
            }
            else
            {
                rPress = true;
                lPress = false;
            }
        }
        else
        {
            rPress = false;
            lPress = false;
        }

    }

    private void AIJump()
    {
        if (groundedAI <= jumpAI)
            jPress = true;
        else
            jPress = false;
    }

    private void AIAim()
    {
        if (Vector2.Distance(transform.position, selectedPS.transform.position) < AI_AIM_DISTANCE)
        {
            float angle = Mathf.Atan2(selectedPS.transform.position.y - transform.position.y, selectedPS.transform.position.x - transform.position.x) * Mathf.Rad2Deg;
            if (angle + 90f < 0)
            {
                rPress = false;
                lPress = true;

            }
            else
            {
                rPress = true;
                lPress = false;
            }
        }
    }

    private void InputScheme1()
    {
        if (Input.GetKeyDown(KeyCode.W))
            jPress = true;
        if (Input.GetKeyUp(KeyCode.W))
            jPress = false;
        if (Input.GetKeyDown(KeyCode.A))
        {
            if (!rPress)
                rPressPriority = false;
            lPress = true;
        }
        if (Input.GetKeyUp(KeyCode.A))
            lPress = false;
        if (Input.GetKeyDown(KeyCode.D))
        {
            if (!lPress)
                rPressPriority = true;
            rPress = true;
        }
        if (Input.GetKeyUp(KeyCode.D))
            rPress = false;
        if (Input.GetKeyDown(KeyCode.S))
            sPress = true;
        if (Input.GetKeyUp(KeyCode.S))
            sPress = false;

    }

    private void InputScheme2()
    {
        if (Input.GetKeyDown(KeyCode.UpArrow))
            jPress = true;
        if (Input.GetKeyUp(KeyCode.UpArrow))
            jPress = false;
        if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            if (!rPress)
                rPressPriority = false;
            lPress = true;
        }
        if (Input.GetKeyUp(KeyCode.LeftArrow))
            lPress = false;
        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            if (!lPress)
                rPressPriority = true;
            rPress = true;
        }
        if (Input.GetKeyUp(KeyCode.RightArrow))
            rPress = false;
        if (Input.GetKeyDown(KeyCode.DownArrow))
            sPress = true;
        if (Input.GetKeyUp(KeyCode.DownArrow))
            sPress = false;

    }

    private void InputScheme3()
    {
        if (Input.GetKeyDown(KeyCode.I))
            jPress = true;
        if (Input.GetKeyUp(KeyCode.I))
            jPress = false;
        if (Input.GetKeyDown(KeyCode.J))
        {
            if (!rPress)
                rPressPriority = false;
            lPress = true;
        }
        if (Input.GetKeyUp(KeyCode.J))
            lPress = false;
        if (Input.GetKeyDown(KeyCode.L))
        {
            if (!lPress)
                rPressPriority = true;
            rPress = true;
        }
        if (Input.GetKeyUp(KeyCode.L))
            rPress = false;
        if (Input.GetKeyDown(KeyCode.K))
            sPress = true;
        if (Input.GetKeyUp(KeyCode.K))
            sPress = false;

    }

    private void InputScheme4()
    {
        if (Input.GetKeyDown(KeyCode.Alpha8))
            jPress = true;
        if (Input.GetKeyUp(KeyCode.Alpha8))
            jPress = false;
        if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            if (!rPress)
                rPressPriority = false;
            lPress = true;
        }
        if (Input.GetKeyUp(KeyCode.Alpha4))
            lPress = false;
        if (Input.GetKeyDown(KeyCode.Alpha6))
        {
            if (!lPress)
                rPressPriority = true;
            rPress = true;
        }
        if (Input.GetKeyUp(KeyCode.Alpha6))
            rPress = false;
        if (Input.GetKeyDown(KeyCode.Alpha5))
            sPress = true;
        if (Input.GetKeyUp(KeyCode.Alpha5))
            sPress = false;

    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Ceiling"))
            ceiling = true;
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Ceiling"))
            ceiling = false;
    }

    public void Death()
    {
        this.gameObject.SetActive(false);
    }

    public CharState GetState()
    {
        return ownState;
    }

    public void TrapCooldownReset()
    {
        trapCooldown = 0.15f;
    }

    public float GetTrapCooldown()
    {
        return trapCooldown;
    }

    private void SwitchToNeutral()
    {
        switch (characterType)
        {
            case 1:
                sr.sprite = neutral_n;
                break;
            case 2:
                sr.sprite = neutral_h;
                break;
            case 3:
                sr.sprite = neutral_b;
                break;
        }
    }

    private void SwitchToKick()
    {
        switch (characterType)
        {
            case 1:
                sr.sprite = kick_n;
                break;
            case 2:
                sr.sprite = kick_h;
                break;
            case 3:
                sr.sprite = kick_b;
                break;
        }
    }

    private void SwitchToPull()
    {
        switch (characterType)
        {
            case 1:
                sr.sprite = pull_n;
                break;
            case 2:
                sr.sprite = pull_h;
                break;
            case 3:
                sr.sprite = pull_b;
                break;
        }
    }

    private void SwitchToZap()
    {
        switch (characterType)
        {
            case 1:
                sr.sprite = zap_n;
                break;
            case 2:
                sr.sprite = zap_h;
                break;
            case 3:
                sr.sprite = zap_b;
                break;
        }
    }

    private void SwitchToZap2()
    {
        switch (characterType)
        {
            case 1:
                sr.sprite = zap_n2;
                break;
            case 2:
                sr.sprite = zap_h2;
                break;
            case 3:
                sr.sprite = zap_b2;
                break;
        }
    }

    /*private void SteadySelf()
    {
        if (ownState == CharState.grounded)
        {
            if (Mathf.Abs(transform.rotation.z) > 0.02f)
                if (transform.rotation.z < 0)
                    rb.angularVelocity += 1500f * Mathf.Pow(transform.rotation.z, 2);
                else
                    rb.angularVelocity -= 1500f * Mathf.Pow(transform.rotation.z, 2);
        }
        else // jumping state
        {
            if (Mathf.Abs(transform.rotation.z) > 0.02f)
                if (transform.rotation.z < 0)
                    rb.angularVelocity -= 1000f * Mathf.Pow(transform.rotation.z, 2);
                else
                    rb.angularVelocity += 1000f * Mathf.Pow(transform.rotation.z, 2);
        }
    }*/

}
