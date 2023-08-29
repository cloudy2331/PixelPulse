using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

public class Barrier : MonoBehaviour
{
    public GameObject barrier, player;
    public float downSpeed;
    private float barrierPosStartX;
    private float speed;
    public float createTime = 0f;
    public bool isChecked = false;

    public ObjectPool<GameObject> barriersPool;

    public PointManager pm;

    // Start is called before the first frame update
    void Start()
    {
        createTime = Time.time;

        barrierPosStartX = this.transform.position.x;
        player = GameObject.FindGameObjectWithTag("Player");
        pm = GameObject.FindGameObjectWithTag("PointManager").GetComponent<PointManager>();

        speed = (this.transform.position.x - player.transform.position.x) / (2 / Time.fixedDeltaTime);
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space) && player.transform.position.y / Mathf.Abs(player.transform.position.y) == this.transform.position.y / Mathf.Abs(this.transform.position.y))
        {
            //Debug.Log((Time.time - createTime) * 1000 - 2000);
            if (Mathf.Abs((Time.time - createTime) * 1000 - 2000) < 150 && !isChecked)//From the barrier generate to player need 2000ms, and check input after 150ms.
            {
                isChecked = true;
                barrier.GetComponent<SpriteRenderer>().material.color = new Color(0, 191 * 0.03f, 191 * 0.03f, 0);
                pm.ComboEvent += pm.AddCombo;
                pm.Combo();
            }
        }

        if (this.transform.position.x < -12)
        {
            if (!isChecked)
            {
                pm.ComboEvent += pm.ClearCombo;
                pm.ComboEvent += pm.AddMiss;
                pm.Combo();
            }
            barriersPool.Release(gameObject);
        }

        if (isChecked)
        {
            barrier.transform.localPosition = new Vector3(barrier.transform.localPosition.x, barrier.transform.localPosition.y - downSpeed, barrier.transform.localPosition.z);
        }
    }

    void FixedUpdate()
    {
        this.transform.position = new Vector3(this.transform.position.x - speed, this.transform.position.y, this.transform.position.z);
    }
}
