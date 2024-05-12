using Photon.Pun;
using Photon.Realtime;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;
using Hashtable = ExitGames.Client.Photon.Hashtable;

public class PlayerController : MonoBehaviourPunCallbacks, IDamageable
{
	[SerializeField] UnityEngine.UI.Image healthbarImage;
	[SerializeField] GameObject ui;

	[SerializeField] GameObject cameraHolder;

	[SerializeField] float mouseSensitivity, sprintSpeed, Spead, jumpForce, smoothTime;

	[SerializeField] Item[] items;

	int itemIndex;
	int previousItemIndex = -1;

	float verticalLookRotation;
	bool grounded;
	Vector3 smoothMoveVelocity;
	Vector3 moveAmount;

	Rigidbody rb;

	PhotonView PV;

	const float maxHealth = 100f;
	float currentHealth = maxHealth;

	PlayerManager playerManager;

	void Awake()
	{
		rb = GetComponent<Rigidbody>();
		PV = GetComponent<PhotonView>();

		playerManager = PhotonView.Find((int)PV.InstantiationData[0]).GetComponent<PlayerManager>();
	}

	void Start()
	{
        UnityEngine.Cursor.lockState = CursorLockMode.Locked;

        if (PV.IsMine)
		{
			EquipItem(0);
		}
		else
		{
			Destroy(GetComponentInChildren<Camera>().gameObject);
			Destroy(rb);
			Destroy(ui);
		}
	}

	void Update()
	{
		if(!PV.IsMine)
			return;

		Look();
		Move();
		

		for(int i = 0; i < items.Length; i++)
		{
			if(Input.GetKeyDown((i + 1).ToString()))
			{
				EquipItem(i);
				break;
			}
		}

		if(Input.GetAxisRaw("Mouse ScrollWheel") > 0f)
		{
			if(itemIndex >= items.Length - 1)
			{
				EquipItem(0);
			}
			else
			{
				EquipItem(itemIndex + 1);
			}
		}
		else if(Input.GetAxisRaw("Mouse ScrollWheel") < 0f)
		{
			if(itemIndex <= 0)
			{
				EquipItem(items.Length - 1);
			}
			else
			{
				EquipItem(itemIndex - 1);
			}
		}

		if(Input.GetMouseButtonDown(0))
		{
			items[itemIndex].Use();
		}

		if(transform.position.y < -10f) // Die if you fall out of the world
		{
			Die();
		}
	}

	void Look()
	{
		transform.Rotate(Vector3.up * Input.GetAxisRaw("Mouse X") * mouseSensitivity);
        transform.Rotate(Vector3.right * Input.GetAxisRaw("Mouse Y") * -mouseSensitivity);
        if (Input.GetKey(KeyCode.E))
        {
			transform.Rotate(new Vector3(0, 0, 1) * -1);
        }
        if (Input.GetKey(KeyCode.Q))
        {
            transform.Rotate(new Vector3(0, 0, 1) * 1);
        }


    }

	void Move()
	{
        if (Input.GetKey(KeyCode.W))
        {
            rb.AddForce((transform.forward) * Spead);
        }
        if (Input.GetKey(KeyCode.S))
        {
            rb.AddForce((-transform.forward) * Spead);
        }
        if (Input.GetKey(KeyCode.A))
        {
            rb.AddForce((transform.rotation * Vector3.left) * Spead);
        }
        if (Input.GetKey(KeyCode.D))
        {
            rb.AddForce((transform.rotation * Vector3.right) * Spead);
        }
        if (Input.GetKey(KeyCode.Space))
        {
            rb.AddForce((transform.rotation * Vector3.up) * Spead);
        }
        if (Input.GetKey(KeyCode.LeftControl))
        {
            rb.AddForce((transform.rotation * Vector3.down) * Spead);
        }
        //Vector3 moveDir = new Vector3(Input.GetAxisRaw("Horizontal"), 0, Input.GetAxisRaw("Vertical")).normalized;

        //smoveAmount = Vector3.SmoothDamp(moveAmount, moveDir * (Input.GetKey(KeyCode.LeftShift) ? sprintSpeed : walkSpeed), ref smoothMoveVelocity, smoothTime);
    }



	void EquipItem(int _index)
	{
		if(_index == previousItemIndex)
			return;

		itemIndex = _index;

		items[itemIndex].itemGameObject.SetActive(true);

		if(previousItemIndex != -1)
		{
			items[previousItemIndex].itemGameObject.SetActive(false);
		}

		previousItemIndex = itemIndex;

		if(PV.IsMine)
		{
			Hashtable hash = new Hashtable();
			hash.Add("itemIndex", itemIndex);
			PhotonNetwork.LocalPlayer.SetCustomProperties(hash);
		}
	}

	public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
	{
		if(changedProps.ContainsKey("itemIndex") && !PV.IsMine && targetPlayer == PV.Owner)
		{
			EquipItem((int)changedProps["itemIndex"]);
		}
	}

	public void SetGroundedState(bool _grounded)
	{
		grounded = _grounded;
	}

	void FixedUpdate()
	{
		if(!PV.IsMine)
			return;

		rb.MovePosition(rb.position + transform.TransformDirection(moveAmount) * Time.fixedDeltaTime);
	}

	public void TakeDamage(float damage)
	{
		PV.RPC(nameof(RPC_TakeDamage), PV.Owner, damage);
	}

	[PunRPC]
	void RPC_TakeDamage(float damage, PhotonMessageInfo info)
	{
		currentHealth -= damage;

		healthbarImage.fillAmount = currentHealth / maxHealth;

		if(currentHealth <= 0)
		{
			Die();
			PlayerManager.Find(info.Sender).GetKill();
		}
	}

	void Die()
	{
		playerManager.Die();
	}
}