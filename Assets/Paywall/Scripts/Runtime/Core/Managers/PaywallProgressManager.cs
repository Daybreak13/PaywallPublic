using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MoreMountains.Tools;
using UnityEngine.SceneManagement;
using Paywall.Documents;
using MoreMountains.InventoryEngine;
using MoreMountains.InfiniteRunnerEngine;

namespace Paywall {
	
	[System.Serializable]
	/// <summary>
	/// A serializable entity to store scenes, whether they've been completed, unlocked, and which ones
	/// </summary>
	public class PaywallScene {
		public string SceneName;
		public bool LevelComplete = false;
		public bool LevelUnlocked = false;
	}

	[System.Serializable]
	/// <summary>
	/// A serializable entity used to store progress : a list of scenes with their internal status (see above), how many lives are left, and how much we can have
	/// </summary>
	public class Progress {
		public int InitialMaximumLives = 0;
		public int InitialCurrentLives = 0;
		public int MaximumLives = 0;
		public int CurrentLives = 0;
		public int Credits = 0;
		public int Trinkets = 0;
		public Dictionary<string, Upgrade> Upgrades = new();
		public Dictionary<string, Upgrade> RunnerUpgrades = new();
		public Dictionary<string, EmailItem> EmailItems = new();
		public EventFlagManager EventFlags;
		public SerializedInventory serializedInventory;
		public PaywallScene[] Scenes;
	}

