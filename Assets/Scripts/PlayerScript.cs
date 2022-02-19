using Mirror;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public enum MenuState { Open, Closed }

class Minion
{
  public GameObject target;
  public GameObject summon;
  public MinionScript script;
}

public class PlayerScript : NetworkBehaviour
{
  #region Input

  [SerializeField]
  InputAction moveAction;
  [SerializeField]
  InputAction lookAction;
  [SerializeField]
  InputAction sprintAction;
  [SerializeField]
  InputAction menuAction;
  [SerializeField]
  InputAction summonAction;
  [SerializeField]
  InputAction enemyAction;

  #endregion

  #region UI

  [SerializeField]
  Canvas canvas;
  Slider slider;
  Button closeButton;
  Button quitButton;
  Dropdown dropdown;

  #endregion

  [SerializeField]
  GameObject minion;
  [SerializeField]
  GameObject enemy;
  GameObject summonRing;
  GameObject model;
  Camera playerCamera;
  Transform cameraTransform;
  CharacterController characterController;
  MenuState menuState = MenuState.Open;
  Quaternion targetModelRotation = Quaternion.identity;
  float summonRingYRotation = 0;
  List<Minion> minions = new List<Minion>();
  List<GameObject> aggro = new List<GameObject>();

  float cameraPitch = 0;
  float baseSpeed = 3;
  float sprintSpeedMultiplier = 2;
  float lookSensitivity = 1;

  #region Getters

  Vector2 movementAxis => moveAction.ReadValue<Vector2>();
  Vector2 lookAxis => lookAction.ReadValue<Vector2>();
  bool isSprinting => sprintAction.ReadValue<float>() > 0;
  float lookAxisX => lookAxis.x;
  float lookAxisY => lookAxis.y;
  float movementAxisX => movementAxis.x;
  float movementAxisY => movementAxis.y;

  #endregion

  public override void OnStartLocalPlayer()
  {
    lookSensitivity = PlayerPrefs.GetFloat("sensitivity", 1);

    lookAction.Enable();
    menuAction.Enable();
    moveAction.Enable();
    summonAction.Enable();
    sprintAction.Enable();
    enemyAction.Enable();

    characterController = GetComponent<CharacterController>();
    cameraTransform = transform.Find("Camera Transform");
    playerCamera = cameraTransform.Find("Camera").GetComponent<Camera>();
    summonRing = transform.Find("Summon Ring").gameObject;
    model = transform.Find("Model").gameObject;

    #region UI

    closeButton = canvas.transform.Find("Close Button").GetComponent<Button>();
    closeButton.onClick.AddListener(() =>
    {
      CloseMenu();
    });

    quitButton = canvas.transform.Find("Quit Button").GetComponent<Button>();
    quitButton.onClick.AddListener(() =>
    {
      Application.Quit();
    });

    slider = canvas.transform.Find("Panel/Slider").GetComponent<Slider>();
    slider.onValueChanged.AddListener((float value) =>
    {
      lookSensitivity = value;

      PlayerPrefs.SetFloat("sensitivity", lookSensitivity);
      PlayerPrefs.Save();
    });

    var resolutions = Screen.resolutions;

    dropdown = canvas.transform.Find("Panel/Dropdown").GetComponent<Dropdown>();
    dropdown.options.AddRange(resolutions.Select(i => new Dropdown.OptionData { text = i.ToString() }));
    dropdown.onValueChanged.AddListener((int value) =>
    {
      var resolution = resolutions.ElementAt(value);

      Screen.SetResolution(resolution.width, resolution.height, FullScreenMode.Windowed, 0);
    });

    #endregion

    menuAction.performed += OnMenu;
    summonAction.performed += OnSummon;
    enemyAction.performed += OnEnemy;

    playerCamera.enabled = true;

    CloseMenu();

    base.OnStartLocalPlayer();
  }

