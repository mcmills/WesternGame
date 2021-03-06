﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using UnityEngine.UI;

public class MyPlayer : MonoBehaviourPun, IPunObservable
{
    public float MoveSpeed = 3f;
    public float smoothRotationTime = 0.25f;
    public float JumpForce;
    public bool fire;
    public float waitCrossHair = 3f;
    bool crossHairStatus;


    //Sound
    public AudioSource shootSound;
    public AudioSource runSound;


    ParticleSystem muzzle;
    Animator anim;
    Transform cameraTransform;


    public Transform rayOrigin;


    //Health
    public GameObject healthBar;
    public GameObject crossHair;
    public Image fillImage;
    public float playerHealth = 1f;
    public float damage = 0.01f;


    float currentSpeed;
    float speedVelocity;
    float currentVeclocity;


    private void Awake()
    {
        // It sets the player health to full
        playerHealth = 1f;
        // If this is my player
        if (photonView.IsMine)
        {
            // It finds an object named MainCamera
            cameraTransform = GameObject.Find("ThirdPersonCamera").transform;
        }
        // It finds an object named GunMuzzle in other object names SciFiRifle(Clone) and It gets the PartucleSystem component
        muzzle = rayOrigin.Find("SciFiRifle(Clone)/GunMuzzle").GetComponent<ParticleSystem>();
    }


    private void Start()
    {
        crossHairStatus = false;
        // It disables the crossHair
        crossHair.SetActive(false);
        // If this is my character
        if (photonView.IsMine)
        {
            // It sets the fire bool to false
            fire = false;
            // It gets the Animator component from this object
            anim = GetComponent<Animator>();
            // It makes the healthBar visible
            healthBar.SetActive(true);
        }
        // If this is not my character
        else
        {
            // It disables the BetterJump component
            GetComponent<BetterJump>().enabled = false;
        } 
    }


    void Update()
    {
        // If this is my character
        if (photonView.IsMine)
        {
            // It makes this method run
            LocalPlayerUpdate();
        }
    }


