using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Paywall.Tools;
using MoreMountains.Tools;

namespace Paywall {

    /// <summary>
    /// If the player jumps on an object with this component, it will launch the player
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class LaunchPad : MonoBehaviour {
        [field: Header("Launch Pad")]

        /// If true, use fixed height instead of launch force
        [field: Tooltip("If true, use fixed height instead of launch force")]
        [field: SerializeField] public bool UseLaunchHeight { get; protected set; } = true;
        /// Launch force
        [field: Tooltip("Launch force")]
        [field: FieldCondition("UseLaunchHeight", true, true)]
        [field: SerializeField] public float LaunchForce { get; protected set; } = 10f;
        /// Launch height
        [field: Tooltip("Launch height")]
        [field: FieldCondition("UseLaunchHeight", true, false)]
        [field: SerializeField] public float LaunchHeight { get; protected set; } = 2f;
        /// How many rays to cast to check for collision above
        [field: Tooltip("How many rays to cast to check for collision above")]
        [field: SerializeField] public int RaysToCast { get; protected set; } = 3;

        protected float _sideBuffer = 0.01f;

        protected const string _playerTag = "Player";
        protected Collider2D _collider2D;
        protected bool _collidingAbove;
        protected CharacterIRE _character;

        protected Vector2 _raycastLeftOrigin;
        protected Vector2 _raycastRightOrigin;

        protected virtual void Awake() {
            _collider2D = GetComponent<Collider2D>();
            if (RaysToCast <= 2) {
                RaysToCast = 2;
            }
        }

        protected virtual void OnCollisionEnter2D(Collision2D collision) {
            if (collision.collider.CompareTag(_playerTag)) {
                if (collision.collider.TryGetComponent(out _character)) {
                    _collidingAbove = CastRaysAbove();
                    if (_collidingAbove) {
                        if (!UseLaunchHeight) {
                            _character.GetComponent<CharacterJumpIRE>().ApplyExternalJumpForce(LaunchForce, true);
                        } else {
                            _character.GetComponent<CharacterJumpIRE>().ApplyExternalJumpForce(LaunchHeight, false);
                        }
                    }
                }
            }
        }

        protected virtual void OnCollisionExit2D(Collision2D collision) {
            if ((_character != null) && (collision.gameObject == _character.gameObject)) {
                _character = null;
            }
        }

        /// <summary>
        /// Returns true if the player is directly above this object
        /// </summary>
        /// <returns></returns>
        protected virtual bool CastRaysAbove() {
            Vector2 leftOrigin = new(_collider2D.bounds.min.x + _sideBuffer, _collider2D.bounds.max.y);
            Vector2 rightOrigin = new(_collider2D.bounds.max.x - _sideBuffer, _collider2D.bounds.max.y);
            RaycastHit2D raycastHit2D;

            for (int i = 0; i < RaysToCast; i++) {
                Vector2 originPoint = Vector2.Lerp(leftOrigin, rightOrigin, i / (float)(RaysToCast - 1f));
                raycastHit2D = Physics2D.Raycast(originPoint, Vector2.up, 1f, 1 << LayerMask.NameToLayer(_playerTag));
                if (raycastHit2D.collider != null) {
                    _collidingAbove = true;
                    return true;
                }
            }

            _collidingAbove = false;
            return false;
        }

        protected virtual void OnDrawGizmosSelected() {
            if (_collider2D == null) {
                _collider2D = GetComponent<Collider2D>();
            }
            Vector2 leftOrigin = new(_collider2D.bounds.min.x + _sideBuffer, _collider2D.bounds.max.y);
            Vector2 rightOrigin = new(_collider2D.bounds.max.x - _sideBuffer, _collider2D.bounds.max.y);
            for (int i = 0; i < RaysToCast; i++) {
                Vector2 originPoint = Vector2.Lerp(leftOrigin, rightOrigin, i / (float)(RaysToCast - 1f));
                Gizmos.color = Color.blue;
                Gizmos.DrawRay(originPoint, Vector3.up.normalized);
            }
        }

    }
}
