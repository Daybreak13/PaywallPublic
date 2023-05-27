using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Paywall.Tools {

	public class Singleton_PW<T> : MonoBehaviour_PW where T : Component {
		protected static T _instance;
		public static bool HasInstance => _instance != null;
		public static T TryGetInstance() => HasInstance ? _instance : null;
		public static T Current => _instance;

		/// <summary>
		/// Singleton design pattern
		/// </summary>
		/// <value>The instance.</value>
		public static T Instance {
			get {
				if (_instance == null) {
					_instance = FindObjectOfType<T>();
					if (_instance == null) {
						GameObject obj = new GameObject();
						obj.name = typeof(T).Name + "_AutoCreated";
						_instance = obj.AddComponent<T>();
					}
				}
				return _instance;
			}
		}

		/// <summary>
		/// On awake, we initialize our instance. Make sure to call base.Awake() in override if you need awake.
		/// </summary>
		protected virtual void Awake() {
			InitializeSingleton();
		}

		/// <summary>
		/// Initializes the singleton.
		/// </summary>
		protected virtual void InitializeSingleton() {
			if (!Application.isPlaying) {
				return;
			}

			_instance = this as T;
		}
	}
}