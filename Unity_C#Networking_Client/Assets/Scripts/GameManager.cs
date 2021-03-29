using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>해당 클라이언트에서 동작하는 것들 관리(server로부터 수신한 data를 토대로 gameobject update)</summary>
public class GameManager : MonoBehaviour
{
    public static GameManager instance;
    
    public static Dictionary<int, PlayerManager> players = new Dictionary<int, PlayerManager>();            //모든플레이어정보 저장
    public static Dictionary<int, ItemSpawner> itemSpawners = new Dictionary<int, ItemSpawner>();           //모든아이템정보 저장
    public static Dictionary<int, ProjectileManager> projectiles = new Dictionary<int, ProjectileManager>();//모든 폭탄정보 저장
    //public static Dictionary<int, EnemyManager> enemies = new Dictionary<int, EnemyManager>();

    /// <summary>다른 player 프리팹</summary>
    public GameObject localPlayerPrefab;    //자신 player 프리팹
    /// <summary>다른 player 프리팹</summary>
    public GameObject playerPrefab;
    /// <summary>아이템 프리팹</summary>
    public GameObject itemSpawnerPrefab;
    /// <summary>폭탄 프리팹</summary>
    public GameObject projectilePrefab;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else if (instance != this)
        {
            Debug.Log("Instance already exists, destroying object!");
            Destroy(this);
        }
    }

    /// <summary>Spawns a player.</summary>
    /// <param name="_id">The player's ID.</param>
    /// <param name="_name">The player's name.</param>
    /// <param name="_position">The player's starting position.</param>
    /// <param name="_rotation">The player's starting rotation.</param>
    public void SpawnPlayer(int _id, string _username, Vector3 _position, Quaternion _rotation)
    {
        GameObject _player;
        if (_id == Client.instance.myId)
        {//현재 클라이언트의 플레이어인경우
            _player = Instantiate(localPlayerPrefab, _position, _rotation);
        }
        else
        {//다른 크라이언트의 플레이어인경우
            _player = Instantiate(playerPrefab, _position, _rotation);
        }

        _player.GetComponent<PlayerManager>().Initialize(_id, _username);
        players.Add(_id, _player.GetComponent<PlayerManager>());
    }

    /// <summary>아이템 생성(아이템 초기화 및 dictionary 추가)</summary>
    /// <param name="_spawnerId">아이템ID</param>
    /// <param name="_position">아이템 position</param>
    /// <param name="_hasItem">아이템 존재여부</param>
    public void CreateItemSpawner(int _spawnerId, Vector3 _position, bool _hasItem)
    {
        GameObject _spawner = Instantiate(itemSpawnerPrefab, _position, itemSpawnerPrefab.transform.rotation);
        _spawner.GetComponent<ItemSpawner>().Initialize(_spawnerId, _hasItem);
        itemSpawners.Add(_spawnerId, _spawner.GetComponent<ItemSpawner>());
    }

    /// <summary>폭탄 생성</summary>
    /// <param name="_id">폭탄 ID</param>
    /// <param name="_position">폭탄 position</param>
    public void SpawnProjectile(int _id, Vector3 _position)
    {
        GameObject _projectile = Instantiate(projectilePrefab, _position, Quaternion.identity);
        _projectile.GetComponent<ProjectileManager>().Initialize(_id);
        projectiles.Add(_id, _projectile.GetComponent<ProjectileManager>());
    }
}