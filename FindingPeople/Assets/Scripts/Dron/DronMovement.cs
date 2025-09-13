﻿using UnityEngine;

//Function for moving the drone in 2 dimensions
public class DronMovement : MonoBehaviour
{
    public DronManager dronManager;
    public float moveSpeed = 0f;
    public float maxSpeed = 30f;
    public float acceleration = 10f;
    public float rotationSpeed = 2f;
    public float rotationThreshold = 1f;

    private float resetDuration = 2f;
    private float resetTimer = 0f;
    private bool resettingRotation = false;

    public bool isGoingToTarget = false;

    void Update()
    {
        // In case the drone is going to the target person, it changes the objective if the person is found by other agent
        if (DronesManager.isTaregt) 
        {
            if (!isGoingToTarget) 
            {
                dronManager.setTargetPos(DronesManager.targetpos);
            }
            isGoingToTarget = true;
        }

        // When it reaches the position of the grid it resets the rotation to cover the whole detection area
        if (resettingRotation)
        {
            resetTimer += Time.deltaTime;

            Quaternion normalRotation = Quaternion.Euler(0, 0, 0);
            transform.rotation = Quaternion.Slerp(transform.rotation, normalRotation, rotationSpeed * Time.deltaTime);

            moveSpeed = 0f;

            if (resetTimer >= resetDuration)
            {
                resettingRotation = false;
            }
            return;
        }

        Vector3 currentPos = transform.position;
        Vector3 target = new Vector3(dronManager.targetPos.x, currentPos.y, dronManager.targetPos.z);
        Vector3 direction = (target - currentPos).normalized;

        if (direction == Vector3.zero) return;

        // Rotates toward the target
        Quaternion targetRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);

        float angleDifference = Quaternion.Angle(transform.rotation, targetRotation);

        // If the angle difference is small enough, it starts moving toward the target
        if (angleDifference < rotationThreshold)
        {
            moveSpeed = Mathf.Min(maxSpeed, moveSpeed + acceleration * Time.deltaTime);

            if (Vector3.Distance(currentPos, target) > 0.1f)
            {
                // Moves towards the target
                transform.position = Vector3.MoveTowards(currentPos, target, moveSpeed * Time.deltaTime);
            }
            else
            {
                // Once it reach it it resets
                resettingRotation = true;
                resetTimer = 0f;
                moveSpeed = 0f; 
                dronManager.OnReachedTarget();

                //In case it reaches the objective person
                if (isGoingToTarget) 
                {
                    dronManager.startLanding();
                }
            }
        }
    }
}