	/// <summary>
	/// Creates save files for game progress and handles saves and loads
	/// Every time credits are updated, save the game
	/// Saves game on MMGameEvent "Save"
	/// 
	/// </summary>
	public class PaywallProgressManager : MMPersistentSingleton<PaywallProgressManager>, MMEventListener<PaywallCreditsEvent>,
		MMEventListener<MMGameEvent>, MMEventListener<PaywallUpgradeEvent>,
		MMEventListener<PaywallLevelEndEvent>, MMEventListener<EmailEvent> {

		public int InitialMaximumLives { get; set; }
		public int InitialCurrentLives { get; set; }

		public string PlayerID = "Player1";

		/// the list of scenes that we'll want to consider for our game
		[Tooltip("the list of scenes that we'll want to consider for our game")]
		public PaywallScene[] Scenes;

		[MMInspectorButton("CreateSaveGame")]
		/// A button to test creating the save file
		public bool CreateSaveGameBtn;

		/// Credits, readonly, for debug purposes only. Comment out Update for final build.
		[field: Tooltip("Credits, readonly, for debug purposes only. Comment out Update for final build.")]
		[field: MMReadOnly]
		[field: SerializeField] private int DisplayedCredits;

		[field: Header("Currency")]

		/// Credits are the "real world" currency
		[field: Tooltip("Credits are the \"real world\" currency")]
		[field: MMReadOnly]
		[field: SerializeField] public int Credits { get; private set; }
		/// Trinkets are the "game world" currency
		[field: Tooltip("Trinkets are the \"game world\" currency")]
		[field: MMReadOnly]
		[field: SerializeField] public int Trinkets { get; private set; }

		[field: Header("Dictionaries")]

		/// Scriptable dictionary for emails. Shared by all components that require email information, so that when emails are updated every component has an updated record. Loaded/saved via script.
		[field: Tooltip("Scriptable dictionary for emails. Shared by all components that require email information, so that when emails are updated every component has an updated record.")]
		[field: SerializeField] public EmailDictionary EmailsDictionary { get; private set; }
		/// Scriptable dictionary for upgrades. Contains an archive of all possible upgrades in the game. Set this in inspector.
		[field: Tooltip("Scriptable dictionary for upgrades. Contains an archive of all possible upgrades in the game. Set this in inspector.")]
		[field: SerializeField] public UpgradeDictionary UpgradesDictionary { get; private set; }

		public int CurrentLevelTrinkets { get; private set; }
		public Dictionary<string, EmailItem> EmailItems { get { return EmailsDictionary.EmailItems; } protected set { } }
		/// <summary>
		/// Dictionary of all unlocked upgrades
		/// </summary>
		public Dictionary<string, Upgrade> Upgrades { get; private set; } = new();
		public EventFlagManager EventFlags { get; protected set; } = new();
		public enum SaveMethods { All, Inventory }

		protected EmailInventory _emailInventory;
		protected StoreMenuManager _storeMenuManager;

		protected const string _saveFolderName = "PaywallProgress";
		protected const string _saveFileName = "Progress.data";

		/// <summary>
		/// On awake, we load our progress
		/// </summary>
		protected override void Awake() {
			base.Awake();
			LoadSavedProgress();
		}

		protected virtual void Start() {

        }

		protected virtual void OnSceneLoaded(Scene scene, LoadSceneMode mode) {
			UpdateManagersAndInventories();
			CurrentLevelTrinkets = 0;
			if (GUIManagerIRE_PW.HasInstance) {
				GUIManagerIRE_PW.Instance.UpdateTrinketsText(CurrentLevelTrinkets);
			}
		}

		/// <summary>
		/// When the scene loads, load the data into the StoreMenuManager and EmailInventory (if applicable)
		/// </summary>
		protected virtual void UpdateManagersAndInventories() {
			if (_storeMenuManager = FindAnyObjectByType<StoreMenuManager>(FindObjectsInactive.Include)) {
				_storeMenuManager.LoadUpgrades(Upgrades);
            }
			if (_emailInventory = FindAnyObjectByType<EmailInventory>(FindObjectsInactive.Include)) {
				_emailInventory.SetDictionary(EmailsDictionary);
            }
        }

		/// <summary>
		/// Updates credit display
		/// For debugging only. Comment out for final build.
		/// </summary>
		protected virtual void Update() {
			DisplayedCredits = Credits;
        }

		/// <summary>
		/// When a level is completed, we update our progress
		/// </summary>
		protected virtual void LevelComplete() {
			for (int i = 0; i < Scenes.Length; i++) {
				if (Scenes[i].SceneName == SceneManager.GetActiveScene().name) {
					Scenes[i].LevelComplete = true;
					Scenes[i].LevelUnlocked = true;
					if (i < Scenes.Length - 1) {
						Scenes[i + 1].LevelUnlocked = true;
					}
				}
			}
		}

		/// <summary>
		/// Saves the progress to a file
		/// </summary>
		protected virtual void SaveProgress(SaveMethods saveMethod = SaveMethods.All) {
			Progress progress = new();

			if (saveMethod == SaveMethods.All) {
				progress.CurrentLives = (GameManagerIRE_PW.Instance as GameManagerIRE_PW).CurrentLives;

				progress.Scenes = Scenes;
				progress.Credits = Credits;
				progress.Trinkets = Trinkets;
				progress.Upgrades = Upgrades;
				progress.EmailItems = EmailItems;
				progress.EventFlags = EventFlags;
			}
			if (saveMethod == SaveMethods.Inventory) {

            }

			//MMSaveLoadManager.Save(progress, _saveFileName, _saveFolderName);
		}

		/// <summary>
		/// Called when the level ends.
		/// Convert points, save progress.
		/// </summary>
		protected virtual void LevelEnd(bool convert) {
			if (convert) {
				ConvertPointsToCredits();
			}
			SaveProgress();
        }

		/// <summary>
		/// Converts current points to credits
		/// </summary>
		protected virtual void ConvertPointsToCredits() {
			if (GameManagerIRE_PW.HasInstance) {
				Credits += (int)(GameManagerIRE_PW.Instance as GameManagerIRE_PW).Points;
            }
        }

		/// <summary>
		/// Adds money
		/// </summary>
		/// <param name="money"></param>
		protected virtual void AddMoney(MoneyTypes MoneyType, int money) {
			if (MoneyType == MoneyTypes.Credit) {
				Credits += money;
			} else {
				Trinkets += money;
				if (GameManagerIRE_PW.HasInstance
					&& (GameManagerIRE_PW.Instance.Status == GameManagerIRE_PW.GameStatus.GameInProgress)) {
					CurrentLevelTrinkets += money;

					if (GUIManagerIRE_PW.HasInstance) {
						GUIManagerIRE_PW.Instance.UpdateTrinketsText(CurrentLevelTrinkets);
					}
				}
			}
        }

		/// <summary>
		/// Sets money
		/// </summary>
		/// <param name="money"></param>
		protected virtual void SetMoney(MoneyTypes MoneyType, int money) {
			if (MoneyType == MoneyTypes.Credit) {
				Credits = money;
			} else {
				Trinkets = money;
				if (GameManagerIRE_PW.HasInstance
					&& (GameManagerIRE_PW.Instance.Status == GameManagerIRE_PW.GameStatus.GameInProgress)) {
					CurrentLevelTrinkets = money;

					if (GUIManagerIRE_PW.HasInstance) {
						GUIManagerIRE_PW.Instance.UpdateTrinketsText(CurrentLevelTrinkets);
					}
				}
            }
		}

		/// <summary>
		/// A test method to create a test save file at any time from the inspector
		/// </summary>
		protected virtual void CreateSaveGame() {
			SaveProgress();
		}

		/// <summary>
		/// Loads the saved progress into memory
		/// </summary>
		protected virtual void LoadSavedProgress() {
			Progress progress = (Progress)MMSaveLoadManager.Load(typeof(Progress), _saveFileName, _saveFolderName);
			if (progress != null) {
				Scenes = progress.Scenes;
				(GameManagerIRE_PW.Instance as GameManagerIRE_PW).SetLives(progress.CurrentLives);
				InitialMaximumLives = progress.InitialMaximumLives;
				InitialCurrentLives = progress.InitialCurrentLives;
				Scenes = progress.Scenes;
				Credits = progress.Credits;
				Trinkets = progress.Trinkets;
				if (progress.Upgrades == null) {
					Upgrades = new();
				}
				else {
					Upgrades = progress.Upgrades;
				}
				EmailsDictionary.SetDictionary(progress.EmailItems);
				if (progress.EventFlags == null) {
					EventFlags = new();
				}
				else {
					EventFlags = progress.EventFlags;
				}
			}
			else {
				InitialCurrentLives = (GameManagerIRE_PW.Instance as GameManagerIRE_PW).CurrentLives;
			}
		}

		/// <summary>
		/// A method used to remove all save files associated to progress
		/// </summary>
		public virtual void ResetProgress() {
			MMSaveLoadManager.DeleteSaveFolder(_saveFolderName);
		}

		protected virtual void ApplyUpgradesToCharacter() {
			if (LevelManager.HasInstance && (LevelManager.Instance.CurrentPlayableCharacters.Count > 0) && (Upgrades != null)) {
				foreach (KeyValuePair<string, Upgrade> entry in Upgrades) {
					if (entry.Value.UpgradeType == UpgradeTypes.Player) {
						//entry.Value.UpgradeAction(LevelManager.Instance.CurrentPlayableCharacters[0]);
                    }
                }
            }
        }

		public virtual void OnMMEvent(PaywallCreditsEvent creditsEvent) {
			switch (creditsEvent.MoneyMethod) {
				case MoneyMethods.Add:
					AddMoney(creditsEvent.MoneyType, creditsEvent.Money);
					break;
				case MoneyMethods.Set:
					SetMoney(creditsEvent.MoneyType, creditsEvent.Money);
					break;
			}
			if (creditsEvent.MoneyType == MoneyTypes.Credit) SaveProgress();
		}

		/// <summary>
		/// On an upgrade event, update the upgrade data in the progress manager and save the game
		/// </summary>
		/// <param name="upgradeEvent"></param>
		public virtual void OnMMEvent(PaywallUpgradeEvent upgradeEvent) {
			if ((upgradeEvent.UpgradeMethod == UpgradeMethods.TryUnlock)) {
				if (!Upgrades.ContainsKey(upgradeEvent.Upgrade.UpgradeID)) {
					Upgrades.Add(upgradeEvent.Upgrade.UpgradeID, upgradeEvent.Upgrade.ConvertToClass());
				}
				Upgrade upgrade = Upgrades[upgradeEvent.Upgrade.UpgradeID];
				if (upgrade.Unlocked) {
					return;
				}

				// Do nothing if there is insufficient funds
				if (upgrade.MoneyType == MoneyTypes.Trinket) {
					if (upgrade.Cost > Trinkets) {
						PaywallUpgradeEvent.Trigger(UpgradeMethods.Error);
						return;
					}
				}
				else {
					if (upgrade.Cost > Trinkets) {
						PaywallUpgradeEvent.Trigger(UpgradeMethods.Error);
						return;
					}
				}

				Upgrades[upgradeEvent.Upgrade.UpgradeName].UnlockUpgrade();
				upgradeEvent.ButtonComponent.SetAsUnlocked();
				PaywallUpgradeEvent.Trigger(UpgradeMethods.Unlock, null, upgradeEvent.ButtonComponent);
				PaywallCreditsEvent.Trigger(upgrade.MoneyType, MoneyMethods.Add, -upgradeEvent.Upgrade.Cost);
				SaveProgress();
			}
			
		}

		public virtual void OnMMEvent(MMGameEvent gameEvent) {
			if (gameEvent.EventName == "Save") {
				SaveProgress();
            }
			if (gameEvent.EventName == "Load") {

            }
        }

		public virtual void OnMMEvent(EmailEvent emailEvent) {
			if (emailEvent.EventType == EmailEventType.ContentChanged) {
				//EmailItems = emailEvent.EmailItems;
				SaveProgress();
            }
        }

		public virtual void OnMMEvent(PaywallLevelEndEvent endEvent) {
			LevelEnd(endEvent.convertCredits);
        }

		/// <summary>
		/// OnEnable, we start listening to events.
		/// </summary>
		protected virtual void OnEnable() {
			this.MMEventStartListening<PaywallCreditsEvent>();
			this.MMEventStartListening<MMGameEvent>();
			this.MMEventStartListening<PaywallUpgradeEvent>();
			this.MMEventStartListening<PaywallLevelEndEvent>();
			this.MMEventStartListening<EmailEvent>();
			SceneManager.sceneLoaded += OnSceneLoaded;
		}

		/// <summary>
		/// OnDisable, we stop listening to events.
		/// </summary>
		protected virtual void OnDisable() {
			this.MMEventStopListening<PaywallCreditsEvent>();
			this.MMEventStopListening<MMGameEvent>();
			this.MMEventStopListening<PaywallUpgradeEvent>();
			this.MMEventStopListening<PaywallLevelEndEvent>();
			this.MMEventStopListening<EmailEvent>();
			SceneManager.sceneLoaded -= OnSceneLoaded;
		}

	}
}
