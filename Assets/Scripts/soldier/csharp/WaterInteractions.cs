﻿using UnityEngine;
using System.Collections;

public class WaterInteractions : PausableBehaviour
{
    public Transform soldier;
    private SoldierController controller;

    private bool emitMovement;
    public Transform movementContainer;
    private ParticleSystem[] movementEmitters;

    private bool emitStand;
    public Transform standingContainer;
    private ParticleSystem[] standingEmitters;

    public float jumpHitDistance = 1.4f;
    public GameObject jumpParticle;

    private Transform thisT;

    public LayerMask affectedLayers;
    private RaycastHit hitInfo;
    private bool jumped;
    private bool emittedHit;
    private float jumpTimer;

    private float runSpeed;
    private float runStrafeSpeed;
    private float walkSpeed;
    private float walkStrafeSpeed;
    private float crouchRunSpeed;
    private float crouchRunStrafeSpeed;
    private float crouchWalkSpeed;
    private float crouchWalkStrafeSpeed;
    private float currentAmount;

    public float depthToReduceSpeed = 0.9f;
    public float speedUnderWater = 0.8f;

    public AudioClip waterImpactSound;
    public AudioClip waterJumpingSound;

    public float fadeSpeed = 0.6f;

    private Vector3 lastPositon;
    private Vector3 currentPosition;

    private AudioSource movementContainerAudio;

    void Start()
    {
        controller = soldier.GetComponent<SoldierController>();

        currentAmount = 1.0f;

        runSpeed = controller.runSpeed;
        runStrafeSpeed = controller.runStrafeSpeed;
        walkSpeed = controller.walkSpeed;
        walkStrafeSpeed = controller.walkStrafeSpeed;
        crouchRunSpeed = controller.crouchRunSpeed;
        crouchRunStrafeSpeed = controller.crouchRunStrafeSpeed;
        crouchWalkSpeed = controller.crouchWalkSpeed;
        crouchWalkStrafeSpeed = controller.crouchWalkStrafeSpeed;

        jumpTimer = 0.0f;
        emitMovement = false;
        jumped = false;
        int i;

        movementContainer.parent = null;
        movementContainerAudio = movementContainer.GetComponent<AudioSource>();
        movementContainerAudio.volume = 0.0f;

        movementEmitters = movementContainer.GetComponentsInChildren<ParticleSystem>();

        for (i = 0; i < movementEmitters.Length; i++)
        {
            movementEmitters[i].Stop();
        }

        emitStand = false;

        standingContainer.parent = null;

        standingEmitters = standingContainer.GetComponentsInChildren<ParticleSystem>();

        for (i = 0; i < standingEmitters.Length; i++)
        {
            standingEmitters[i].Stop();
        }

        thisT = transform;
    }

