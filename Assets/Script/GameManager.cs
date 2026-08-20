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
		if (slimeGen == null) slimeGen = FindAnyObjectByType<SlimeGen>();
		if (breedingManager == null) breedingManager = FindAnyObjectByType<BreedingManager>();
		if (breedingUIManager == null) breedingUIManager = FindAnyObjectByType<BreedingUIManager>();
		if (currencyManager == null) currencyManager = FindAnyObjectByType<CurrencyManager>();
		
		if (currencyManager == null)
		{
			Debug.LogWarning("CurrencyManager not found trong scene! Create GameObject voi CurrencyManager component.");
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
