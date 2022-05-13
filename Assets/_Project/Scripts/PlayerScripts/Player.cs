using Demonics.Sounds;
using System.Collections;
using UnityEngine;

public class Player : MonoBehaviour, IHurtboxResponder, IHitboxResponder
{
	[SerializeField] private PlayerAnimator _playerAnimator = default;
	[SerializeField] private Assist _assist = default;
	[SerializeField] private Pushbox _groundPushbox = default;
	[SerializeField] private Pushbox _airPushbox = default;
	[SerializeField] private GameObject _hurtbox = default;
	[SerializeField] private GameObject _blockEffectPrefab = default;
	[SerializeField] private GameObject _shadowbreakPrefab = default;
	[SerializeField] protected Transform _effectsParent = default;
	[SerializeField] private Transform _grabPoint = default;
	[SerializeField] private Transform _keepFlip = default;
	[SerializeField] private InputBuffer _inputBuffer = default;
	[SerializeField] private GameObject[] _playerIcons = default;
	private PlayerMovement _otherPlayer;
	protected PlayerUI _playerUI;
	private PlayerUI _otherPlayerUI;
	private PlayerMovement _playerMovement;
	protected PlayerComboSystem _playerComboSystem;
	private PlayerStats _playerStats;
	private BrainController _controller;
	private Audio _audio;
	private Coroutine _stunCoroutine;
	private Coroutine _blockCoroutine;
	protected float _arcana;
	private float _assistGauge = 1.0f;
	private bool _throwBreakInvulnerable;
	public PlayerStatsSO PlayerStats { get { return _playerStats.PlayerStatsSO; } private set { } }
	public PlayerUI PlayerUI { get { return _playerUI; } private set { } }
	public AttackSO CurrentAttack { get; set; }
	public AttackSO CurrentHurtAttack { get; set; }
	public float Health { get; private set; }
	public int Lives { get; set; } = 2;
	public bool IsBlocking { get; private set; }
	public bool IsKnockedDown { get; private set; }
	public bool HitMiddair { get; set; }
	public bool IsAttacking { get; set; }
	public bool IsPlayerOne { get; set; }
	public float ArcaneSlowdown { get; set; } = 7.5f;
	public bool IsStunned { get; private set; }
	public bool BlockingLow { get; set; }
	public bool BlockingHigh { get; set; }
	public bool BlockingMiddair { get; set; }
	public bool IsDead { get; set; }
	public bool CanFlip { get; set; } = true;
	public bool CanShadowbreak { get; set; } = true;
	public bool CanCancelAttack { get; set; }

	void Awake()
	{
		_playerMovement = GetComponent<PlayerMovement>();
		_playerComboSystem = GetComponent<PlayerComboSystem>();
		_playerStats = GetComponent<PlayerStats>();
		_audio = GetComponent<Audio>();
	}

	public void SetController()
	{
		_controller = GetComponent<BrainController>();
	}

	public void SetAssist(AssistStatsSO assistStats)
	{
		_assist.SetAssist(assistStats);
		_playerUI.SetAssistName(assistStats.name[0].ToString());
	}

	void Start()
	{
		InitializeStats();
	}

	public void SetPlayerUI(PlayerUI playerUI)
	{
		_playerUI = playerUI;
	}

	public void SetOtherPlayer(PlayerMovement otherPlayer)
	{
		_otherPlayer = otherPlayer;
		_otherPlayerUI = otherPlayer.GetComponent<Player>().PlayerUI;
	}

	public void ResetPlayer()
	{
		transform.rotation = Quaternion.identity;
		_playerMovement.SetLockMovement(true);
		IsStunned = false;
		CanFlip = true;
		IsDead = false;
		IsAttacking = false;
		_controller.ActiveController.enabled = true;
		_controller.ActivateInput();
		_effectsParent.gameObject.SetActive(true);
		_playerAnimator.Rebind();
		SetGroundPushBox(true);
		SetAirPushBox(false);
		SetPushboxTrigger(false);
		SetHurtbox(true);
		_assistGauge = 1.0f;
		_playerMovement.FullyLockMovement = false;
		transform.SetParent(null);
		_playerMovement.IsInCorner = false;
		_playerMovement.SetRigidbodyToKinematic(false);
		if (!GameManager.Instance.InfiniteArcana)
		{
			_arcana = 0.0f;
		}
		IsKnockedDown = false;
		StopAllCoroutines();
		_playerMovement.StopAllCoroutines();
		_otherPlayerUI.ResetCombo();
		_playerMovement.ResetPlayerMovement();
		_playerUI.SetArcana(_arcana);
		_playerUI.SetAssist(_assistGauge);
		_playerUI.ResetHealthDamaged();
		InitializeStats();
		_playerUI.ShowPlayerIcon();
	}