    void Update()
    {
        if (!soldier.gameObject.activeSelf) return;

        lastPositon = currentPosition;
        currentPosition = new Vector3(soldier.position.x, 0.0f, soldier.position.z);

        var dir = (currentPosition - lastPositon).normalized;

        thisT.position = soldier.position + new Vector3(0, 1.8f, 0);

        if (!IsPaused)
        {
            jumped = Input.GetButtonDown("Jump");
        }

        if (!controller.inAir)
        {
            jumpTimer = 0.0f;
            emittedHit = false;
        }
        else
        {
            jumpTimer += Time.deltaTime;
        }

        if (Physics.Raycast(thisT.position, -Vector3.up, out hitInfo, Mathf.Infinity, affectedLayers))
        {
            if (hitInfo.collider.tag == "water")
            {
                if (hitInfo.distance < depthToReduceSpeed)
                {
                    ChangeSpeed(speedUnderWater);
                }
                else
                {
                    ChangeSpeed(1.0f);
                }

                if (controller.inAir)
                {
                    if (hitInfo.distance < jumpHitDistance && !emittedHit && jumpTimer > 0.5)
                    {
                        emittedHit = true;
                        EmitJumpParticles(true, hitInfo);
                        ChangeMovementState(false);
                        ChangeStandingState(false);
                    }
                }
                else
                {
                    if (jumped)
                    {
                        EmitJumpParticles(false, hitInfo);
                        ChangeMovementState(false);
                        ChangeStandingState(false);
                    }
                    else if (!controller.inAir)
                    {
                        if (dir.magnitude > 0.2f)
                        {
                            movementContainer.position = hitInfo.point;
                            ChangeMovementState(true);
                            ChangeStandingState(false);
                        }
                        else
                        {
                            standingContainer.position = hitInfo.point;
                            ChangeMovementState(false);
                            ChangeStandingState(true);
                        }
                    }
                }
            }
            else
            {
                ChangeSpeed(1.0f);
                ChangeMovementState(false);
                ChangeStandingState(false);
            }
        }
        else
        {
            ChangeSpeed(1.0f);
            ChangeMovementState(false);
            ChangeStandingState(false);
        }

        if (emitMovement)
        {
            if (movementContainerAudio.volume < 0.65f)
            {
                if (!movementContainerAudio.isPlaying) movementContainerAudio.Play();

                movementContainerAudio.volume += Time.deltaTime * fadeSpeed;
            }
            else
            {
                movementContainerAudio.volume = 0.65f;
            }
        }
        else
        {
            if (movementContainerAudio.isPlaying)
            {
                if (movementContainerAudio.volume > 0.0)
                {
                    movementContainerAudio.volume -= Time.deltaTime * fadeSpeed * 2.0f;
                }
                else
                {
                    movementContainerAudio.Pause();
                }
            }
        }
    }

    void ChangeSpeed(float amount)
    {
        if (currentAmount == amount) return;

        currentAmount = amount;

        controller.runSpeed = runSpeed * amount;
        controller.runStrafeSpeed = runStrafeSpeed * amount;
        controller.walkSpeed = walkSpeed * amount;
        controller.walkStrafeSpeed = walkStrafeSpeed * amount;
        controller.crouchRunSpeed = crouchRunSpeed * amount;
        controller.crouchRunStrafeSpeed = crouchRunStrafeSpeed * amount;
        controller.crouchWalkSpeed = crouchWalkSpeed * amount;
        controller.crouchWalkStrafeSpeed = crouchWalkStrafeSpeed * amount;
    }

    void EmitJumpParticles(bool b, RaycastHit hitInfo)
    {
        var go = Instantiate(jumpParticle, hitInfo.point, Quaternion.identity);
        var goAudio = go.GetComponent<AudioSource>();

        if (goAudio != null)
        {
            if (b)
            {
                goAudio.PlayOneShot(waterImpactSound, 0.5f);
            }
            else
            {
                goAudio.PlayOneShot(waterJumpingSound, 1);
            }
        }

        ParticleSystem emitter;
        for (int i = 0; i < go.transform.childCount; i++)
        {
            emitter = go.transform.GetChild(i).GetComponent<ParticleSystem>();

            if (emitter == null) continue;

            emitter.Play();
        }

        AutoDestroy aux = go.AddComponent<AutoDestroy>();
        aux.time = 2f;
    }

    void ChangeMovementState(bool state)
    {
        if (state == emitMovement) return;

        emitMovement = state;

        for (int i = 0; i < movementEmitters.Length; i++)
        {
            if (state)
            {
                movementEmitters[i].Play();
            }
            else
            {
                movementEmitters[i].Stop();
            }
        }
    }

    void ChangeStandingState(bool state)
    {
        if (state == emitStand) return;

        emitStand = state;

        for (int i = 0; i < standingEmitters.Length; i++)
        {
            if (state)
            {
                movementEmitters[i].Play();
            }
            else
            {
                movementEmitters[i].Stop();
            }
        }
    }
}
