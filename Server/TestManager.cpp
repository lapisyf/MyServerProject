#include "TestManager.h"
#include "LogManager.h"
#include "SessionManager.h"
//
#pragma region Singleton
bool TestManager::CreateInstance()
{
	if (m_instance == nullptr)
	{
		m_instance = new TestManager();
		return true;
	}
	else
	{
		return false;
	}
}
void TestManager::DestroyInstance()
{
	if (m_instance != nullptr)
	{
		delete m_instance;
	}
}
bool TestManager::Initialize() // �ʱ�ȭ
{
	m_giveIdCounter = 1;
	m_hostId = -1;
	return true;
}
void TestManager::Release() // ��ó��
{
}
TestManager* TestManager::GetInstance()
{
	if (m_instance != nullptr)
	{
		return m_instance;
	}
	else
	{
		return nullptr;
	}
}
TestManager::TestManager()// ������
{
}
TestManager::~TestManager()// �Ҹ���
{
}
TestManager* TestManager::m_instance = nullptr;	// Singleton ��ü
#pragma endregion


void TestManager::Function(Session* _session)
{
	E_PROTOCOL l_protocol = static_cast<E_PROTOCOL>(_session->GetProtocol());
	switch (l_protocol)
	{
	case TestManager::E_PROTOCOL::CTS_IDCREATE:
		IdCreateProcess(_session);
		break;
	case TestManager::E_PROTOCOL::CTS_ENEMYSPAWN:
		EnemySpawnProcess(_session);
		break;
	case TestManager::E_PROTOCOL::CTS_SPAWN:
		SpawnProcess(_session);
		break;
	case TestManager::E_PROTOCOL::CTS_MOVE:
		PlayProcess(_session);
		break;
	case TestManager::E_PROTOCOL::CTS_ENEMYMOVE:
		EnemyMoveProcess(_session);
		break;
	case TestManager::E_PROTOCOL::CTS_ENEMYOUT:
		EnemyOutProcess(_session);
		break;
	case TestManager::E_PROTOCOL::CTS_EXIT:
		ExitProcess(_session);
		break;
	default:
		LogManager::GetInstance()->LogWrite(7777);
		break;
	}
}
void TestManager::IdCreateProcess(Session* _session)
{
	LockGuard l_lockGuard(&m_criticalKey); // ���
	BYTE l_data[BUFSIZE];
	ZeroMemory(l_data, BUFSIZE);
	int l_dataSize = -1;
	_session->SetIdNumber(m_giveIdCounter);

	l_dataSize = IdDataMake(l_data, m_giveIdCounter);
	m_playerList.push_back(_session);

	// ���߿� Host�� ������ �Ǹ� m_hostId -1�� �ʱ�ȭ �ʿ�
	if (m_hostId == -1)
	{	
		m_hostId = m_giveIdCounter++;
		
		if (!_session->SendPacket(static_cast<int>(E_PROTOCOL::STC_HOSTIDCREATE), l_dataSize, l_data))
		{
			LogManager::GetInstance()->LogWrite(1005);
		}
		return;
	}

	m_giveIdCounter++;

	if (!_session->SendPacket(static_cast<int>(E_PROTOCOL::STC_IDCREATE), l_dataSize, l_data))
	{
		LogManager::GetInstance()->LogWrite(1005);
	}
}
void TestManager::EnemySpawnProcess(Session* _session)
{
	LockGuard l_lockGuard(&m_criticalKey); // ���
	BYTE l_data[BUFSIZE];
	ZeroMemory(l_data, BUFSIZE);
	int l_dataSize = -1;
	int l_spawnAmount = 0;
	int l_hp = 0;

	// ���߿� List Data������ List�� �������ִ� �ִ� ������, m_size���� �����ʾƼ� ���� ���� ����
	if (m_enemyDataList.size() >= MAXLISTCOUNT)
	{
		return;
	}

	EnemySpawnDataSplit(_session->GetDataField(), l_spawnAmount, l_hp);

	for (int i = 0; i < l_spawnAmount; i++)
	{
		m_enemyDataList.insert(make_pair(m_giveIdCounter, EnemyData(l_hp, m_giveIdCounter)));
		m_giveIdCounter++;
	}

	
	// �� id �迭�� �����
	for (list<Session*>::iterator iter = m_playerList.begin(); iter != m_playerList.end(); iter++)
	{
		l_dataSize = EnemySpawnDataMake(l_data, (*iter)->GetIdNumber() == m_hostId ? true : false, l_hp);

		if (!(*iter)->SendPacket(static_cast<int>(E_PROTOCOL::STC_ENEMYSPAWN), l_dataSize, l_data))
		{
			LogManager::GetInstance()->LogWrite(1005);
		}

		ZeroMemory(l_data, l_dataSize);
	}
}
void TestManager::SpawnProcess(Session* _session)
{
	BYTE l_data[BUFSIZE];
	ZeroMemory(l_data, BUFSIZE);
	int l_dataSize = -1;

	LockGuard l_lockGuard(&m_criticalKey); // ���

	if (m_MoveDataList.size() >= MAXLISTCOUNT)
	{
		return;
	}

	m_MoveDataList.insert(make_pair(_session, MoveData(_session->GetIdNumber())));
	l_dataSize = SpawnDataMake(l_data);

	for (list<Session*>::iterator iter = m_playerList.begin(); iter != m_playerList.end(); iter++)
	{
		if (!(*iter)->SendPacket(static_cast<int>(E_PROTOCOL::STC_SPAWN), l_dataSize, l_data))
		{
			LogManager::GetInstance()->LogWrite(1005);
		}
	}

	return;
}

