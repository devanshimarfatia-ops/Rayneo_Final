using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Hands; // package com.unity.xr.hands

// Test for camera hand tracking on the RayNeo X3 Pro (OpenXR XR_EXT_hand_tracking, which the RayNeo
// runtime reports in its startup log). Put it on any always-active object (e.g. UIManager), build,
// and watch:  adb logcat -s Unity | Select-String "Hands"
//   "[Hands] no running XRHandSubsystem"  -> the OpenXR "Hand Tracking Subsystem" feature is off / unsupported
//   "[Hands] right tracked"               -> hands are seen
//   "[Hands] PINCH right"                 -> thumb + index tip touched; On Pinch fires
// On Pinch can be wired in the Inspector to anything (e.g. a test method) to try pinch as a "tap".
public class HandTrackingProbe : MonoBehaviour
{
    [Tooltip("Thumb tip and index tip closer than this (m) = pinch starts.")]
    public float pinchOnMeters = 0.02f;
    [Tooltip("Farther apart than this (m) = pinch released (bigger than On, so it does not flicker).")]
    public float pinchOffMeters = 0.035f;
    public UnityEvent onPinch;

    private static readonly List<XRHandSubsystem> found = new List<XRHandSubsystem>();
    private XRHandSubsystem hands;
    private readonly bool[] pinching = new bool[2];
    private readonly float[] nextTrackedLog = new float[2];
    private float nextMissingLog;

    void Update()
    {
        if (hands == null || !hands.running)
        {
            hands = null;
            SubsystemManager.GetSubsystems(found);
            foreach (var s in found) if (s.running) { hands = s; break; }
            if (hands == null)
            {
                if (Time.unscaledTime > nextMissingLog)
                {
                    nextMissingLog = Time.unscaledTime + 3f;
                    Debug.Log($"[Hands] no running XRHandSubsystem ({found.Count} found)");
                }
                return;
            }
            Debug.Log("[Hands] hand subsystem running");
        }

        Check(hands.leftHand, 0, "left");
        Check(hands.rightHand, 1, "right");
    }

    void Check(XRHand hand, int i, string side)
    {
        if (!hand.isTracked) { pinching[i] = false; return; }

        if (Time.unscaledTime > nextTrackedLog[i])
        {
            nextTrackedLog[i] = Time.unscaledTime + 2f;
            Debug.Log($"[Hands] {side} tracked");
        }

        // Distance only, so it does not matter which space the joint poses are in.
        if (!hand.GetJoint(XRHandJointID.ThumbTip).TryGetPose(out Pose thumb)) return;
        if (!hand.GetJoint(XRHandJointID.IndexTip).TryGetPose(out Pose index)) return;
        float d = Vector3.Distance(thumb.position, index.position);

        if (!pinching[i] && d < pinchOnMeters)
        {
            pinching[i] = true;
            Debug.Log($"[Hands] PINCH {side} ({d * 100f:F1} cm)");
            onPinch?.Invoke();
        }
        else if (pinching[i] && d > pinchOffMeters)
        {
            pinching[i] = false;
        }
    }
}