    void LocalPlayerUpdate()
    {
        // It get the input of the space bar
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Jump();
        }
        // It creates a variable names input
        Vector2 input = Vector2.zero;
        // It checks if the player is using a phone or a pc 
        input = Myinputs(input);
        // It creates a variable names inputDir
        Vector2 inputDir = input.normalized;
        // It rotates the player with the camera
        RotateWCamera(inputDir);
        // It is firing
        if (crossHairStatus)
        {
            // It makes the character rotate with the camera 
            float rotation = Mathf.Atan2(inputDir.x, inputDir.y) * Mathf.Rad2Deg + cameraTransform.eulerAngles.y;
            // It rotates the character
            transform.eulerAngles = Vector3.up * rotation;
        }
        // It creates a targetSpeed with a given value (MoveSpeed) by inputDir magnitude
        float tragetSpeed = MoveSpeed * inputDir.magnitude;
        // It smooths the speed
        currentSpeed = Mathf.SmoothDamp(currentSpeed, tragetSpeed, ref speedVelocity, 0.1f);
        // It controls the Running Animation
        RunningAnim(inputDir);
        // It is not firing
        if (!fire)
        {
            // It moves the character
            transform.Translate(transform.forward * currentSpeed * Time.deltaTime, Space.World);
        }
        // It controls the Fire States
        FireManager();
    }

    void FireManager()
    {
        // It checks if the player press the Left Click
        if (Input.GetButtonDown("Fire2") || Input.GetButtonDown("Fire1"))
        {
            if (Input.GetButtonDown("Fire1"))
            {
                // It calls the Fire method
                Fire();
                fire = true;
            }
            else if (Input.GetButtonDown("Fire2"))
            {
                // It enables the crossHair
                crossHairStatus = true;
                crossHair.SetActive(crossHairStatus);
            }
            
        }
        // It checks if the player release the Left Click
        else if (Input.GetButtonUp("Fire2") || Input.GetButtonUp("Fire1"))
        {
            fire = false;
            // It calls the FireUp method
            FireUp();
            if (Input.GetButtonUp("Fire2"))
            {
                // It disables the crossHair
                crossHairStatus = false;
                crossHair.SetActive(crossHairStatus);
            }
        }
    }


    // It plays the MuzzleFlash
    public void MuzzleFlash()
    {
        muzzle.Play();
    }


    // It is call when the player wants to fire
    public void Fire()
    {
        // It start the TimerCrossHair coroutine
        StartCoroutine(TimerCrossHair());
        // Start the Fire animation
        anim.SetTrigger("Fire");
        // Structure used to get information back from a raycast
        RaycastHit hit;
        // It creates a ray
        if(Physics.Raycast(rayOrigin.position, cameraTransform.forward, out hit, 25f))
        {
            // It gets the PhotonView component from the object hit
            PhotonView pv = hit.transform.GetComponent<PhotonView>();
            // It true when pv isn't null, the character isn't mine and the object hit has the Player tag
            if (pv != null && !hit.transform.GetComponent<PhotonView>().IsMine && hit.transform.tag == "Player")
            {
                // It damages the other character
                hit.transform.GetComponent<PhotonView>().RPC("GetDamage", RpcTarget.AllBuffered, damage);
            }
        }
        // It plays the Shoot Sound
        shootSound.Play();
        // The MuzzleFlash method starts
        MuzzleFlash();
    }


    // It is call when the character stops firing 
    public void FireUp()
    {
        // It stops tje muzzle
        muzzle.Stop();
    }


    // It is call when the player wants to Jump
    public void Jump()
    {
        // It starts the Jump Animation
        anim.SetTrigger("Jump");
        // It gets the Rigidbody component
        Rigidbody rb = GetComponent<Rigidbody>();
        // It sets the velocity to 0
        rb.velocity = Vector3.zero;
        // It sets the angular Velocity to 0
        rb.angularVelocity = Vector3.zero;
        // It adds Force to the character up
        rb.AddForce(Vector3.up * JumpForce, ForceMode.Impulse);
    }


    // This method is call remotely when the character gets hurt
    [PunRPC]
    public void GetDamage(float amount)
    {
        // It sets the amount value minus the playerHealth value
        playerHealth -= amount;
        // If this is my character
        if (photonView.IsMine)
        {
            // It sets the playerHealth value to the fillAmount value
            fillImage.fillAmount = playerHealth;
            // If playerHealth value is less than O
            if(playerHealth <= 0f)
            {
                // Die
                Die();
            }
        }
    }


    // This method synchronized variables that constantly change
    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        // If we are sending information
        if (stream.IsWriting)
        {
            // It sends the fire value
            stream.SendNext(fire);
        }
        // If it isn't the local player, it means it is the server
        else
        {
            // If fire is true
            if ((bool)stream.ReceiveNext())
            {
                // It calls the MuzzleFlash method
                MuzzleFlash();
            }
            // If fire is false
            else
            {
                // It calls the FireUp method
                FireUp();
            }
        }
    }


    // The player dies
    void Die()
    {
        // If this is my character
        if (photonView.IsMine)
        {
            // It calls the LeaveRoom method
            GameManager.instance.LeaveRoom();
        }
    }


    // It checks if the player is using a phone or a pc
    Vector2 Myinputs(Vector2 input)
    {
        // It sets the GetAzisRaw values to input variable
        input = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        return input;
    }


    // It rotates the player with the camera
    void RotateWCamera(Vector2 inputDirRWC)
    {
        // inputDir is not zero, it makes sure that the character rotation follows the camera rotation if it's moving
        if (inputDirRWC != Vector2.zero)
        {
            // It makes the character rotate with the camera
            float rotation = Mathf.Atan2(inputDirRWC.x, inputDirRWC.y) * Mathf.Rad2Deg + cameraTransform.eulerAngles.y;
            // It smooths the character rotation
            transform.eulerAngles = Vector3.up * Mathf.SmoothDampAngle(transform.eulerAngles.y, rotation, ref currentVeclocity, smoothRotationTime);
            // The runSound is not playing
            if (!runSound.isPlaying)
            {
                // It plays the runSound
                runSound.Play();
            }
        }
        // inputDir is zero
        else
        {
            // It stops the runSound
            runSound.Stop();
        }
    }


    // It controls the Running Animation
    void RunningAnim(Vector2 inputDirRuAni)
    {
        // The inputDir magnitude is greater than 0
        if (inputDirRuAni.magnitude > 0f)
        {
            // It sets the Running animation to true
            anim.SetBool("Running", true);
        }
        // The inputDir magnitude is equal than 0
        else if (inputDirRuAni.magnitude == 0f)
        {
            // It sets the Running animation to false
            anim.SetBool("Running", false);
        }
    }
    // It makes the CrossHair enable for few seconds
    private IEnumerator TimerCrossHair()
    {
        crossHairStatus = true;
        crossHair.SetActive(crossHairStatus);
        yield return new WaitForSeconds(waitCrossHair);
        crossHairStatus = false;
        crossHair.SetActive(crossHairStatus);
    }
}