void TestManager::PlayProcess(Session* _session)
{
	BYTE l_data[BUFSIZE];
	ZeroMemory(l_data, BUFSIZE);
	int l_dataSize = -1;

	LockGuard l_lockGuard(&m_criticalKey); // ���

	MoveData moveData;
	MoveDataSplit(_session->GetDataField(), moveData);
	//
	// �̵� ������ ���������� üũ �ϴ� �κ�??? 
	//
	m_MoveDataList.find(_session)->second.CopyData(moveData);
	
	l_dataSize = MoveDataMake(l_data, m_MoveDataList.find(_session)->second);

	for (list<Session*>::iterator iter = m_playerList.begin(); iter != m_playerList.end(); iter++)
	{
		if (!(*iter)->SendPacket(static_cast<int>(E_PROTOCOL::STC_MOVE), l_dataSize, l_data))
		{
			LogManager::GetInstance()->LogWrite(1006);
		}
	}
	return;
}

void TestManager::EnemyMoveProcess(Session* _session)
{
	BYTE l_data[BUFSIZE];
	ZeroMemory(l_data, BUFSIZE);
	int l_dataSize = -1;

	MoveData l_enemyMoveData;
	map <int, EnemyData>::iterator l_find;

	LockGuard l_lockGuard(&m_criticalKey);

	MoveDataSplit(_session->GetDataField(), l_enemyMoveData);
	

	// ���� Out��ȣ�� Host�� �ƴ� Guest�� �������� �����Ƿ�, RecvQ�� ���� Move��Ŷ�� ���̿� Enemy Out ��Ŷ�� ���� �ִ�.
	// �� ��� Out��ȣ�� ������ �����͸� ���� Move��Ŷ�� �˻��ϰԵǹǷ�, �̸� ���ϱ����� üũ�̴�.
	l_find = m_enemyDataList.find(l_enemyMoveData.m_id);
	if (l_find == m_enemyDataList.end())
	{
		return;
	}

	l_find->second.m_moveData.CopyData(l_enemyMoveData);
	l_dataSize = MoveDataMake(l_data, m_enemyDataList.find(l_enemyMoveData.m_id)->second.m_moveData);

	for (list<Session*>::iterator iter = m_playerList.begin(); iter != m_playerList.end(); iter++)
	{
		if ((*iter)->GetIdNumber() == m_hostId)
		{
			continue;
		}

		if (!(*iter)->SendPacket(static_cast<int>(E_PROTOCOL::STC_ENEMYMOVE), l_dataSize, l_data))
		{
			LogManager::GetInstance()->LogWrite(1006);
		}
	}
	return;
}

