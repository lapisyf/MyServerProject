using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MainManager : MonoBehaviour
{
    private NetWorkManager m_network = new NetWorkManager();


    private void Awake()
    {
        Screen.SetResolution(900, 600, false);
    }
    public FollowCam followCam;
    public GameObject playerUnit;
    public GameObject enemyUnit;
    public Dictionary<int, GameObject> players = new Dictionary<int, GameObject>();
    public Dictionary<int, GameObject> enemys = new Dictionary<int, GameObject>();

    #region 플레이어 컨트롤 변수
    public Player m_mainPlayer;
    float X;
    float Z;
    bool jumpDown;
    public Vector3 m_moveVector;

    public float m_moveSpeed;
    public float m_rotateSpeed;
    public float m_jumpPower;
    public PacketMoveData m_mainPlayerData;
    #endregion

    #region 플레이어 컨트롤 함수
    void GetInput()
    {
        X = Input.GetAxis("Horizontal");
        Z = Input.GetAxis("Vertical");
        jumpDown = Input.GetKeyDown(KeyCode.Space);
    }
    void Move()
    {
        m_moveVector = new Vector3(X, 0, Z).normalized;
        m_mainPlayer.transform.position += m_moveVector * m_moveSpeed * Time.deltaTime;
    }
    void Turn()
    {
        m_mainPlayer.transform.LookAt(m_mainPlayer.transform.position + m_moveVector);
    }
    void Jump()
    {
        if (jumpDown && !m_mainPlayer.isJump)
        {
            m_mainPlayer.m_rigidbody.AddForce(Vector3.up * m_jumpPower, ForceMode.Impulse);
            m_mainPlayer.isJump = true;
        }
    }

    // 임시 적 소환
    void TestEnemySpawn()
    {
        if (Input.GetKeyDown(KeyCode.T))
        {
            EnemySpawnAmountData l_SpawnAmountData;
            l_SpawnAmountData.m_spawnAmount = 1;
            l_SpawnAmountData.m_hp = 1;

            m_network.Session.Write((int)E_PROTOCOL.CTS_ENEMYSPAWN, l_SpawnAmountData);
        }
    }
    #endregion

    void Start()
    {
        m_moveSpeed = 10;
        m_rotateSpeed = 30;
        m_jumpPower = 10;

        m_network.Register(E_PROTOCOL.STC_SPAWN, SpawnProcess);
        m_network.Register(E_PROTOCOL.STC_MOVE, MoveProcess);
        m_network.Register(E_PROTOCOL.STC_OUT, OutProcess);

        m_network.Register(E_PROTOCOL.STC_ENEMYSPAWN, EnemySpawnProcess);
        m_network.Register(E_PROTOCOL.STC_ENEMYMOVE, EnemyMoveProcess);
        m_network.Register(E_PROTOCOL.STC_ENEMYOUT, EnemyOutProcess);
        m_network.Initialize();
    }
    void Update()
    {
        #region 플레이어 컨트롤러
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            m_network.End();
            //m_network.protocolThread.Join();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
                Application.Quit(); // 어플리케이션 종료
#endif
        }
        if (m_mainPlayer != null)
        {
            GetInput();
            Move();
            Turn();
            Jump();

            // test (후에는 트리거 충돌시 스폰)
            TestEnemySpawn();
        }
        
        #endregion

       
    }


    const int m_SendTimeCounterF = 3;
    int m_sendTimeCounter = 0;
    private void FixedUpdate()
    {
        if (m_mainPlayer != null)
        {
            if (m_SendTimeCounterF <= m_sendTimeCounter)
            {
                m_mainPlayer.PositionAndRotationWrite(ref m_mainPlayerData);
                m_network.Session.Write((int)E_PROTOCOL.CTS_MOVE, m_mainPlayerData);
                m_sendTimeCounter = 0;
            }
            else
            {
                ++m_sendTimeCounter;
            }
        }
        #region 메세지 처리 루프
        m_network.UpdateRecvProcess();
        #endregion
    }

    /*=================<기능 함수(TEST용 -> 클라에 적용시에 각자 해당하는 메니저에서 유사한 기능의 함수를 제작하는 것이 좋음)>=====================*/
    void SpawnProcess()
    {
        ListData liddata;
        m_network.Session.GetData<ListData>(out liddata);

        for (int i = 0; i < liddata.m_size; i++)
        {
            if (liddata.m_list[i] == -1)
            {
                continue;
            }
            if (!players.ContainsKey(liddata.m_list[i]))
            {
                GameObject temp = GameObject.Instantiate(playerUnit);
                temp.GetComponent<Player>().moveData.m_id = liddata.m_list[i];
                players.Add(liddata.m_list[i], temp);
                temp.SetActive(true);
                if (liddata.m_list[i] != m_network.ClientId)
                {
                    temp.GetComponent<Rigidbody>().useGravity = true;
                }
                else
                {
                    temp.GetComponent<Rigidbody>().useGravity = true;
                }
                
            }
        }
        if(m_mainPlayer == null)
        {
            m_mainPlayerData.m_id = m_network.ClientId;

            m_mainPlayerData.m_state = 5;
            m_mainPlayerData.m_move.x = 15;
            m_mainPlayerData.m_move.y = 25;
            m_mainPlayerData.m_animing = 35;

            m_mainPlayer = players[m_network.ClientId].GetComponent<Player>();
            followCam.target = m_mainPlayer.transform;
        }
    }
    void MoveProcess()
    {
        PacketMoveData lData;
        m_network.Session.GetData<PacketMoveData>(out lData);
        if (players.ContainsKey(lData.m_id))
        {
            if (lData.m_id != m_network.ClientId)
            {
                players[lData.m_id].GetComponent<Player>().PositionAndRotationRead(lData);
            }
        }
    }
    void OutProcess()
    {
        IDData liddata;
        m_network.Session.GetData<IDData>(out liddata);
        Destroy(players[liddata.m_id]);
        players.Remove(liddata.m_id);
    }


    void EnemySpawnProcess()
    {
        EnemySpawnData lenemySpawnData;
        m_network.Session.GetData<EnemySpawnData>(out lenemySpawnData);

        for (int i = 0; i < lenemySpawnData.m_size; i++)
        {
            if (lenemySpawnData.m_list[i] == -1)
            {
                continue;
            }
            if (!enemys.ContainsKey(lenemySpawnData.m_list[i]))
            {
                GameObject temp = GameObject.Instantiate(enemyUnit);
                Enemy enemy = temp.GetComponent<Enemy>();

                enemy.m_enemyData.m_moveData.m_id = lenemySpawnData.m_list[i];
                enemy.m_enemyData.m_hp = lenemySpawnData.m_hp;
                enemy.AIFlag = lenemySpawnData.m_isHost ? true : false;
                enemy.NetWork = m_network;
                enemy.MainManager = GetComponent<MainManager>();

                enemys.Add(lenemySpawnData.m_list[i], temp);
                temp.SetActive(true);
            }
        }
    }

    void EnemyMoveProcess()
    {
        PacketMoveData lData;
        m_network.Session.GetData<PacketMoveData>(out lData);

        enemys[lData.m_id].GetComponent<Enemy>().m_enemyData.m_moveData = lData;
        enemys[lData.m_id].transform.position
            = new Vector3(lData.m_position.x, lData.m_position.y, lData.m_position.z);
    }

    void EnemyOutProcess()
    {
        IDData liddata;
        m_network.Session.GetData<IDData>(out liddata);
        Destroy(enemys[liddata.m_id]);
        enemys.Remove(liddata.m_id);
    }


    //해당 함수는 Enemy정보를 관리하는 Manager에 들어가있어야함.
    public void EnemyDestroy(Enemy _enemy)
    {
        _enemy.m_enemyData.m_hp -= 1;

        if (_enemy.m_enemyData.m_hp <= 0)
        {
            IDData liddata;
            liddata.m_id = _enemy.m_enemyData.m_moveData.m_id;
            m_network.Session.Write((int)E_PROTOCOL.CTS_ENEMYOUT, liddata);

            Destroy(_enemy.gameObject);
            enemys.Remove(liddata.m_id);
        }
    }

    /*======================================*/
}
