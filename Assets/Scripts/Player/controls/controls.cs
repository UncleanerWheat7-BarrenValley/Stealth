//------------------------------------------------------------------------------
// <auto-generated>
//     This code was auto-generated by com.unity.inputsystem:InputActionCodeGenerator
//     version 1.7.0
//     from Assets/Scripts/Player/controls/controls.inputactions
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;

public partial class @Controls: IInputActionCollection2, IDisposable
{
    public InputActionAsset asset { get; }
    public @Controls()
    {
        asset = InputActionAsset.FromJson(@"{
    ""name"": ""controls"",
    ""maps"": [
        {
            ""name"": ""PlayerInput"",
            ""id"": ""1364ec8b-5a0f-48f5-8f04-105bdbd94255"",
            ""actions"": [
                {
                    ""name"": ""Movement"",
                    ""type"": ""PassThrough"",
                    ""id"": ""2817a480-2cc6-453d-9199-ee9224150920"",
                    ""expectedControlType"": ""Vector2"",
                    ""processors"": """",
                    ""interactions"": """",
                    ""initialStateCheck"": false
                },
                {
                    ""name"": ""Camera"",
                    ""type"": ""PassThrough"",
                    ""id"": ""ca77eb8c-dac6-4cdc-82d5-a87bb63b0ad1"",
                    ""expectedControlType"": ""Vector2"",
                    ""processors"": """",
                    ""interactions"": """",
                    ""initialStateCheck"": false
                },
                {
                    ""name"": ""Fire1"",
                    ""type"": ""Button"",
                    ""id"": ""d7139cc5-3886-43f3-beba-57c7c2275875"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": ""Press(behavior=2)"",
                    ""initialStateCheck"": false
                }
            ],
            ""bindings"": [
                {
                    ""name"": ""WASD"",
                    ""id"": ""6f51da7e-02ec-4a7e-8b03-1f58d5a3120c"",
                    ""path"": ""2DVector"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Movement"",
                    ""isComposite"": true,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": ""up"",
                    ""id"": ""1f9c1f5b-3631-4753-b2e9-7f2fb270a517"",
                    ""path"": ""<Keyboard>/w"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Movement"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                },
                {
                    ""name"": ""down"",
                    ""id"": ""6e639154-3033-4fc8-8df6-14368a489a36"",
                    ""path"": ""<Keyboard>/s"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Movement"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                },
                {
                    ""name"": ""left"",
                    ""id"": ""674b39dd-1bbb-400d-ae74-e45afc098925"",
                    ""path"": ""<Keyboard>/a"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Movement"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                },
                {
                    ""name"": ""right"",
                    ""id"": ""d9696cff-9b2f-4c39-b247-2ab9f51274fd"",
                    ""path"": ""<Keyboard>/d"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Movement"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                },
                {
                    ""name"": """",
                    ""id"": ""0662a6e3-49c4-481e-a582-3cbd381c1d12"",
                    ""path"": ""<Mouse>/delta"",
                    ""interactions"": """",
                    ""processors"": ""NormalizeVector2"",
                    ""groups"": """",
                    ""action"": ""Camera"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""ccbadffe-59bf-4464-ad70-e425b55588d1"",
                    ""path"": ""<Mouse>/leftButton"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Fire1"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                }
            ]
        },
        {
            ""name"": ""PlayerActions"",
            ""id"": ""5384f87d-8cd4-43eb-ad13-d5b749b4834a"",
            ""actions"": [
                {
                    ""name"": ""HugWall"",
                    ""type"": ""Button"",
                    ""id"": ""bd973e6d-06da-412e-84d4-b677ccbf9fe4"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": ""Press"",
                    ""initialStateCheck"": false
                },
                {
                    ""name"": ""WeaponWheel"",
                    ""type"": ""Button"",
                    ""id"": ""4b4e9087-1184-478f-aef6-0fe9fefa6a6a"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": """",
                    ""initialStateCheck"": false
                },
                {
                    ""name"": ""Crouch"",
                    ""type"": ""Button"",
                    ""id"": ""1be3d51b-c9ae-41a3-8f40-a3fa4e6efe4b"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": """",
                    ""initialStateCheck"": false
                },
                {
                    ""name"": ""WallKnock"",
                    ""type"": ""Button"",
                    ""id"": ""020df1ed-dcdd-4d69-97fd-9c0c66ef6498"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": """",
                    ""initialStateCheck"": false
                }
            ],
            ""bindings"": [
                {
                    ""name"": """",
                    ""id"": ""21075820-a718-490b-8384-bd2327b72ee9"",
                    ""path"": ""<Keyboard>/e"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""HugWall"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""cf6a723d-2330-4a4d-8412-5cc33ce92d32"",
                    ""path"": ""<Keyboard>/tab"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""WeaponWheel"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""32ffa0db-1100-455e-be18-bd2ce0fcf6b3"",
                    ""path"": ""<Keyboard>/c"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Crouch"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""d7dca57c-f7ac-412d-855e-4bdad88ac905"",
                    ""path"": ""<Keyboard>/q"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""WallKnock"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                }
            ]
        }
    ],
    ""controlSchemes"": []
}");
        // PlayerInput
        m_PlayerInput = asset.FindActionMap("PlayerInput", throwIfNotFound: true);
        m_PlayerInput_Movement = m_PlayerInput.FindAction("Movement", throwIfNotFound: true);
        m_PlayerInput_Camera = m_PlayerInput.FindAction("Camera", throwIfNotFound: true);
        m_PlayerInput_Fire1 = m_PlayerInput.FindAction("Fire1", throwIfNotFound: true);
        // PlayerActions
        m_PlayerActions = asset.FindActionMap("PlayerActions", throwIfNotFound: true);
        m_PlayerActions_HugWall = m_PlayerActions.FindAction("HugWall", throwIfNotFound: true);
        m_PlayerActions_WeaponWheel = m_PlayerActions.FindAction("WeaponWheel", throwIfNotFound: true);
        m_PlayerActions_Crouch = m_PlayerActions.FindAction("Crouch", throwIfNotFound: true);
        m_PlayerActions_WallKnock = m_PlayerActions.FindAction("WallKnock", throwIfNotFound: true);
    }

    public void Dispose()
    {
        UnityEngine.Object.Destroy(asset);
    }

    public InputBinding? bindingMask
    {
        get => asset.bindingMask;
        set => asset.bindingMask = value;
    }

    public ReadOnlyArray<InputDevice>? devices
    {
        get => asset.devices;
        set => asset.devices = value;
    }

    public ReadOnlyArray<InputControlScheme> controlSchemes => asset.controlSchemes;

    public bool Contains(InputAction action)
    {
        return asset.Contains(action);
    }

    public IEnumerator<InputAction> GetEnumerator()
    {
        return asset.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public void Enable()
    {
        asset.Enable();
    }

    public void Disable()
    {
        asset.Disable();
    }

    public IEnumerable<InputBinding> bindings => asset.bindings;

    public InputAction FindAction(string actionNameOrId, bool throwIfNotFound = false)
    {
        return asset.FindAction(actionNameOrId, throwIfNotFound);
    }

    public int FindBinding(InputBinding bindingMask, out InputAction action)
    {
        return asset.FindBinding(bindingMask, out action);
    }

    // PlayerInput
    private readonly InputActionMap m_PlayerInput;
    private List<IPlayerInputActions> m_PlayerInputActionsCallbackInterfaces = new List<IPlayerInputActions>();
    private readonly InputAction m_PlayerInput_Movement;
    private readonly InputAction m_PlayerInput_Camera;
    private readonly InputAction m_PlayerInput_Fire1;
    public struct PlayerInputActions
    {
        private @Controls m_Wrapper;
        public PlayerInputActions(@Controls wrapper) { m_Wrapper = wrapper; }
        public InputAction @Movement => m_Wrapper.m_PlayerInput_Movement;
        public InputAction @Camera => m_Wrapper.m_PlayerInput_Camera;
        public InputAction @Fire1 => m_Wrapper.m_PlayerInput_Fire1;
        public InputActionMap Get() { return m_Wrapper.m_PlayerInput; }
        public void Enable() { Get().Enable(); }
        public void Disable() { Get().Disable(); }
        public bool enabled => Get().enabled;
        public static implicit operator InputActionMap(PlayerInputActions set) { return set.Get(); }
        public void AddCallbacks(IPlayerInputActions instance)
        {
            if (instance == null || m_Wrapper.m_PlayerInputActionsCallbackInterfaces.Contains(instance)) return;
            m_Wrapper.m_PlayerInputActionsCallbackInterfaces.Add(instance);
            @Movement.started += instance.OnMovement;
            @Movement.performed += instance.OnMovement;
            @Movement.canceled += instance.OnMovement;
            @Camera.started += instance.OnCamera;
            @Camera.performed += instance.OnCamera;
            @Camera.canceled += instance.OnCamera;
            @Fire1.started += instance.OnFire1;
            @Fire1.performed += instance.OnFire1;
            @Fire1.canceled += instance.OnFire1;
        }

        private void UnregisterCallbacks(IPlayerInputActions instance)
        {
            @Movement.started -= instance.OnMovement;
            @Movement.performed -= instance.OnMovement;
            @Movement.canceled -= instance.OnMovement;
            @Camera.started -= instance.OnCamera;
            @Camera.performed -= instance.OnCamera;
            @Camera.canceled -= instance.OnCamera;
            @Fire1.started -= instance.OnFire1;
            @Fire1.performed -= instance.OnFire1;
            @Fire1.canceled -= instance.OnFire1;
        }

        public void RemoveCallbacks(IPlayerInputActions instance)
        {
            if (m_Wrapper.m_PlayerInputActionsCallbackInterfaces.Remove(instance))
                UnregisterCallbacks(instance);
        }

        public void SetCallbacks(IPlayerInputActions instance)
        {
            foreach (var item in m_Wrapper.m_PlayerInputActionsCallbackInterfaces)
                UnregisterCallbacks(item);
            m_Wrapper.m_PlayerInputActionsCallbackInterfaces.Clear();
            AddCallbacks(instance);
        }
    }
    public PlayerInputActions @PlayerInput => new PlayerInputActions(this);

    // PlayerActions
    private readonly InputActionMap m_PlayerActions;
    private List<IPlayerActionsActions> m_PlayerActionsActionsCallbackInterfaces = new List<IPlayerActionsActions>();
    private readonly InputAction m_PlayerActions_HugWall;
    private readonly InputAction m_PlayerActions_WeaponWheel;
    private readonly InputAction m_PlayerActions_Crouch;
    private readonly InputAction m_PlayerActions_WallKnock;
    public struct PlayerActionsActions
    {
        private @Controls m_Wrapper;
        public PlayerActionsActions(@Controls wrapper) { m_Wrapper = wrapper; }
        public InputAction @HugWall => m_Wrapper.m_PlayerActions_HugWall;
        public InputAction @WeaponWheel => m_Wrapper.m_PlayerActions_WeaponWheel;
        public InputAction @Crouch => m_Wrapper.m_PlayerActions_Crouch;
        public InputAction @WallKnock => m_Wrapper.m_PlayerActions_WallKnock;
        public InputActionMap Get() { return m_Wrapper.m_PlayerActions; }
        public void Enable() { Get().Enable(); }
        public void Disable() { Get().Disable(); }
        public bool enabled => Get().enabled;
        public static implicit operator InputActionMap(PlayerActionsActions set) { return set.Get(); }
        public void AddCallbacks(IPlayerActionsActions instance)
        {
            if (instance == null || m_Wrapper.m_PlayerActionsActionsCallbackInterfaces.Contains(instance)) return;
            m_Wrapper.m_PlayerActionsActionsCallbackInterfaces.Add(instance);
            @HugWall.started += instance.OnHugWall;
            @HugWall.performed += instance.OnHugWall;
            @HugWall.canceled += instance.OnHugWall;
            @WeaponWheel.started += instance.OnWeaponWheel;
            @WeaponWheel.performed += instance.OnWeaponWheel;
            @WeaponWheel.canceled += instance.OnWeaponWheel;
            @Crouch.started += instance.OnCrouch;
            @Crouch.performed += instance.OnCrouch;
            @Crouch.canceled += instance.OnCrouch;
            @WallKnock.started += instance.OnWallKnock;
            @WallKnock.performed += instance.OnWallKnock;
            @WallKnock.canceled += instance.OnWallKnock;
        }

        private void UnregisterCallbacks(IPlayerActionsActions instance)
        {
            @HugWall.started -= instance.OnHugWall;
            @HugWall.performed -= instance.OnHugWall;
            @HugWall.canceled -= instance.OnHugWall;
            @WeaponWheel.started -= instance.OnWeaponWheel;
            @WeaponWheel.performed -= instance.OnWeaponWheel;
            @WeaponWheel.canceled -= instance.OnWeaponWheel;
            @Crouch.started -= instance.OnCrouch;
            @Crouch.performed -= instance.OnCrouch;
            @Crouch.canceled -= instance.OnCrouch;
            @WallKnock.started -= instance.OnWallKnock;
            @WallKnock.performed -= instance.OnWallKnock;
            @WallKnock.canceled -= instance.OnWallKnock;
        }

        public void RemoveCallbacks(IPlayerActionsActions instance)
        {
            if (m_Wrapper.m_PlayerActionsActionsCallbackInterfaces.Remove(instance))
                UnregisterCallbacks(instance);
        }

        public void SetCallbacks(IPlayerActionsActions instance)
        {
            foreach (var item in m_Wrapper.m_PlayerActionsActionsCallbackInterfaces)
                UnregisterCallbacks(item);
            m_Wrapper.m_PlayerActionsActionsCallbackInterfaces.Clear();
            AddCallbacks(instance);
        }
    }
    public PlayerActionsActions @PlayerActions => new PlayerActionsActions(this);
    public interface IPlayerInputActions
    {
        void OnMovement(InputAction.CallbackContext context);
        void OnCamera(InputAction.CallbackContext context);
        void OnFire1(InputAction.CallbackContext context);
    }
    public interface IPlayerActionsActions
    {
        void OnHugWall(InputAction.CallbackContext context);
        void OnWeaponWheel(InputAction.CallbackContext context);
        void OnCrouch(InputAction.CallbackContext context);
        void OnWallKnock(InputAction.CallbackContext context);
    }
}
