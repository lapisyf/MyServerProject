using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MainManager : MonoBehaviour
{
    private NetWorkManager m_network = new NetWorkManager();
    private void Awake()
    {
        Screen.SetResolution(600, 900, false);
    }
    public GameObject playerUnit;
    public GameObject enemyUnit;
    public Dictionary<int, GameObject> players = new Dictionary<int, GameObject>();
    public Dictionary<int, GameObject> enemys = new Dictionary<int, GameObject>();
    public float m_moveSpeed;
    public float m_rotateSpeed;
    private Player m_mainPlayer;
    public PacketMoveData m_mainPlayerData;
    // Start is called before the first frame update
    void Start()
    {
        m_moveSpeed = 2;
        m_rotateSpeed = 30;

        m_network.Register(E_PROTOCOL.STC_SPAWN, SpawnProcess);
        m_network.Register(E_PROTOCOL.STC_MOVE, MoveProcess);
        m_network.Register(E_PROTOCOL.STC_OUT, OutProcess);
        m_network.Register(E_PROTOCOL.STC_ENEMYSPAWN, EnemySpawnProcess);
        m_network.Register(E_PROTOCOL.STC_ENEMYMOVE, EnemyMoveProcess);
        m_network.Register(E_PROTOCOL.STC_ENEMYOUT, EnemyOutProcess);
        m_network.Initialize();
    }

    // Update is called once per frame
    void Update()
    {
        bool l_isMove = false;
        
        if (Input.GetKey(KeyCode.LeftArrow))
        {
            m_mainPlayerData.m_position.x -= Time.deltaTime * m_moveSpeed;
            l_isMove = true;
        }
        if (Input.GetKey(KeyCode.RightArrow))
        {
            m_mainPlayerData.m_position.x += Time.deltaTime * m_moveSpeed;
            l_isMove = true;
        }
        if (Input.GetKey(KeyCode.DownArrow))
        {
            m_mainPlayerData.m_position.y -= Time.deltaTime * m_moveSpeed;
            l_isMove = true;
        }
        if (Input.GetKey(KeyCode.UpArrow))
        {
            m_mainPlayerData.m_position.y += Time.deltaTime * m_moveSpeed;
            l_isMove = true;
        }
        if(Input.GetKeyDown(KeyCode.Space))
        {
            //m_mainPlayer.GetComponent<R>()
        }
        if (Input.GetKey(KeyCode.Q))
        {
            transform.Rotate(Time.deltaTime * new Vector3(0, -m_rotateSpeed, 0));

            m_mainPlayerData.m_rotation.x = transform.rotation.x;
            m_mainPlayerData.m_rotation.y = transform.rotation.y;
            m_mainPlayerData.m_rotation.z = transform.rotation.z;
            m_mainPlayerData.m_rotation.w = transform.rotation.w;
            l_isMove = true;
        }
        if (Input.GetKey(KeyCode.E))
        {
            transform.Rotate(Time.deltaTime * new Vector3(0, m_rotateSpeed, 0));

            m_mainPlayerData.m_rotation.x = transform.rotation.x;
            m_mainPlayerData.m_rotation.y = transform.rotation.y;
            m_mainPlayerData.m_rotation.z = transform.rotation.z;
            m_mainPlayerData.m_rotation.w = transform.rotation.w;
            l_isMove = true;
        }
        if (l_isMove)
        {
            m_network.Session.Write((int)E_PROTOCOL.CTS_MOVE, m_mainPlayerData);
        }

        // 임시 몬스터 소환키 (후에는 트리거 충돌시 소환하도록)
        if(Input.GetKeyDown(KeyCode.T))
        {
            EnemySpawnAmountData l_SpawnAmountData;
            l_SpawnAmountData.m_spawnAmount = 1;
            l_SpawnAmountData.m_hp = 1;

            m_network.Session.Write((int)E_PROTOCOL.CTS_ENEMYSPAWN, l_SpawnAmountData);
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            m_network.End();
            #if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
            #else
                Application.Quit(); // 어플리케이션 종료
            #endif
        }

        m_network.UpdateRecvProcess();
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
            bool flag = true;
            foreach (int id in players.Keys)
            {
                if (id == liddata.m_list[i])
                {
                    flag = false;
                }
            }
            if (flag)
            {
                GameObject temp = GameObject.Instantiate(playerUnit);
                temp.GetComponent<Player>().moveData.m_id = liddata.m_list[i];
                players.Add(liddata.m_list[i], temp);
                temp.SetActive(true);
            }
        }

        m_mainPlayer = players[m_network.ClientId].GetComponent<Player>();
        m_mainPlayerData = players[m_network.ClientId].GetComponent<Player>().moveData;
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
            bool flag = true;
            foreach (int id in enemys.Keys)
            {
                if (id == lenemySpawnData.m_list[i])
                {
                    flag = false;
                }
            }
            if (flag)
            {
                GameObject temp = GameObject.Instantiate(enemyUnit);
                Enemy enemy = temp.GetComponent<Enemy>();
                enemy.m_enemyData.m_moveData.m_id = lenemySpawnData.m_list[i];
                enemy.m_enemyData.m_hp = lenemySpawnData.m_hp;
                enemy.AIFlag = lenemySpawnData.m_isHost ? true : false;
                enemy.NetWork = m_network;

                enemys.Add(lenemySpawnData.m_list[i], temp);
                temp.SetActive(true);
            }
        }
    }
    void MoveProcess()
    {
        PacketMoveData lData;
        m_network.Session.GetData<PacketMoveData>(out lData);

        players[lData.m_id].GetComponent<Player>().moveData = lData;
    }
    void EnemyMoveProcess()
    {
        PacketMoveData lData;
        m_network.Session.GetData<PacketMoveData>(out lData);

        enemys[lData.m_id].GetComponent<Enemy>().m_enemyData.m_moveData = lData;
        enemys[lData.m_id].transform.position 
            = new Vector3(lData.m_position.x, lData.m_position.y, lData.m_position.z);
    }
    void OutProcess()
    {
        IDData liddata;
        m_network.Session.GetData<IDData>(out liddata);
        Destroy(players[liddata.m_id]);
        players.Remove(liddata.m_id);
    }
    void EnemyOutProcess()
    {
        IDData liddata;
        m_network.Session.GetData<IDData>(out liddata);
        Destroy(enemys[liddata.m_id]);
        enemys.Remove(liddata.m_id);
    }
    /*======================================*/
}