	public void ResetLives()
	{
		Lives = 2;
		_playerUI.ResetLives();
	}

	public void MaxHealthStats()
	{
		Health = _playerStats.PlayerStatsSO.maxHealth;
		_playerUI.SetHealth(Health);
	}

	private void InitializeStats()
	{
		_playerUI.InitializeUI(_playerStats.PlayerStatsSO, _controller, _playerIcons);
		Health = _playerStats.PlayerStatsSO.maxHealth;
		_playerUI.SetHealth(Health);
	}

	void Update()
	{
		ArcanaCharge();
		AssistCharge();
		CheckFlip();
		CheckIsBlocking();
	}

	private void AssistCharge()
	{
		if (_assistGauge < 1.0f && !_assist.IsOnScreen && CanShadowbreak && GameManager.Instance.HasGameStarted)
		{
			_assistGauge += Time.deltaTime / (11.0f - _assist.AssistStats.assistRecharge);
			if (GameManager.Instance.InfiniteAssist)
			{
				_assistGauge = 1.0f;
			}
			_playerUI.SetAssist(_assistGauge);
		}
	}

	private void ArcanaCharge()
	{
		if (_arcana < _playerStats.PlayerStatsSO.maxArcana && GameManager.Instance.HasGameStarted)
		{
			_arcana += Time.deltaTime / (ArcaneSlowdown - _playerStats.PlayerStatsSO.arcanaRecharge);
			if (GameManager.Instance.InfiniteArcana)
			{
				_arcana = _playerStats.PlayerStatsSO.maxArcana;
			}
			_playerUI.SetArcana(_arcana);
		}
	}

	private void CheckFlip()
	{
		if (!IsDead && CanFlip && !IsKnockedDown && !_playerMovement.FullyLockMovement)
		{
			if (_playerMovement.IsGrounded && !IsAttacking )
			{
				Flip();
			}
		}
	}

	public void Flip()
	{
		if (_otherPlayer.transform.position.x > transform.position.x && transform.position.x < 9.2f && transform.localScale.x != 1.0f)
		{
			_playerAnimator.IsRunning(false);
			transform.localScale = new Vector2(1.0f, transform.localScale.y);
			_keepFlip.localScale = new Vector2(1.0f, transform.localScale.y);
		}
		else if (_otherPlayer.transform.position.x < transform.position.x && transform.position.x > -9.2f && transform.localScale.x != -1.0f)
		{
			_playerAnimator.IsRunning(false);
			transform.localScale = new Vector2(-1.0f, transform.localScale.y);
			_keepFlip.localScale = new Vector2(-1.0f, transform.localScale.y);
		}
	}

	public bool ThrowAction()
	{
		if (!IsAttacking && _playerMovement.IsGrounded)
		{
			_audio.Sound("Hit").Play();
			IsAttacking = true;
			_playerAnimator.Throw();
			CurrentAttack = _playerComboSystem.GetComboAttack(InputEnum.Throw);
			_playerMovement.TravelDistance(Vector2.zero);
			return true;
		}
		return false;
	}

	public virtual bool ArcaneAction()
	{
		//REPLACE
		if (_arcana >= 1.0f)
		{
			if (CanCancelAttack)
			{
				IsAttacking = false;
				_playerAnimator.CancelAttack();
				CanCancelAttack = false;
			}
			if (!IsAttacking && !IsBlocking && !_playerMovement.IsDashing && !IsKnockedDown)
			{
				if (_playerComboSystem.GetArcana().airOk || _playerMovement.IsGrounded)
				{
					_playerMovement.StopKnockback();
					_playerMovement.ResetToWalkSpeed();
					if (!GameManager.Instance.InfiniteArcana)
					{
						_arcana--;
					}
					_playerUI.DecreaseArcana();
					_playerUI.SetArcana(_arcana);
					_audio.Sound("Hit").Play();
					IsAttacking = true;
					_playerAnimator.Arcana();
					CurrentAttack = _playerComboSystem.GetArcana();

					if (!string.IsNullOrEmpty(CurrentAttack.attackSound))
					{
						_audio.Sound(CurrentAttack.attackSound).Play();
					}
					if (!CurrentAttack.isAirAttack)
					{
						_playerMovement.TravelDistance(new Vector2(CurrentAttack.travelDistance * transform.localScale.x, CurrentAttack.travelDirection.y));
					}
					return true;
				}
			}
		}
		return false;
		//REPLACE
	}

