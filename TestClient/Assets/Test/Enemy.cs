using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enemy : MonoBehaviour
{
    private const int       m_SendTimeCounterF = 3;

    #region 변수
    // 어차피 정욱이형 코드에서는 Manager가 싱글톤이기 떄문에, 이럴필요 없음
    private NetWorkManager  m_network;
    private MainManager     m_main;

    private bool            m_aiFlag = false;
    private Transform       m_target;
    private int             m_sendTimeCounter = 0;

    public Transform        m_startPoint;
    public Transform        m_endPoint;

    public EnemyData        m_enemyData;
    public float            m_speed;
    #endregion

    #region 프로퍼티
    public NetWorkManager NetWork { get => m_network; set => m_network = value; }
    public MainManager MainManager { get => m_main; set => m_main = value; }
    public bool AIFlag { get => m_aiFlag; set => m_aiFlag = value; }
    #endregion

    private void Start()
    {
        transform.position = m_startPoint.position;
        m_target = m_endPoint;
    }

    private void Update()
    {
        if(m_aiFlag)
        {
            EnemyPositionUpdate();
        }
    }

    
    private void EnemyPositionUpdate()
    {
        m_sendTimeCounter++;
        if (m_sendTimeCounter >= m_SendTimeCounterF)
        {
            m_sendTimeCounter = 0;
            m_network.Session.Write((int)E_PROTOCOL.CTS_ENEMYMOVE, m_enemyData.m_moveData);
        }

        transform.position = Vector3.MoveTowards(transform.position, m_target.position, Time.deltaTime * m_speed);

        m_enemyData.m_moveData.m_position.x = transform.position.x;
        m_enemyData.m_moveData.m_position.y = transform.position.y;
        m_enemyData.m_moveData.m_position.z = transform.position.z;

        if (transform.position == m_startPoint.position)
        {
            m_target = m_endPoint;
        }
        else if (transform.position == m_endPoint.position)
        {
            m_target = m_startPoint;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if(other.gameObject.layer == LayerMask.NameToLayer("Player"))
        {
            m_main.EnemyDestroy(transform.GetComponent<Enemy>());
        }
    }

}
