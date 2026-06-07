using UnityEngine;

public class GameManager : MonoBehaviour
{
	public static GameManager Instance { get; private set; }

	[Header("System References")]
	public SlimeGen slimeGen;
	public BreedingManager breedingManager;
	public BreedingUIManager breedingUIManager;
	public CurrencyManager currencyManager;

	private void Awake()
	{
		if (Instance != null && Instance != this)
		{
			Destroy(gameObject);
			return;
		}
		Instance = this;
		DontDestroyOnLoad(gameObject);

		EnsureSystems();
	}

	private void EnsureSystems()
	{
		// Tìm các system components trong scene (không tự động tạo)
		if (slimeGen == null) slimeGen = FindAnyObjectByType<SlimeGen>();
		if (breedingManager == null) breedingManager = FindAnyObjectByType<BreedingManager>();
		if (breedingUIManager == null) breedingUIManager = FindAnyObjectByType<BreedingUIManager>();
		if (currencyManager == null) currencyManager = FindAnyObjectByType<CurrencyManager>();
		
		// Log warning nếu thiếu components quan trọng
		if (currencyManager == null)
		{
			Debug.LogWarning("CurrencyManager không tìm thấy trong scene! Vui lòng tạo GameObject với CurrencyManager component.");
		}
	}

	private void Start()
	{
		StartGame();
	}

	// Simple game flow helpers
	public void StartGame()
	{
		// Future: load main scene, initialize data, etc.
		if (breedingUIManager != null)
		{
			breedingUIManager.RefreshAllUI();
		}
	}


}
