using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Paywall.Tools;

namespace Paywall {

    /// <summary>
    /// Override this to create an EX move
    /// </summary>
    public class CharacterEXMove : CharacterAbilityIRE {
        /// Does this ability cost EX to use
        [field: Tooltip("Does this ability cost EX to use")]
        [field: SerializeField] public int EXCost { get; protected set; } = 1;
        /// Block use of this ability while super is active
        [field: Tooltip("Block use of this ability while super is active")]
        [field: SerializeField] public bool BlockDuringSuper { get; protected set; }

        protected virtual void HandleInputSub(InputAction.CallbackContext ctx) {
            PerformAbility();
        }

        protected virtual void PerformAbility() {
            if (_character.SuperComponent.SuperActive) {
                return;
            }
        }

        protected override void OnEnable() {
            base.OnEnable();
            _inputManager.InputActions.PlayerControls.Dodge.performed += HandleInputSub;
        }

        protected override void OnDisable() {
            base.OnDisable();
            _inputManager.InputActions.PlayerControls.Dodge.performed -= HandleInputSub;
        }
    }
}