  void Update()
  {
    if (!isLocalPlayer) return;

    HandleUpdateMovementAndAiming();

    summonRing.transform.rotation = Quaternion.Euler(Vector3.up * summonRingYRotation);

    // 180 means it's 180 degrees per second
    //  90 means it's  90 degrees per second
    //  45 means it's  45 degrees per second
    summonRingYRotation += (45 * Time.deltaTime) % 360;

    #region Experimental

    Debug.DrawRay(summonRing.transform.position, summonRing.transform.forward * 10);

    var expansion = Vector3.one * Mathf.Sin(((Time.frameCount / 5) % 360) * Mathf.Deg2Rad) * .5f;

    summonRing.transform.localScale = Vector3.one * 2 + expansion;

    #endregion
  }

  void FixedUpdate() { }

  void HandleUpdateMovementAndAiming()
  {
    if (menuState == MenuState.Open) return;

    var tempLookAxisX = lookAxisX * lookSensitivity;
    var tempLookAxisY = lookAxisY * lookSensitivity;

    var tempMovementAxisX = movementAxisX;
    var tempMovementAxisY = movementAxisY;

    var move = Vector3.ClampMagnitude(tempMovementAxisY * transform.forward + tempMovementAxisX * transform.right, 1);

    #region Model orientation

    if (move.magnitude > 0)
    {
      var targetRotation = Quaternion.LookRotation(move, Vector3.up);

      targetModelRotation = Quaternion.Lerp(targetModelRotation, targetRotation, 10 * Time.deltaTime);
    }

    model.transform.rotation = targetModelRotation;

    #endregion

    transform.Rotate(Vector3.up * tempLookAxisX);

    cameraPitch = Mathf.Clamp(cameraPitch - tempLookAxisY, -90, 90);

    cameraTransform.transform.localEulerAngles = new Vector3(cameraPitch, 0, 0);

    var moveSpeed = baseSpeed * (isSprinting ? sprintSpeedMultiplier : 1);

    characterController.Move((move * moveSpeed + Physics.gravity) * Time.deltaTime);

    #region Experimental

#if false

    if (summonAction.IsPressed())
    {
      Summon();
    }

#endif

    #endregion
  }

  void OpenMenu()
  {
    menuState = MenuState.Open;
    Cursor.lockState = CursorLockMode.None;

    slider.value = lookSensitivity;

    canvas.enabled = true;
  }

  void CloseMenu()
  {
    menuState = MenuState.Closed;
    Cursor.lockState = CursorLockMode.Locked;

    canvas.enabled = false;
  }

  void OnMenu(InputAction.CallbackContext context)
  {
    if (menuState == MenuState.Open)
      CloseMenu();
    else
      OpenMenu();
  }

  void OnSummon(InputAction.CallbackContext context)
  {
    Summon();
  }

  void OnEnemy(InputAction.CallbackContext context)
  {
    Enemy();
  }

  void Summon()
  {
    var target = GameObject.Instantiate(new GameObject(), Vector3.zero, Quaternion.identity, summonRing.transform);
    var summon = GameObject.Instantiate(minion);
    var script = summon.GetComponent<MinionScript>();

    script.SetOwner(gameObject);
    script.SetTarget(target.transform);
    script.SetAggro(aggro);

    target.AddComponent<TargetScript>();

    var tempMinion = new Minion { script = script, summon = summon, target = target };

    minions.Add(tempMinion);

    UpdateTargetLayout();
  }

  void Enemy()
  {
    var rand = Random.insideUnitCircle;
    var temp = GameObject.Instantiate(enemy, new Vector3(rand.x, 0, rand.y) * 10, Quaternion.identity);
  }

  void UpdateTargetLayout()
  {
    var count = minions.Count;

    if (count == 0) return;

    var step = 360f / count;

    int index = 0;

    foreach (var tempMinion in minions)
    {
      tempMinion.target.transform.localPosition = Quaternion.Euler(0, index * step, 0) * Vector3.forward * 1.5f;

      index++;
    }
  }

  void OnGUI()
  {
    var count = minions.Count;

    GUI.Label(new Rect(8, 0, 200, 200), $"{count} minions active");
  }

  public void AddAggro(GameObject go)
  {
    if (!aggro.Contains(go))
    {
      aggro.Add(go);

      UpdateAggro();
    }
  }

