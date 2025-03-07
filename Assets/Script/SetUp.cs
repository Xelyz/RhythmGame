using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem.EnhancedTouch;
using UnityEngine.InputSystem;

public class SetUp : MonoBehaviour
{
    void Awake()
    {
        EnhancedTouchSupport.Enable();
        if (GravitySensor.current != null)
        {
            Values.accAvail = true;
        }

        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 60;
    }
}
