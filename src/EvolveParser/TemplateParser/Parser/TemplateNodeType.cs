namespace EvolveUI.Parsing {

    internal enum TemplateStructureType : ushort {

        TemplateBlockNode,
        TemplateIfStatement,
        ElementNode,
        RepeatNode,
        RunSection,
        CreateSection,
        EnableSection,
        MarkerNode,
        TemplateConstDeclaration,
        TemplateVariableDeclaration,
        TemplateStateDeclaration,
        TemplateSwitchStatement,
        LocalTemplateFn,
        LocalMethodDeclaration,
        RenderSlotNode,
        RenderPortalNode,
        RenderMarkerNode,
        SlotOverrideNode,
        TeleportNode,
        TemplateSwitchSection,
        None = 100

    }


    internal enum TemplateNodeType : ushort {

        // structural nodes -- must exactly mirror TemplateStructureType values!
        TemplateBlockNode,
        TemplateIfStatement,
        ElementNode,
        RepeatNode,
        RunSection,
        CreateSection,
        EnableSection,
        MarkerNode,
        TemplateConstDeclaration,
        TemplateVariableDeclaration,
        TemplateStateDeclaration,
        TemplateSwitchStatement,
        LocalTemplateFn,
        MethodDeclaration,
        RenderSlotNode,
        RenderPortalNode,
        RenderMarkerNode,
        SlotOverrideNode,
        TeleportNode,
        TemplateSwitchSection,
        
        // Non structural nodes
        StyleListNode = 100,
        DecoratorNode,
        TemplateElementAssignment, // maybe remove
        AttributeAssignment,
        Extrusion,
        KeyedArgument, //maybe make an expression 
        TemplateSignatureNode,
        InstanceStyleNode,

        TemplateSwitchLabel,

        InputHandlerNode,
        LifeCycleEventNode,
        PropertyBindingNode, // maybe combine
        OnChangeBindingNode, // maybe combine
        RepeatParameter, // maybe expressions 

        TemplateFunctionSignature,
        TemplateFunctionParameter,
        TemplateParameter,
        DecoratorArgumentNode,
        TemplateSlotSignature,
        ComputedProperty,
        FromMapping,
        TemplateSpawnList,
        TypedQualifiedIdentifierNode

    }

}