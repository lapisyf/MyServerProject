using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enemy : MonoBehaviour
{
    #region 변수
    // 어차피 정욱이형 코드에서는 NetWorkManager 싱글톤이기 떄문에, 이럴필요 없음
    private NetWorkManager  m_network;

    private bool            m_aiFlag = false;
    private Transform       m_target;

    public Transform        m_startPoint;
    public Transform        m_endPoint;

    public EnemyData        m_enemyData;
    public float            m_speed;
    private float           m_elpasedTime = 0;      // 경과시간
    #endregion

    #region 프로퍼티
    public NetWorkManager NetWork { get => m_network; set => m_network = value; }
    public bool AIFlag { get => m_aiFlag; set => m_aiFlag = value; }
    #endregion

    private void Start()
    {
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
        m_elpasedTime += Time.deltaTime;       
        if(m_elpasedTime >= 0.1f)
        {
            m_elpasedTime = 0.0f;
            m_network.Session.Write((int)E_PROTOCOL.CTS_ENEMYMOVE, m_enemyData.m_moveData);

        }

        transform.position = Vector3.MoveTowards(transform.position, m_target.position, Time.deltaTime * m_speed);

        m_enemyData.m_moveData.m_position.x = transform.position.x;
        m_enemyData.m_moveData.m_position.y = transform.position.y;
        m_enemyData.m_moveData.m_position.z = transform.position.z;

        //m_enemyData.m_rotation.x = transform.rotation.x;
        //m_enemyData.m_rotation.y = transform.rotation.y;
        //m_enemyData.m_rotation.z = transform.rotation.z;
        //m_enemyData.m_rotation.w = transform.rotation.w;

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
            m_enemyData.m_hp -= 1;

            if(m_enemyData.m_hp <=0)
            {
                IDData l_id;
                l_id.m_id = m_enemyData.m_moveData.m_id;
                m_network.Session.Write((int)E_PROTOCOL.CTS_ENEMYOUT, l_id);

                Destroy(gameObject);
            }
        }
    }

}