	public virtual bool LightAction()
	{
		return Attack(InputEnum.Light);
	}

	public virtual bool MediumAction()
	{
		return Attack(InputEnum.Medium);
	}

	public virtual bool HeavyAction()
	{
		return Attack(InputEnum.Heavy);
	}

	protected virtual bool Attack(InputEnum inputEnum)
	{
		if (CanCancelAttack)
		{
			IsAttacking = false;
			_playerAnimator.CancelAttack();
			CanCancelAttack = false;
		}
		if (!IsAttacking && !IsBlocking && !_playerMovement.IsDashing && !IsKnockedDown)
		{
			_audio.Sound("Hit").Play();
			IsAttacking = true;
			_playerAnimator.Attack(inputEnum.ToString());
			CurrentAttack = _playerComboSystem.GetComboAttack(inputEnum);
			AttackTravel();
			if (!string.IsNullOrEmpty(CurrentAttack.attackSound))
			{
				_audio.Sound(CurrentAttack.attackSound).Play();
			}
			if (CurrentAttack.travelDirection.y > 0.0f)
			{
				SetPushboxTrigger(true);
				SetAirPushBox(true);
			}
			return true;
		}
		return false;
	}

	public void AttackTravel()
	{
		if (!CurrentAttack.isAirAttack)
		{
			_playerMovement.TravelDistance(new Vector2(CurrentAttack.travelDistance * transform.localScale.x, CurrentAttack.travelDirection.y));
		}
	}

	public bool AssistAction()
	{
		if (_assistGauge >= 1.0f && !_playerMovement.FullyLockMovement && !IsStunned && !IsKnockedDown && GameManager.Instance.HasGameStarted)
		{
			if (!IsBlocking)
			{
				_assist.Attack();
			}
			else
			{
				if (CanShadowbreak)
				{
					Shadowbreak();
				}
			}
			_assistGauge--;
			_playerUI.SetAssist(_assistGauge);
			return true;
		}
		return false;
	}

	private void Shadowbreak()
	{
		CanShadowbreak = false;
		_audio.Sound("Shadowbreak").Play();
		_playerAnimator.Shadowbreak();
		_playerMovement.SetLockMovement(true);
		CameraShake.Instance.Shake(0.5f, 0.1f);
		Transform shadowbreak = Instantiate(_shadowbreakPrefab, _playerAnimator.transform).transform;
		shadowbreak.position = new Vector2(transform.position.x, transform.position.y + 1.5f);
	}

	public void HitboxCollided(RaycastHit2D hit, Hurtbox hurtbox = null)
	{
		CurrentAttack.hurtEffectPosition = hit.point;
		bool gotHit = hurtbox.TakeDamage(CurrentAttack);
		if (!CurrentAttack.isAirAttack && CurrentAttack.attackTypeEnum != AttackTypeEnum.Break && !CurrentAttack.isProjectile && !CurrentAttack.isArcana)
		{
			CanCancelAttack = true;
		}
		_playerMovement.SetLockMovement(true);
		if (gotHit && CurrentAttack.attackTypeEnum == AttackTypeEnum.Throw)
		{
			Throw();
		}
		if (_otherPlayer.IsInCorner && !CurrentAttack.isProjectile)
		{
			_playerMovement.Knockback(new Vector2(-transform.localScale.x, 0.0f), CurrentAttack.knockback, CurrentAttack.knockbackDuration);
		}
	}

	private void Throw()
	{
		_playerMovement.FullyLockMovement = true;
		_otherPlayer.GetComponent<Player>().GetThrown(_grabPoint);
		_playerAnimator.ArcanaEnd();
		_playerAnimator.ThrowEnd();
		SetHurtbox(false);
	}

