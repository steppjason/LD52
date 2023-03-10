using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Player : MonoBehaviour
{
	public GameObject grabber;
	public GameObject bodyGrabber;

	[SerializeField] float moveSpeed = 1f;
	[SerializeField] GameObject empty;
	[SerializeField] SpriteRenderer _normal;
	[SerializeField] SpriteRenderer _holding;
	[SerializeField] SpriteRenderer _holdingBody;
	[SerializeField] GameObject chopbar;
	[SerializeField] Image chopFillBar;
	[SerializeField] Animator animator;

	Vector2 _input = new Vector2(0, 0);
	Vector2 _direction = new Vector2(0, 0);
	Rigidbody2D _rb;

	GameObject _pickup;
	GameObject _temp;

	bool _isDisposal;
	bool _isTable;
	bool _isFreezer;
	bool _isTank;
	bool _isPickup;
	bool _canMove = true;
	bool _chopping = false;

	float chopTimer = 0f;

	Organ _organ;
	Body _body;
	Table _table;
	Ice _iceBox;


	bool _isCarrying = false;

	void Start()
	{
		_rb = GetComponent<Rigidbody2D>();
		_table = FindObjectOfType<Table>();
		_pickup = empty;
	}

	void Update()
	{
		if (!GameManager.Instance.isGameOver)
		{
			if (_canMove)
			{
				Action();
				Move();
			}

			if (_chopping)
			{
				chopTimer += Time.deltaTime;
				chopFillBar.fillAmount = chopTimer / 5;
				if (chopTimer > 5)
				{
					chopbar.SetActive(false);
					_chopping = false;
					animator.SetBool("isChopping", false);
					chopTimer = 0;
					_canMove = true;
					GameManager.Instance.audioManager.PlayOrganSpawn();
					GameManager.Instance.eyePool.SetActiveObject(new Vector3(-2.859f, -2.229f, 0), Quaternion.identity);
					GameManager.Instance.heartPool.SetActiveObject(new Vector3(-2.859f, -2.229f, 0), Quaternion.identity);
					GameManager.Instance.lungsPool.SetActiveObject(new Vector3(-2.859f, -2.229f, 0), Quaternion.identity);
					GameManager.Instance.kidneyPool.SetActiveObject(new Vector3(-2.859f, -2.229f, 0), Quaternion.identity);
				}
			}
		}

	}

	void Move()
	{

		_input.x = Input.GetAxisRaw("Horizontal");
		_input.y = Input.GetAxisRaw("Vertical");
		_direction = _input;
		_direction.Normalize();

		_rb.position += _direction * moveSpeed * Time.deltaTime;

		if(_input.x != 0 || _input.y != 0)
			animator.SetBool("isWalking", true);
		else 
			animator.SetBool("isWalking", false);

		if (_input.x < 0)
		{
			chopbar.transform.rotation = Quaternion.Euler(0, 0, 0);
			transform.rotation = Quaternion.Euler(0, 180, 0);
		}
		else if (_input.x > 0)
		{
			chopbar.transform.rotation = Quaternion.Euler(0, 0, 0);
			transform.rotation = Quaternion.Euler(0, 0, 0);
		}

	}

	void Action()
	{
		if (Input.GetKeyDown(KeyCode.F) && _isTable && _table.hasBody && !_isCarrying)
		{
			if (_table.body.TryGetComponent<Body>(out _body))
			{
				if (!_body.isOpen)
				{
					_canMove = false;
					_chopping = true;
					animator.SetBool("isChopping", true);
					chopbar.SetActive(true);
					chopFillBar.fillAmount = 0;
					_body.Chop();
					_table.OpenBody();
					_body = null;
				}
			}

		}

		if (Input.GetKeyDown(KeyCode.E))
		{
			if (_isTable && _table.hasBody && !_isCarrying)
				_pickup = _table.RemoveBody();

			if (_isTank && !_isCarrying)
				_pickup = GameManager.Instance.bodyPool.GrabActiveObject(gameObject.transform.position, Quaternion.identity);

			CheckCarrying();
			CheckPickup();
		}
	}

	void CheckCarrying()
	{
		if (_isCarrying)
		{
			if (_organ != null)
			{
				_normal.enabled = true;
				_holding.enabled = false;

				if (_isDisposal)
					_organ.Dispose();
				else if (_isFreezer)
				{
					_pickup = GameManager.Instance.iceboxPool.GrabActiveObject(gameObject.transform.position, Quaternion.identity);
					if (_pickup != empty && _pickup.TryGetComponent<Ice>(out _iceBox))
					{
						_iceBox.SetOrganType(_organ);
						_isCarrying = true;
						_normal.enabled = false;
						_holding.enabled = true;
						_iceBox.Grab();
					}

					_organ.Deposit();
					return;
				}
				else
					_organ.Drop();

				_organ = null;
			}

			if (_body != null)
			{
				_normal.enabled = true;
				_holdingBody.enabled = false;

				if (_isDisposal)
					_body.Dispose();
				else if (_isTable && !_table.hasBody)
					_body.Put();
				else
					_body.Drop();

				_body = null;
			}

			if (_iceBox != null)
			{
				_normal.enabled = true;
				_holding.enabled = false;

				if (_isPickup)
				{
					GameManager.Instance.Deposit(_iceBox.organType);
					_iceBox.Deposit();
				}
				else
					_iceBox.Drop();

				_iceBox = null;
			}

			_isCarrying = false;
			_pickup = empty;

			return;
		}
	}

	void CheckPickup()
	{
		if (_pickup != empty && _pickup.TryGetComponent<Organ>(out _organ))
		{
			_isCarrying = true;
			_normal.enabled = false;
			_holding.enabled = true;
			_organ.Grab();
			return;
		}

		if (_pickup != empty && _pickup.TryGetComponent<Body>(out _body))
		{
			_isCarrying = true;
			_normal.enabled = false;
			_holdingBody.enabled = true;
			_body.Grab();
			return;
		}

		if (_pickup != empty && _pickup.TryGetComponent<Ice>(out _iceBox))
		{
			_isCarrying = true;
			_normal.enabled = false;
			_holding.enabled = true;
			_iceBox.Grab();
			return;
		}
	}

	void OnCollisionStay2D(Collision2D other)
	{
		if (other.gameObject.CompareTag("Body") || other.gameObject.CompareTag("Organ") || other.gameObject.CompareTag("Ice"))
			_pickup = other.gameObject;

		if (other.gameObject.CompareTag("Disposal"))
			_isDisposal = true;

		if (other.gameObject.CompareTag("Table"))
			_isTable = true;

		if (other.gameObject.CompareTag("Freezer"))
			_isFreezer = true;

		if (other.gameObject.CompareTag("Tank"))
			_isTank = true;

		if (other.gameObject.CompareTag("Pickup"))
			_isPickup = true;

	}

	void OnCollisionExit2D(Collision2D other)
	{
		_pickup = empty;

		_isTank = false;
		_isFreezer = false;
		_isTable = false;
		_isDisposal = false;
		_isPickup = false;
	}
}
