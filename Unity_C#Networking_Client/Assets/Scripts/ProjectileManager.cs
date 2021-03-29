using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProjectileManager : MonoBehaviour
{
    public int id;                      //폭탄 ID
    public GameObject explosionPrefab;  //폭탄 프리팹

    /// <summary>초기화(폭탄ID설정)</summary>
    /// <param name="_id">폭탄ID</param>
    public void Initialize(int _id)
    {
        id = _id;
    }

    /// <summary>폭발</summary>
    /// <param name="_position">폭탄 포지션</param>
    public void Explode(Vector3 _position)
    {
        transform.position = _position;
        Instantiate(explosionPrefab, transform.position, Quaternion.identity);
        GameManager.projectiles.Remove(id);
        Destroy(gameObject);
    }
}