void TestManager::EnemyOutProcess(Session* _session)
{
	BYTE l_data[BUFSIZE];
	ZeroMemory(l_data, BUFSIZE);
	int l_dataSize = -1;
	int l_id = -1;

	LockGuard l_lockGuard(&m_criticalKey); // ���

	IdDataSplit(_session->GetDataField(), l_id);
	l_dataSize = ExitDataMake(l_data, l_id);

	for (list<Session*>::iterator iter = m_playerList.begin(); iter != m_playerList.end(); iter++)
	{
		if ((*iter)->GetIdNumber() == m_hostId)
		{
			continue;
		}

		if (!(*iter)->SendPacket(static_cast<int>(E_PROTOCOL::STC_ENEMYOUT), l_dataSize, l_data))
		{
			LogManager::GetInstance()->LogWrite(1006);
		}
	}

	m_enemyDataList.erase(l_id);
}

void TestManager::ExitProcess(Session* _session)
{
	BYTE l_data[BUFSIZE];
	ZeroMemory(l_data, BUFSIZE);
	int l_dataSize = -1;

	LockGuard l_lockGuard(&m_criticalKey); // ���

	l_dataSize = ExitDataMake(l_data, _session->GetIdNumber());

	for (list<Session*>::iterator iter = m_playerList.begin(); iter != m_playerList.end(); iter++)
	{
		if ((*iter) == _session)
		{
			if (!(*iter)->SendPacket(static_cast<int>(E_PROTOCOL::STC_EXIT), l_dataSize, l_data))
			{
				LogManager::GetInstance()->LogWrite(1006);
			}
			continue;
		}

		if (!(*iter)->SendPacket(static_cast<int>(E_PROTOCOL::STC_OUT), l_dataSize, l_data))
		{
			LogManager::GetInstance()->LogWrite(1006);
		}
	}

	m_MoveDataList.erase(_session);

	for (list<Session*>::iterator iter = m_playerList.begin(); iter != m_playerList.end(); )
	{
		if ((*iter) == _session)
		{
			if (m_hostId == _session->GetIdNumber())
			{
				m_hostId = -1;
			}

			(*iter)->SetIdNumber(-1);
			m_playerList.erase(iter);		
			break;
		}
		else
		{
			iter++;
		}
	}

	if (m_playerList.empty())
	{
		m_enemyDataList.clear();
	}
	return;
}

void TestManager::ForceExitProcess(Session* _session)
{
	BYTE l_data[BUFSIZE];
	ZeroMemory(l_data, BUFSIZE);
	int l_dataSize = -1;

	LockGuard l_lockGuard(&m_criticalKey); // ���

	// IdNumber�� -1�̶�°���, �̹� Exitó�� �Ǿ��ٴ� �ǹ�
	if (_session->GetIdNumber() == -1)
	{
		return;
	}

	l_dataSize = ExitDataMake(l_data, _session->GetIdNumber());

	for (list<Session*>::iterator iter = m_playerList.begin(); iter != m_playerList.end(); iter++)
	{
		if ((*iter) == _session)
		{
			if (!(*iter)->SendPacket(static_cast<int>(E_PROTOCOL::STC_EXIT), l_dataSize, l_data))
			{
				LogManager::GetInstance()->LogWrite(1006);
			}

			continue;
		}
		if (!(*iter)->SendPacket(static_cast<int>(E_PROTOCOL::STC_OUT), l_dataSize, l_data))
		{
			LogManager::GetInstance()->LogWrite(1006);
		}
	}

	m_MoveDataList.erase(_session);

	for (list<Session*>::iterator iter = m_playerList.begin(); iter != m_playerList.end(); )
	{
		if ((*iter) == _session)
		{
			m_playerList.erase(iter);
			break;
		}
		else
		{
			iter++;
		}
	}
	return;
}