	public void ThrowEnd()
	{
		_playerMovement.FullyLockMovement = false;
		_otherPlayer.GetComponent<Player>().GetThrownEnd();
		_playerAnimator.ResetTrigger("ArcanaEnd");
		_playerAnimator.ResetTrigger("ThrowEnd");
		SetHurtbox(true);
	}
	private void GetThrown(Transform grabPoint)
	{
		if (_stunCoroutine != null)
		{
			StopCoroutine(_stunCoroutine);
		}
		_playerMovement.SetRigidbodyToKinematic(true);
		transform.SetParent(grabPoint);
		transform.localPosition = Vector2.zero;
		transform.localScale = new Vector2(-1.0f, 1.0f);
		_controller.DeactivateInput();
		_playerAnimator.Hurt();
		_playerAnimator.SetSpriteOrder(-1);
		if (_otherPlayer.GetComponent<Player>().IsStunned)
		{
			_otherPlayer.GetComponent<Player>().ThrowEnd();
		}
	}

	private void GetThrownEnd()
	{
		transform.SetParent(null);
		_playerMovement.SetRigidbodyToKinematic(false);
		_playerAnimator.SetSpriteOrder(0);
		IsKnockedDown = true;
		_playerAnimator.CancelHurt();
		_controller.ActivateInput();
		LoseHealth();
	}

	public virtual void CreateEffect(bool isProjectile = false)
	{
		if (CurrentAttack.hitEffect != null)
		{
			GameObject hitEffect;
			hitEffect = Instantiate(CurrentAttack.hitEffect, _effectsParent);
			hitEffect.transform.localPosition = CurrentAttack.hitEffectPosition;
			hitEffect.transform.localRotation = Quaternion.Euler(0.0f, 0.0f, CurrentAttack.hitEffectRotation);
			if (isProjectile)
			{
				hitEffect.transform.SetParent(null);
				hitEffect.GetComponent<MoveInDirection>().Direction = new Vector2(transform.localScale.x, 0.0f);
				hitEffect.transform.GetChild(0).GetChild(0).GetComponent<Hitbox>().SetHitboxResponder(transform);
			}
		}
	}

