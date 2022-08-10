using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestGizmo : MonoBehaviour
{
    public enum KIND
    {
        START,
        END
    }

    public KIND m_gizmoKind;


    private void OnDrawGizmos()
    {
        if(m_gizmoKind == KIND.START)
        {
            Gizmos.color = Color.green;
        }
        else
        {
            Gizmos.color = Color.red;
        }
        
        Gizmos.DrawWireSphere(transform.position, 1.0f);
    }
}