  public void RemoveAggro(GameObject go)
  {
    aggro.Remove(go);

    UpdateAggro();
  }

  void UpdateAggro()
  {
    foreach (var tempMinion in minions)
      tempMinion.script.SetAggro(aggro);
  }
}

/* 
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Mirror;

public enum MenuState { Open, Closed }

public class PlayerController : NetworkBehaviour
{
  [SerializeField] GameObject projectile;
  [SerializeField] Canvas canvas;
  Camera playerCamera;
  Button closeButton;
  ScrollRect scrollView;
  Animator weaponAnimator;
  Transform idleSwayTransform;
  Transform lookSwayTransform;
  Transform recoilSwayTransform;
  Transform weaponAnimationsTransform;
  Transform cameraRecoilSwayTransform;
  Transform projectileSpawnTransform;
  CharacterController characterController;
  Vector3 swayPosition = Vector3.zero;
  Vector3 positionalRecoil = Vector3.zero;
  Vector3 rotationalRecoil = Vector3.zero;
  Vector3 rotationalCameraRecoil = Vector3.zero;
  Vector3 recoilSwayTransformRotation = Vector3.zero;
  Vector3 cameraRecoilSwayTransformRotation = Vector3.zero;
  float cameraPitch = 0;
  float cameraRoll = 0;
  float sprintSpeed = 2;
  float maxCameraWalkLean = 2;
  float maxLookSway = 0.4f;
  float lookSwayFactor = 0.05f;
  float weaponLeanMultiplier = 2;
  bool isSprinting => Input.GetKey(KeyCode.LeftShift);
  float getYaw => Input.GetAxisRaw("Mouse X");
  float getPitch => Input.GetAxisRaw("Mouse Y");
  float getZ => Input.GetAxisRaw("Vertical");
  float getX => Input.GetAxisRaw("Horizontal");

  public MenuState menuState = MenuState.Open;

  public override void OnStartLocalPlayer()
  {
    characterController = GetComponent<CharacterController>();
    cameraRecoilSwayTransform = transform.Find("CameraRecoilSway");
    playerCamera = cameraRecoilSwayTransform.Find("Camera").GetComponent<Camera>();
    idleSwayTransform = playerCamera.transform.Find("IdleSway");
    lookSwayTransform = idleSwayTransform.Find("LookSway");
    recoilSwayTransform = lookSwayTransform.Find("RecoilSway");
    weaponAnimationsTransform = recoilSwayTransform.Find("WeaponAnimations");
    weaponAnimator = weaponAnimationsTransform.GetComponent<Animator>();
    projectileSpawnTransform = weaponAnimationsTransform.Find("ProjectileSpawn");

    scrollView = canvas.transform.Find("ScrollView").GetComponent<ScrollRect>();
    closeButton = canvas.transform.Find("CloseButton").GetComponent<Button>();

    closeButton.onClick.AddListener(() =>
    {
      CloseMenu();
    });

    // var content = new RectTransform();

    var item = new GameObject();

    item.AddComponent<Button>();

    // item.transform.SetParent(content);
    item.transform.SetParent(scrollView.content);

    // Debug.Log

    // content.transform.SetParent(scrollView.content);

    scrollView.content.gameObject.AddComponent<Button>();

    Cursor.lockState = CursorLockMode.Locked;
    playerCamera.enabled = true;

    base.OnStartLocalPlayer();
  }

  void Update()
  {
    if (!isLocalPlayer) return;

    HandleUpdateMovementAndAiming();

    HandleUpdateInput();
  }

  void FixedUpdate()
  {
    if (!isLocalPlayer) return;

    var yaw = getYaw;
    var pitch = getPitch;

    var z = getZ;
    var x = getX;

    var lookSway = menuState == MenuState.Open ? Vector3.zero : new Vector3(-yaw, -pitch, 0);

    idleSwayTransform.localEulerAngles = new Vector3(0, 0, cameraRoll * weaponLeanMultiplier);
    lookSwayTransform.localPosition = Vector3.Lerp(lookSwayTransform.localPosition, Vector3.ClampMagnitude(lookSway * lookSwayFactor, maxLookSway), Time.deltaTime * 4);

    ComputeRecoilSway();
    ComputeIdleSway();
  }

  IEnumerator StartShooting()
  {
    do
    {
      if (menuState == MenuState.Open) break;

      Shoot();

      yield return new WaitForSeconds(1f / (800f / 60f));
    } while (Input.GetMouseButton(0) && menuState == MenuState.Closed);
  }

  void Shoot()
  {
    Instantiate(projectile, projectileSpawnTransform.position, projectileSpawnTransform.rotation);
    positionalRecoil += new Vector3(0, 0.15f, -0.1f);
    rotationalRecoil += new Vector3(-8, Random.Range(-8, 8), 0);
    rotationalCameraRecoil += new Vector3(-8, Random.Range(-8, 8), 0);
  }

  void ComputeRecoilSway()
  {
    positionalRecoil = Vector3.Slerp(positionalRecoil, Vector3.zero, Time.deltaTime * 32);
    rotationalRecoil = Vector3.Slerp(rotationalRecoil, Vector3.zero, Time.deltaTime * 16);
    rotationalCameraRecoil = Vector3.Slerp(rotationalCameraRecoil, Vector3.zero, Time.deltaTime * 16);
    recoilSwayTransform.localPosition = Vector3.Slerp(recoilSwayTransform.localPosition, positionalRecoil, Time.deltaTime * 4);
    recoilSwayTransformRotation = Vector3.Slerp(recoilSwayTransformRotation, rotationalRecoil, Time.deltaTime * 4);
    cameraRecoilSwayTransformRotation = Vector3.Slerp(cameraRecoilSwayTransformRotation, rotationalCameraRecoil, Time.deltaTime * 4);
    recoilSwayTransform.localRotation = Quaternion.Euler(recoilSwayTransformRotation);
    cameraRecoilSwayTransform.localRotation = Quaternion.Euler(cameraRecoilSwayTransformRotation);
  }

  void ComputeIdleSway()
  {
    var targetPosition = CustomUtils.LissajousCurve(Time.realtimeSinceStartup * 1, 1, 2) / 100;
    swayPosition = Vector3.Lerp(swayPosition, targetPosition, Time.smoothDeltaTime * 14);
    idleSwayTransform.localPosition = swayPosition;
  }

  void HandleUpdateMovementAndAiming()
  {
    if (menuState == MenuState.Open) return;

    var yaw = getYaw;
    var pitch = getPitch;

    var z = getZ;
    var x = getX;

    var move = Vector3.ClampMagnitude(z * transform.forward + x * transform.right, 1);

    transform.Rotate(Vector3.up * yaw);

    cameraRoll = Mathf.Clamp(Mathf.LerpUnclamped(cameraRoll, x == 0 ? 0 : (x < 0 ? maxCameraWalkLean : -maxCameraWalkLean), 0.02f), -maxCameraWalkLean, maxCameraWalkLean);
    cameraPitch = Mathf.Clamp(cameraPitch - pitch, -90, 90);

    playerCamera.transform.localEulerAngles = new Vector3(cameraPitch, 0, cameraRoll);

    characterController.Move((move * sprintSpeed * (isSprinting ? 2 : 1) + Physics.gravity) * Time.deltaTime);

    weaponAnimator.speed = Mathf.Clamp(characterController.velocity.magnitude / 3, 0, 1);

    weaponAnimator.SetBool("IsSprinting", isSprinting);
    weaponAnimator.SetFloat("Velocity", characterController.velocity.magnitude);
  }

  void HandleUpdateInput()
  {
    if (Input.GetMouseButtonDown(0))
      StartCoroutine(StartShooting());

    if (Input.GetKeyDown(KeyCode.Escape))
    {
      if (menuState == MenuState.Closed)
        OpenMenu();
      else
        CloseMenu();
    }
  }

  void OpenMenu()
  {
    menuState = MenuState.Open;
    Cursor.lockState = CursorLockMode.None;

    canvas.enabled = true;
  }

  void CloseMenu()
  {
    menuState = MenuState.Closed;
    Cursor.lockState = CursorLockMode.Locked;

    canvas.enabled = false;
  }
}
 */