	public bool TakeDamage(AttackSO attackSO)
	{
		CurrentHurtAttack = attackSO;
		DestroyEffects();
		if (!_playerMovement.IsGrounded)
		{
			HitMiddair = true;
		}

		if (attackSO.attackTypeEnum == AttackTypeEnum.Throw)
		{
			if (_playerMovement.IsGrounded && !_throwBreakInvulnerable)
			{
				_playerAnimator.Hurt();
				return true;
			}
			else
			{
				return false;
			}
		}

		if (!IsAttacking && !_playerMovement.IsDashing && _controller.ControllerInputName == ControllerTypeEnum.Cpu.ToString() && TrainingSettings.BlockAlways && !IsStunned && GameManager.Instance.IsCpuOff)
		{
			if (!_playerMovement.IsGrounded)
			{
				BlockingMiddair = true;
			}
			else
			{
				if (attackSO.attackTypeEnum == AttackTypeEnum.Overhead)
				{
					BlockingHigh = true;
				}
				else if (attackSO.attackTypeEnum == AttackTypeEnum.Mid)
				{
					BlockingHigh = true;
				}
				else if (attackSO.attackTypeEnum == AttackTypeEnum.Low)
				{
					BlockingLow = true;
				}
			}
		}

		if (!BlockingLow && !BlockingHigh && !BlockingMiddair || BlockingLow && attackSO.attackTypeEnum == AttackTypeEnum.Overhead || BlockingHigh && attackSO.attackTypeEnum == AttackTypeEnum.Low || attackSO.attackTypeEnum == AttackTypeEnum.Break)
		{
			if (attackSO.attackTypeEnum == AttackTypeEnum.Break && _throwBreakInvulnerable)
			{
				return false;
			}
			_playerAnimator.Hurt();
			if (attackSO.cameraShaker != null)
			{
				CameraShake.Instance.Shake(attackSO.cameraShaker.intensity, attackSO.cameraShaker.timer);
			}
			CanCancelAttack = false;
			_playerMovement.StopGhosts();
			GameObject effect = Instantiate(attackSO.hurtEffect);
			effect.transform.localPosition = attackSO.hurtEffectPosition;
			if (IsAttacking)
			{
				_otherPlayerUI.DisplayNotification("Punish");
			}
			IsKnockedDown = attackSO.causesKnockdown;
			_audio.Sound(attackSO.impactSound).Play();
			_playerMovement.StopDash();
			_otherPlayerUI.IncreaseCombo();
			Stun(attackSO.hitStun);
			_inputBuffer.ClearInputBuffer();
			_playerMovement.Knockback(new Vector2(_otherPlayer.transform.localScale.x, attackSO.knockbackDirection.y), attackSO.knockback, attackSO.knockbackDuration);
			IsAttacking = false;
			if (!GameManager.Instance.InfiniteHealth)
			{
				Health--;
				_playerUI.SetHealth(Health);
			}
			if (Health <= 0)
			{
				Die();
			}
			else
			{
				GameManager.Instance.HitStop(attackSO.hitstop);
			}
			return true;
		}
		else
		{
			_playerAnimator.Hurt();
			_playerMovement.Knockback(new Vector2(_otherPlayer.transform.localScale.x, 0.0f), attackSO.knockback, attackSO.knockbackDuration);
			IsAttacking = false;
			GameObject effect = Instantiate(_blockEffectPrefab);
			effect.transform.localPosition = attackSO.hurtEffectPosition;
			_audio.Sound("Block").Play();
			if (!BlockingMiddair)
			{
				if (BlockingLow)
				{
					_playerAnimator.IsBlockingLow(true);
				}
				else
				{
					_playerAnimator.IsBlocking(true);
				}
			}
			else
			{
				_playerAnimator.IsBlockingAir(true);
			}

			IsBlocking = true;
			if (_blockCoroutine != null)
			{
				StopCoroutine(_blockCoroutine);
			}
			_blockCoroutine = StartCoroutine(ResetBlockingCoroutine(attackSO.hitStun));
			return false;
		}
	}

	private void LoseHealth()
	{
		_inputBuffer.ClearInputBuffer();
		GameObject effect = Instantiate(CurrentHurtAttack.hurtEffect);
		effect.transform.localPosition = new Vector2(transform.position.x, transform.position.y + 0.5f);
		if (!GameManager.Instance.InfiniteHealth)
		{
			Health--;
			_playerUI.SetHealth(Health);
		}
		if (Health <= 0)
		{
			Die();
		}
		else
		{
			StartCoroutine(KnockdownCoroutine());
			GameManager.Instance.HitStop(CurrentHurtAttack.hitstop);
		}
		_playerUI.UpdateHealthDamaged();
	}

	IEnumerator ResetBlockingCoroutine(float blockStun)
	{
		yield return new WaitForSeconds(blockStun);
		IsBlocking = false;
		_controller.ActivateInput();
		_playerAnimator.CancelHurt();
		_playerAnimator.IsBlocking(false);
		_playerAnimator.IsBlockingLow(false);
		_playerAnimator.IsBlockingAir(false);
		_playerAnimator.ResetTrigger("CancelHurt");
		if (_controller.ControllerInputName == ControllerTypeEnum.Cpu.ToString() && TrainingSettings.OnHit)
		{
			LightAction();
		}
	}

	private void CheckIsBlocking()
	{
		if (!IsAttacking && !_playerMovement.IsDashing)
		{
			if (transform.localScale.x == 1.0f && _playerMovement.MovementInput.x < 0.0f 
				|| transform.localScale.x == -1.0f && _playerMovement.MovementInput.x > 0.0f )
			{
				if (_playerMovement.IsGrounded)
				{
					if (_playerMovement.MovementInput.y < 0.0f)
					{
						BlockingLow = true;
						BlockingHigh = false;
						BlockingMiddair = false;
					}
					else
					{
						BlockingLow = false;
						BlockingHigh = true;
						BlockingMiddair = false;
					}
				}
				else
				{
					BlockingLow = false;
					BlockingHigh = false;
					BlockingMiddair = true;
				}
	
			}
			else
			{
				BlockingLow = false;
				BlockingHigh = false;
				BlockingMiddair = false;
			}
		}
		else
		{
			BlockingLow = false;
			BlockingHigh = false;
			BlockingMiddair = false;
		}
	}

