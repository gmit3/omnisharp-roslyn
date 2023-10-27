namespace EvolveUI {

    [Flags]
    public enum KeyboardModifiers : byte {

        None = 0,
        Alt = 1 << 0,
        Shift = 1 << 1,
        Control = 1 << 2,
        Command = 1 << 3,
        Windows = 1 << 4,
        NumLock = 1 << 5,
        CapsLock = 1 << 6

    }

    internal struct EnumOffsets {

        public const int LifeCycleOffset = 0;
        public const int InputOffset = 18;
        public const int AfterInput = 23;

    }

    [Flags]
    public enum BindingEvent : ulong {

        BeforeCreate = LifeCycleEventType.OnBeforeCreate,
        AfterCreate = LifeCycleEventType.OnAfterCreate,

        BeforeEnable = LifeCycleEventType.OnBeforeEnable,
        AfterEnable = LifeCycleEventType.OnAfterEnable,

        BeforeUpdate = LifeCycleEventType.OnBeforeUpdate,
        AfterUpdate = LifeCycleEventType.OnAfterUpdate,

        BeforeEarlyInput = LifeCycleEventType.OnBeforeEarlyInput,
        AfterEarlyInput = LifeCycleEventType.OnAfterEarlyInput,

        BeforeInput = LifeCycleEventType.OnBeforeInput,
        AfterInput = LifeCycleEventType.OnAfterInput,

        BeforeLateInput = LifeCycleEventType.OnBeforeLateInput,
        AfterLateInput = LifeCycleEventType.OnAfterLateInput,

        BeforeFinish = LifeCycleEventType.OnBeforeFinish,
        AfterFinish = LifeCycleEventType.OnAfterFinish,

        BeforeDestroy = LifeCycleEventType.OnBeforeDestroy,
        AfterDestroy = LifeCycleEventType.OnAfterDestroy,

        BeforeDisable = LifeCycleEventType.OnBeforeDisable,
        AfterDisable = LifeCycleEventType.OnAfterDisable,

        MouseEnter = InputEventType.MouseEnter,
        MouseExit = InputEventType.MouseExit,
        MouseUp = InputEventType.MouseUp,
        MouseDown = InputEventType.MouseDown,
        MouseHeldDown = InputEventType.MouseHeldDown,
        MouseMove = InputEventType.MouseMove,
        MouseHover = InputEventType.MouseHover,
        MouseContext = InputEventType.MouseContext,
        MouseScroll = InputEventType.MouseScroll,
        MouseClick = InputEventType.MouseClick,

        KeyDown = InputEventType.KeyDown,
        KeyUp = InputEventType.KeyUp,
        KeyHeldDown = InputEventType.KeyHeldDown,

        FocusGain = InputEventType.FocusGain,
        FocusLost = InputEventType.FocusLost,

        DragCreate = InputEventType.DragCreate,
        DragMove = InputEventType.DragMove,
        DragHover = InputEventType.DragHover,
        DragEnter = InputEventType.DragEnter,
        DragExit = InputEventType.DragExit,
        DragDrop = InputEventType.DragDrop,
        DragCancel = InputEventType.DragCancel,

        TextInput = InputEventType.TextInput,

        Default = AfterInput + 0,

        DragUpdate = DragMove | DragHover,
        MouseUpdate = MouseMove | MouseHover,

        Always = ulong.MaxValue,


    }

    [Flags]
    public enum LifeCycleEventType : ulong {

        Invalid,

        OnBeforeCreate = 1L << EnumOffsets.LifeCycleOffset + 0,
        OnAfterCreate = 1L << EnumOffsets.LifeCycleOffset + 1,

        OnBeforeEnable = 1L << EnumOffsets.LifeCycleOffset + 2,
        OnAfterEnable = 1L << EnumOffsets.LifeCycleOffset + 3,

        OnBeforeUpdate = 1L << EnumOffsets.LifeCycleOffset + 4,
        OnAfterUpdate = 1L << EnumOffsets.LifeCycleOffset + 5,

        OnBeforeEarlyInput = 1L << EnumOffsets.LifeCycleOffset + 6,
        OnAfterEarlyInput = 1L << EnumOffsets.LifeCycleOffset + 7,

        OnBeforeInput = 1L << EnumOffsets.LifeCycleOffset + 8,
        OnAfterInput = 1L << EnumOffsets.LifeCycleOffset + 9,

        OnBeforeLateInput = 1L << EnumOffsets.LifeCycleOffset + 10,
        OnAfterLateInput = 1L << EnumOffsets.LifeCycleOffset + 11,

        OnBeforeFinish = 1L << EnumOffsets.LifeCycleOffset + 12,
        OnAfterFinish = 1L << EnumOffsets.LifeCycleOffset + 13,

        OnBeforeDisable = 1L << EnumOffsets.LifeCycleOffset + 14,
        OnAfterDisable = 1L << EnumOffsets.LifeCycleOffset + 15,

        OnBeforeDestroy = 1L << EnumOffsets.LifeCycleOffset + 16,
        OnAfterDestroy = 1L << EnumOffsets.LifeCycleOffset + 17,

    }

    // this type mirrors LifeCycleEvent, do not change one without the other!
    [Flags]
    public enum InputEventType : ulong {

        None = 0,
        MouseEnter = 1L << EnumOffsets.InputOffset + 0,
        MouseExit = 1L << EnumOffsets.InputOffset + 1,
        MouseUp = 1L << EnumOffsets.InputOffset + 2,
        MouseDown = 1L << EnumOffsets.InputOffset + 3,
        MouseHeldDown = 1L << EnumOffsets.InputOffset + 4,
        MouseMove = 1L << EnumOffsets.InputOffset + 5,
        MouseHover = 1L << EnumOffsets.InputOffset + 6,
        MouseContext = 1L << EnumOffsets.InputOffset + 7,
        MouseScroll = 1L << EnumOffsets.InputOffset + 8,
        MouseClick = 1L << EnumOffsets.InputOffset + 9,

        KeyDown = 1L << EnumOffsets.InputOffset + 10,
        KeyUp = 1L << EnumOffsets.InputOffset + 11,
        KeyHeldDown = 1L << EnumOffsets.InputOffset + 12,

        FocusGain = 1L << EnumOffsets.InputOffset + 13,
        FocusLost = 1L << EnumOffsets.InputOffset + 14,

        DragCreate = 1L << EnumOffsets.InputOffset + 15,
        DragMove = 1L << EnumOffsets.InputOffset + 16,
        DragHover = 1L << EnumOffsets.InputOffset + 17,
        DragEnter = 1L << EnumOffsets.InputOffset + 18,
        DragExit = 1L << EnumOffsets.InputOffset + 19,
        DragDrop = 1L << EnumOffsets.InputOffset + 20,
        DragCancel = 1L << EnumOffsets.InputOffset + 21,

        TextInput = 1L << EnumOffsets.InputOffset + 22,

        DragUpdate = DragMove | DragHover,
        MouseUpdate = MouseMove | MouseHover,

        MOUSE_EVENT = MouseEnter | MouseExit | MouseUp | MouseDown | MouseHeldDown | MouseMove | MouseHover | MouseContext | MouseScroll | MouseClick,
        KEY_EVENT = KeyDown | KeyUp | KeyHeldDown,
        DRAG_EVENT = DragCreate | DragMove | DragHover | DragExit | DragEnter | DragDrop | DragCancel,
        FOCUS_EVENT = FocusGain | FocusLost

    }

}