int TestManager::IdDataMake(BYTE* _data, int _id)
{
	int l_packedSize = 0;
	BYTE* l_focusPointer = _data;

	l_focusPointer = MemoryCopy(l_focusPointer, l_packedSize, _id);
	return l_packedSize;
}

int TestManager::SpawnDataMake(BYTE* _data)
{
	int l_packedSize = 0;
	BYTE* l_focusPointer = _data;
	int counter = MAXLISTCOUNT;
	l_focusPointer = MemoryCopy(l_focusPointer, l_packedSize, static_cast<int>(m_playerList.size()));
	for (list<Session*>::iterator iter = m_playerList.begin(); iter != m_playerList.end(); iter++)
	{
		l_focusPointer = MemoryCopy(l_focusPointer, l_packedSize, (*iter)->GetIdNumber());
		counter--;
	}

	while (counter > 0)
	{
		l_focusPointer = MemoryCopy(l_focusPointer, l_packedSize, -1);
		counter--;
		if (counter <= 0)
		{
			break;
		}
	}

	return l_packedSize;
}

int TestManager::EnemySpawnDataMake(BYTE* _data, bool _isHost, int _hp)
{
	int l_packedSize = 0;
	BYTE* l_focusPointer = _data;
	int counter = MAXLISTCOUNT;
	l_focusPointer = MemoryCopy(l_focusPointer, l_packedSize, _isHost);
	l_focusPointer = MemoryCopy(l_focusPointer, l_packedSize, _hp);
	l_focusPointer = MemoryCopy(l_focusPointer, l_packedSize, static_cast<int>(m_enemyDataList.size()));

	for (map<int, EnemyData>::iterator iter = m_enemyDataList.begin(); iter != m_enemyDataList.end(); iter++)
	{
		l_focusPointer = MemoryCopy(l_focusPointer, l_packedSize, (*iter).second.m_moveData.m_id);
		counter--;
	}

	while (counter > 0)
	{
		l_focusPointer = MemoryCopy(l_focusPointer, l_packedSize, -1);
		counter--;
		if (counter <= 0)
		{
			break;
		}
	}

	return l_packedSize;
}

int TestManager::MoveDataMake(BYTE* _data, MoveData _moveData)
{
	int l_packedSize = 0;
	BYTE* l_focusPointer = _data;

	l_focusPointer = MemoryCopy(l_focusPointer, l_packedSize, _moveData);

	return l_packedSize;
}


int TestManager::ExitDataMake(BYTE* _data, int _id)
{
	int l_packedSize = 0;
	BYTE* l_focusPointer = _data;

	l_focusPointer = MemoryCopy(l_focusPointer, l_packedSize, _id);

	return l_packedSize;
}



void TestManager::MoveDataSplit(BYTE* _data, MoveData& _moveData)
{
	BYTE* l_focusPointer = _data;

	memcpy(&_moveData, l_focusPointer, sizeof(MoveData));
	l_focusPointer = l_focusPointer + sizeof(MoveData);
}

void TestManager::EnemySpawnDataSplit(BYTE* _data, int& _amount, int& _hp)
{
	BYTE* l_focusPointer = _data;

	memcpy(&_amount, l_focusPointer, sizeof(int));
	l_focusPointer = l_focusPointer + sizeof(int);

	memcpy(&_amount, l_focusPointer, sizeof(int));
	l_focusPointer = l_focusPointer + sizeof(int);
}

void TestManager::IdDataSplit(BYTE* _data, int& _id)
{
	BYTE* l_focusPointer = _data;

	memcpy(&_id, l_focusPointer, sizeof(int));
	l_focusPointer = l_focusPointer + sizeof(int);
}