	private void Die()
	{
		DestroyEffects();
		_playerAnimator.IsKnockedDown(true);
		_controller.ActiveController.enabled = false;
		SetGroundPushBox(false);
		SetHurtbox(false);
		if (!IsDead)
		{
			if (GameManager.Instance.HasGameStarted && !GameManager.Instance.IsTrainingMode)
			{
				Lives--;
			}
			if (Lives <= 0)
			{
				GameManager.Instance.MatchOver();
			}
			else
			{
				GameManager.Instance.RoundOver();
			}
		}
		IsDead = true;
		GameManager.Instance.HitStop(0.35f);
	}

	public void Knockdown()
	{
		StartCoroutine(KnockdownCoroutine());
	}

	IEnumerator KnockdownCoroutine()
	{
		_throwBreakInvulnerable = true;
		_controller.DeactivateInput();
		SetHurtbox(false);
		_playerAnimator.IsKnockedDown(true);
		yield return new WaitForSeconds(0.75f);
		_playerAnimator.IsKnockedDown(false);
		_playerAnimator.ResetTrigger("CancelHurt");
		_playerMovement.SetLockMovement(false);
		yield return new WaitForSeconds(0.25f);
		SetHurtbox(true);
		IsKnockedDown = false;
		_controller.ActivateInput();
		_otherPlayerUI.ResetCombo();
		if (_controller.ControllerInputName == ControllerTypeEnum.Cpu.ToString() && TrainingSettings.OnHit)
		{
			LightAction();
		}
		yield return new WaitForSeconds(0.05f);
		_throwBreakInvulnerable = false;
	}

	public void Taunt()
	{
		_playerAnimator.Taunt();
		_playerMovement.SetLockMovement(true);
		_controller.ActiveController.enabled = false;
	}

	public void LoseLife()
	{
		Lives--;
		_playerUI.SetLives();
	}

	public void SetPushboxTrigger(bool state)
	{
		_groundPushbox.SetIsTrigger(state);
	}

	public void SetGroundPushBox(bool state)
	{
		_groundPushbox.gameObject.SetActive(state);
	}

	public void SetAirPushBox(bool state)
	{
		_airPushbox.gameObject.SetActive(state);
	}

	public void SetHurtbox(bool state)
	{
		_hurtbox.gameObject.SetActive(state);
	}

	public void Stun(float hitStun)
	{
		StopStun(false);
		_stunCoroutine = StartCoroutine(StunCoroutine(hitStun));
	}

	public void StopStun(bool resetCombo)
	{
		if (_stunCoroutine != null)
		{
			if (resetCombo)
			{
				_playerUI.UpdateHealthDamaged();
				_otherPlayerUI.ResetCombo();
			}
			IsStunned = false;
			StopCoroutine(_stunCoroutine);
		}
	}

	private void DestroyEffects()
	{
		foreach (Transform effect in _effectsParent)
		{
			Destroy(effect.gameObject);
		}
	}

	IEnumerator StunCoroutine(float hitStun)
	{
		IsStunned = true;
		_playerMovement.SetLockMovement(true);
		_controller.DeactivateInput();
		yield return new WaitForSeconds(hitStun);
		if (!HitMiddair)
		{
			_controller.ActivateInput();
			_playerAnimator.CancelHurt();
		}
		_playerMovement.SetLockMovement(false);
		_playerUI.UpdateHealthDamaged();
		_otherPlayerUI.ResetCombo();
		IsStunned = false;
		if (_controller.ControllerInputName == ControllerTypeEnum.Cpu.ToString() && TrainingSettings.OnHit)
		{
			LightAction();
		}
	}

	public void Pause(bool isPlayerOne)
	{
		if (GameManager.Instance.IsTrainingMode)
		{
			_playerUI.OpenTrainingPause(isPlayerOne);
		}
		else
		{
			_playerUI.OpenPauseHold(isPlayerOne);
		}
	}

	public void UnPause()
	{
		if (!GameManager.Instance.IsTrainingMode)
		{
			_playerUI.ClosePauseHold();
		}
	}

	public void HitboxCollidedGround(RaycastHit2D hit)
	{
		throw new System.NotImplementedException();
	}
}
